using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Komponens Referenci�k")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI slotInfoText; // Pl. "Slot 1 - Rooster Island"
    [SerializeField] private TextMeshProUGUI emptySlotText;  // Pl. "�res Hely"

    /// <summary>
    /// Be�ll�tja a gombot a ment�si adatok alapj�n.
    /// </summary>
    /// <param name="slotIndex">A ment�si hely sorsz�ma.</param>
    /// <param name="data">A mentett j�t�kadatok (lehet null, ha �res).</param>
    /// <param name="onLoadAction">A m�velet, ami lefut, ha a gombra kattintanak.</param>
    public void Setup(int slotIndex, GameData data, Action<int> onLoadAction)
    {
        if (data != null)
        {
            // Ha van ment�s, ki�rjuk az adatait.
            slotInfoText.text = $"Ment�s {slotIndex + 1}\n<size=24>{data.lastSceneName} | Pont: {data.score}</size>";
            slotInfoText.gameObject.SetActive(true);
            emptySlotText.gameObject.SetActive(false);
            button.interactable = true;
        }
        else
        {
            // Ha nincs ment�s, jelezz�k, hogy a hely �res.
            slotInfoText.gameObject.SetActive(false);
            emptySlotText.text = $"Ment�s {slotIndex + 1}\n<size=24>(�res)</size>";
            emptySlotText.gameObject.SetActive(true);
            button.interactable = false; // Az �res gomb nem kattinthat�.
        }

        // Elt�vol�tjuk a r�gi listenereket, majd hozz�adjuk az �jat.
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onLoadAction?.Invoke(slotIndex));
    }
}
