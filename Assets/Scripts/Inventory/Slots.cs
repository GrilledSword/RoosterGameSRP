using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Slots : NetworkBehaviour
{
    [Header("Inventár Beállítások")]
    [SerializeField] private int inventorySize = 10;
    [SerializeField] private int weaponSlotCount = 2;

    private NetworkList<ItemData> inventoryItems;
    public static event Action OnInventoryUpdated;

    void Awake()
    {
        inventoryItems = new NetworkList<ItemData>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            InitializeInventory();
        }
        inventoryItems.OnListChanged += OnInventoryListChanged;
    }

    public override void OnNetworkDespawn()
    {
        inventoryItems.OnListChanged -= OnInventoryListChanged;
    }

    private void InitializeInventory()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            inventoryItems.Add(new ItemData { isEmpty = true });
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddItemServerRpc(int itemID, string itemName, int quantity)
    {
        if (!IsServer) return;

        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef == null) return;

        int startIndex = (itemDef.category == ItemCategory.Weapon) ? 0 : weaponSlotCount;
        int endIndex = (itemDef.category == ItemCategory.Weapon) ? weaponSlotCount : inventorySize;

        bool itemAdded = false;

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
    }

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

                // JAVÍTVA: Hang lejátszása az új rendszeren keresztül
                PlayerSoundController soundController = GetComponent<PlayerSoundController>();
                if (soundController != null)
                {
                    // A 'false' jelzi, hogy ez egy használati (use) hang.
                    soundController.PlayItemSoundClientRpc(itemToAttackWith.itemID, false);
                }
            }
        }
    }

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
            // JAVÍTVA: Hang lejátszása az új rendszeren keresztül
            PlayerSoundController soundController = GetComponent<PlayerSoundController>();
            if (soundController != null)
            {
                soundController.PlayItemSoundClientRpc(itemToUse.itemID, false);
            }
            RemoveItemServerRpc(slotIndex, 1);
        }
    }

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

    public NetworkList<ItemData> GetInventoryItems()
    {
        return inventoryItems;
    }

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
