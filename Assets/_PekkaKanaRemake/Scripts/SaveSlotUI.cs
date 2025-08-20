using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI Referenci�k")]
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI lastSavedText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private Button slotButton;
    [SerializeField] private Button deleteButton;

    private int _slotIndex;
    private LoadGameUIManager _uiManager;

    public void Setup(int slotIndex, GameData data, LoadGameUIManager uiManager)
    {
        _slotIndex = slotIndex;
        _uiManager = uiManager;

        // A gombok esem�nykezel�inek be�ll�t�sa.
        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }

        if (data != null)
        {
            // Ha van ment�si adat, felt�ltj�k a mez�ket.
            slotNameText.text = $"Ment�s {slotIndex + 1}";
            lastSavedText.text = string.IsNullOrEmpty(data.lastUpdated) ? "" : $"Mentve: {data.lastUpdated}";
            if (gameModeText != null)
            {
                gameModeText.text = data.isMultiplayer ? "Multiplayer" : "Singleplayer";
            }

            slotButton.interactable = true;

            // A t�rl�s gombot csak akkor jelen�tj�k meg, ha van mit t�r�lni.
            if (deleteButton != null) deleteButton.gameObject.SetActive(true);
        }
        else
        {
            // Ha a slot �res, alap�rtelmezett sz�vegeket �runk ki.
            slotNameText.text = "�res Hely";
            lastSavedText.text = "";
            if (gameModeText != null) gameModeText.text = "";

            slotButton.interactable = false;

            // Ha nincs ment�s, a t�rl�s gombot elrejtj�k.
            if (deleteButton != null) deleteButton.gameObject.SetActive(false);
        }
    }

    private void OnSlotClicked()
    {
        if (_uiManager != null)
        {
            _uiManager.OnSaveSlotClicked(_slotIndex);
        }
    }

    /// <summary>
    /// Akkor h�v�dik meg, ha a j�t�kos a t�rl�s gombra kattint.
    /// </summary>
    private void OnDeleteButtonClicked()
    {
        // El�sz�r t�r�lj�k a ment�si f�jlt.
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSaveData(_slotIndex);
        }

        // Ezut�n friss�tj�k a teljes UI list�t, hogy az elt�nt ment�s hely�n
        // m�r az "�res Hely" felirat jelenjen meg.
        if (_uiManager != null)
        {
            _uiManager.RefreshSaveSlots();
        }
    }
}
