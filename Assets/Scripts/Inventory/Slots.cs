using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Kezeli a játékos inventory-ját, beleértve a tárgyak hozzáadását, eltávolítását,
/// és a specializált fegyver/általános slotok logikáját.
/// </summary>
public class Slots : NetworkBehaviour
{
    [Header("Inventár Beállítások")]
    [Tooltip("Az inventory teljes mérete (fegyver és általános slotok együtt).")]
    [SerializeField] private int inventorySize = 10;

    [Tooltip("Az első hány slot van a fegyvereknek fenntartva (0-tól kezdve). A többi slot általános célú lesz.")]
    [SerializeField] private int weaponSlotCount = 2;

    // A hálózaton szinkronizált lista, ami a tárgyak adatait tárolja.
    private NetworkList<ItemData> inventoryItems;

    // Esemény, ami akkor sül el, ha az inventory tartalma megváltozik.
    // A UI menedzser erre iratkozik fel, hogy frissítse a kijelzőt.
    public static event Action OnInventoryUpdated;

    void Awake()
    {
        inventoryItems = new NetworkList<ItemData>();
    }

    public override void OnNetworkSpawn()
    {
        // A szerver inicializálja az inventory-t üres slotokkal.
        if (IsServer)
        {
            InitializeInventory();
        }
        // Minden kliens (és a szerver is) feliratkozik a lista változásaira.
        inventoryItems.OnListChanged += OnInventoryListChanged;
    }

    public override void OnNetworkDespawn()
    {
        // Fontos leiratkozni, hogy elkerüljük a memóriaszivárgást.
        inventoryItems.OnListChanged -= OnInventoryListChanged;
    }

