using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item Beállítások")]
    [Tooltip("A felveendõ tárgy ID-ja.")]
    [SerializeField] private int itemID;

    [Tooltip("Mennyi darabot vegyen fel ebbõl a tárgyból a játékos.")]
    [SerializeField] private int quantity = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }


        if (other.CompareTag("Player"))
        {
            PekkaPlayerController playerController = other.GetComponent<PekkaPlayerController>();
            if (playerController == null) return;

            ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
            if (itemDef == null)
            {
                Debug.LogError($"ItemPickup Hiba: A(z) {itemID} ID-val rendelkezõ tárgy nem található az ItemManagerben!");
                return;
            }

            // --- UNIVERZÁLIS MÛVELETEK MINDEN FELVÉTELNÉL ---
            // 1. Pontszám hozzáadása (ha van)
            if (itemDef.scoreValue > 0)
            {
                playerController.AddScoreServerRpc(itemDef.scoreValue);
            }

            // 2. Felvétel hangjának lejátszása
            playerController.PlayPickupSoundClientRpc(itemID);

            // --- KATEGÓRIA-SPECIFIKUS MÛVELETEK ---
            if (itemDef.category == ItemCategory.Coin)
            {
                // Az érmék nem kerülnek az inventory-ba, itt végeztünk is velük.
            }
            else // General és Weapon kategóriájú tárgyak
            {
                Slots playerInventory = other.GetComponent<Slots>();
                if (playerInventory != null)
                {
                    playerInventory.AddItemServerRpc(itemID, itemDef.itemName, quantity);
                }
                else
                {
                    Debug.LogError($"A játékosról hiányzik a Slots komponens! A(z) {itemDef.itemName} nem adható hozzá.");
                }
            }

            // --- VÉGSÕ MÛVELET ---
            // 3. A felvett objektum eltüntetése a pályáról
            GetComponent<NetworkObject>().Despawn();
        }
    }
}