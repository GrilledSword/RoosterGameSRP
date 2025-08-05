using UnityEngine;
using Unity.Netcode;

// A f�jl neve: ItemPickup.cs
// A szkriptet a p�ly�n l�v�, felvehet� GameObject-ekre kell tenni.

[RequireComponent(typeof(NetworkObject))]
public class ItemPickup : MonoBehaviour // <-- �TNEVEZVE!
{
    [Header("Item Be�ll�t�sok")]
    [Tooltip("A felveend� t�rgy ID-ja.")]
    [SerializeField] private int itemID;

    [Tooltip("Mennyi darabot vegyen fel ebb�l a t�rgyb�l a j�t�kos.")]
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
                Debug.LogError($"ItemPickup Hiba: A(z) {itemID} ID-val rendelkez� t�rgy nem tal�lhat� az ItemManagerben!");
                return;
            }

            // --- UNIVERZ�LIS M�VELETEK MINDEN FELV�TELN�L ---
            // 1. Pontsz�m hozz�ad�sa (ha van)
            if (itemDef.scoreValue > 0)
            {
                playerController.AddScoreServerRpc(itemDef.scoreValue);
            }

            // 2. Felv�tel hangj�nak lej�tsz�sa
            playerController.PlayPickupSoundClientRpc(itemID);

            // --- KATEG�RIA-SPECIFIKUS M�VELETEK ---
            if (itemDef.category == ItemCategory.Coin)
            {
                // Az �rm�k nem ker�lnek az inventory-ba, itt v�gezt�nk is vel�k.
            }
            else // General �s Weapon kateg�ri�j� t�rgyak
            {
                Slots playerInventory = other.GetComponent<Slots>();
                if (playerInventory != null)
                {
                    playerInventory.AddItemServerRpc(itemID, itemDef.itemName, quantity);
                }
                else
                {
                    Debug.LogError($"A j�t�kosr�l hi�nyzik a Slots komponens! A(z) {itemDef.itemName} nem adhat� hozz�.");
                }
            }

            // --- V�GS� M�VELET ---
            // 3. A felvett objektum elt�ntet�se a p�ly�r�l
            GetComponent<NetworkObject>().Despawn();
        }
    }
}