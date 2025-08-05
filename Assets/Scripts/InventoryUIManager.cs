using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    [Tooltip("UI slot GameObjectek listája.")]
    public List<GameObject> uiSlots;
    private PekkaPlayerController localPlayerController;
    private Slots localPlayerSlots;

    void OnEnable()
    {
        FindLocalPlayerAndSlots();
        Slots.OnInventoryUpdated += UpdateUI;
    }

    void OnDisable()
    {
        Slots.OnInventoryUpdated -= UpdateUI;
    }

    private void FindLocalPlayerAndSlots()
    {
        if (localPlayerController == null || localPlayerSlots == null)
        {
            PekkaPlayerController[] players = FindObjectsByType<PekkaPlayerController>(FindObjectsSortMode.None);
            foreach (PekkaPlayerController player in players)
            {
                if (player.IsOwner)
                {
                    localPlayerController = player;
                    localPlayerSlots = player.GetComponent<Slots>();
                    break;
                }
            }
        }
    }

    void UpdateUI()
    {
        if (localPlayerSlots == null)
        {
            FindLocalPlayerAndSlots();
            if (localPlayerSlots == null) return;
        }

        NetworkList<ItemData> items = localPlayerSlots.GetInventoryItems();

        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < items.Count)
            {
                ItemData item = items[i];
                GameObject uiSlot = uiSlots[i];

                Image itemIconImage = uiSlot.GetComponent<Image>();
                TextMeshProUGUI itemQuantityText = uiSlot.GetComponentInChildren<TextMeshProUGUI>();

                if (item.isEmpty)
                {
                    if (itemIconImage != null)
                    {
                        itemIconImage.sprite = null;
                        itemIconImage.enabled = false;
                    }
                    if (itemQuantityText != null) itemQuantityText.text = "";
                }
                else
                {
                    ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(item.itemID);
                    if (itemDef != null && itemIconImage != null)
                    {
                        itemIconImage.sprite = itemDef.itemIcon;
                        itemIconImage.enabled = true;
                    }
                    if (itemQuantityText != null)
                    {
                        itemQuantityText.text = item.quantity > 1 ? item.quantity.ToString() : "";
                    }
                }
            }
        }
    }
}