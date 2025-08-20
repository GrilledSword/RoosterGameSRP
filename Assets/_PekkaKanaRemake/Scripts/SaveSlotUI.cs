using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Komponens Referenciák")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI slotInfoText; // Pl. "Slot 1 - Rooster Island"
    [SerializeField] private TextMeshProUGUI emptySlotText;  // Pl. "Üres Hely"

    /// <summary>
    /// Beállítja a gombot a mentési adatok alapján.
    /// </summary>
    /// <param name="slotIndex">A mentési hely sorszáma.</param>
    /// <param name="data">A mentett játékadatok (lehet null, ha üres).</param>
    /// <param name="onLoadAction">A mûvelet, ami lefut, ha a gombra kattintanak.</param>
    public void Setup(int slotIndex, GameData data, Action<int> onLoadAction)
    {
        if (data != null)
        {
            // Ha van mentés, kiírjuk az adatait.
            slotInfoText.text = $"Mentés {slotIndex + 1}\n<size=24>{data.lastSceneName} | Pont: {data.score}</size>";
            slotInfoText.gameObject.SetActive(true);
            emptySlotText.gameObject.SetActive(false);
            button.interactable = true;
        }
        else
        {
            // Ha nincs mentés, jelezzük, hogy a hely üres.
            slotInfoText.gameObject.SetActive(false);
            emptySlotText.text = $"Mentés {slotIndex + 1}\n<size=24>(Üres)</size>";
            emptySlotText.gameObject.SetActive(true);
            button.interactable = false; // Az üres gomb nem kattintható.
        }

        // Eltávolítjuk a régi listenereket, majd hozzáadjuk az újat.
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onLoadAction?.Invoke(slotIndex));
    }
}
