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

            // Pontszám hozzáadása
            if (itemDef.scoreValue > 0)
            {
                playerController.AddScoreServerRpc(itemDef.scoreValue);
            }

            // JAVÍTVA: Hang lejátszása az új rendszeren keresztül
            PlayerSoundController soundController = other.GetComponent<PlayerSoundController>();
            if (soundController != null)
            {
                // A soundController-nek szólunk, hogy játssza le a hangot minden kliensen.
                // Az 'true' jelzi, hogy ez egy felvételi (pickup) hang.
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

            // Objektum eltüntetése
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