    /// <summary>
    /// A szerveren feltölti az inventory-t üres adatokkal a játék kezdetekor.
    /// </summary>
    private void InitializeInventory()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            inventoryItems.Add(new ItemData { isEmpty = true });
        }
    }

    /// <summary>
    /// [ServerRpc] Hozzáad egy tárgyat az inventory-hoz.
    /// A kategória alapján a megfelelő slot-típusba (fegyver/általános) helyezi.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddItemServerRpc(int itemID, string itemName, int quantity)
    {
        if (!IsServer) return;

        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef == null)
        {
            Debug.LogError($"Server: Item with ID {itemID} not found.");
            return;
        }

        // Meghatározzuk a keresési tartományt a tárgy kategóriája alapján.
        int startIndex = (itemDef.category == ItemCategory.Weapon) ? 0 : weaponSlotCount;
        int endIndex = (itemDef.category == ItemCategory.Weapon) ? weaponSlotCount : inventorySize;

        bool itemAdded = false;

        // 1. Megpróbáljuk stack-elni egy meglévő tárgyhoz a megfelelő tartományban.
        if (itemDef.isStackable)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                ItemData currentItem = inventoryItems[i];
                if (!currentItem.isEmpty && currentItem.itemID == itemID && currentItem.quantity < itemDef.maxStackSize)
                {
                    int canAdd = itemDef.maxStackSize - currentItem.quantity;
                    int amountToAdd = Mathf.Min(quantity, canAdd);

                    currentItem.quantity += amountToAdd;
                    inventoryItems[i] = currentItem;

                    quantity -= amountToAdd;
                    if (quantity <= 0)
                    {
                        itemAdded = true;
                        break;
                    }
                }
            }
        }

        // 2. Ha maradt még a tárgyból, keresünk egy üres helyet a megfelelő tartományban.
        if (quantity > 0)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                if (inventoryItems[i].isEmpty)
                {
                    inventoryItems[i] = new ItemData(itemID, itemName, quantity);
                    itemAdded = true;
                    break;
                }
            }
        }

        if (!itemAdded && quantity > 0)
        {
            Debug.LogWarning($"Server: Inventory is full for category '{itemDef.category}'. Could not add {itemName}.");
        }
    }

    /// <summary>
    /// [Szerver oldali] A PlayerController hívja meg, amikor a játékos támadni próbál egy fegyver slottal.
    /// </summary>
    public void TriggerAttackFromSlot(int slotIndex)
    {
        if (!IsServer) return;
        if (slotIndex < 0 || slotIndex >= weaponSlotCount || inventoryItems[slotIndex].isEmpty) return;

        ItemData itemToAttackWith = inventoryItems[slotIndex];
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemToAttackWith.itemID);

        if (itemDef != null && itemDef.category == ItemCategory.Weapon)
        {
            PekkaPlayerController playerController = GetComponent<PekkaPlayerController>();
            if (playerController != null)
            {
                playerController.ExecuteAttack();
                playerController.PlayUseSoundClientRpc(itemToAttackWith.itemID);
            }
        }
    }

    /// <summary>
    /// [ServerRpc] Egy általános tárgyat használ el a megadott slotból (nem fegyvert).
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void UseItemServerRpc(int slotIndex)
    {
        if (!IsServer) return;
        if (slotIndex < weaponSlotCount || slotIndex >= inventoryItems.Count || inventoryItems[slotIndex].isEmpty) return;

        ItemData itemToUse = inventoryItems[slotIndex];
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemToUse.itemID);
        if (itemDef == null || itemDef.category == ItemCategory.Weapon) return;

        PekkaPlayerController playerController = GetComponent<PekkaPlayerController>();
        if (playerController == null) return;

        bool itemWasConsumed = false;
        if (itemDef is ConsumableItemDefinition consumable)
        {
            playerController.HealServerRpc(consumable.healthRestored);
            itemWasConsumed = true;
        }
        else if (itemDef is PowerUpItemDefinition powerUp)
        {
            playerController.ApplyPowerUpServerRpc(powerUp.itemID);
            itemWasConsumed = true;
        }

        if (itemWasConsumed)
        {
            playerController.PlayUseSoundClientRpc(itemToUse.itemID);
            RemoveItemServerRpc(slotIndex, 1);
        }
    }

    /// <summary>
    /// [ServerRpc] Eltávolít egy megadott mennyiségű tárgyat egy slotból.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RemoveItemServerRpc(int slotIndex, int quantityToRemove = 1)
    {
        if (!IsServer) return;
        if (slotIndex < 0 || slotIndex >= inventoryItems.Count || inventoryItems[slotIndex].isEmpty) return;

        ItemData itemInSlot = inventoryItems[slotIndex];
        itemInSlot.quantity -= quantityToRemove;

        if (itemInSlot.quantity <= 0)
        {
            inventoryItems[slotIndex] = new ItemData { isEmpty = true };
        }
        else
        {
            inventoryItems[slotIndex] = itemInSlot;
        }
    }

    /// <summary>
    /// Visszaadja a teljes inventory listát. A UI menedzser használja.
    /// </summary>
    public NetworkList<ItemData> GetInventoryItems()
    {
        return inventoryItems;
    }

    /// <summary>
    /// Callback metódus, ami lefut minden kliensen, ha a szerver oldali lista megváltozik.
    /// </summary>
    private void OnInventoryListChanged(NetworkListEvent<ItemData> changeEvent)
    {
        OnInventoryUpdated?.Invoke();
    }

    public void LoadInventoryData(List<ItemDataSerializable> itemsToLoad)
    {
        if (!IsServer) return;

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (i < itemsToLoad.Count)
            {
                var loadedItem = itemsToLoad[i];
                if (loadedItem.isEmpty)
                {
                    inventoryItems[i] = new ItemData { isEmpty = true };
                }
                else
                {
                    ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(loadedItem.itemID);
                    if (itemDef != null)
                    {
                        inventoryItems[i] = new ItemData(loadedItem.itemID, itemDef.itemName, loadedItem.quantity);
                    }
                }
            }
            else
            {
                inventoryItems[i] = new ItemData { isEmpty = true };
            }
        }
    }
}