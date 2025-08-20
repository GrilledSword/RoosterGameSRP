using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ItemPickup : NetworkBehaviour
{
    [Header("Item Beállítások")]
    [SerializeField] private int itemID;
    [SerializeField] private int quantity = 1;

    private UniqueId _uniqueId;
    private void Awake()
    {
        _uniqueId = GetComponent<UniqueId>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !IsSpawned) return;

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

            // Pontszám hozzáadása
            if (itemDef.scoreValue > 0)
            {
                playerController.AddScoreServerRpc(itemDef.scoreValue);
            }

            PlayerSoundController soundController = other.GetComponent<PlayerSoundController>();
            if (soundController != null)
            {
                soundController.PlayItemSoundClientRpc(itemID, true);
            }

            // Tárgy hozzáadása az inventory-hoz (ha nem érme)
            if (itemDef.category != ItemCategory.Coin)
            {
                Slots playerInventory = other.GetComponent<Slots>();
                if (playerInventory != null)
                {
                    playerInventory.AddItemServerRpc(itemID, itemDef.itemName, quantity);
                }
            }
            if (SaveManager.Instance != null && _uniqueId != null)
            {
                SaveManager.Instance.MarkItemAsCollected(_uniqueId.Id);
            }

            // Objektum eltüntetése
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
