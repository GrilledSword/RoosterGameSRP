using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI Referenciák")]
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

        // A gombok eseménykezelõinek beállítása.
        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }

        if (data != null)
        {
            // Ha van mentési adat, feltöltjük a mezõket.
            slotNameText.text = $"Mentés {slotIndex + 1}";
            lastSavedText.text = string.IsNullOrEmpty(data.lastUpdated) ? "" : $"Mentve: {data.lastUpdated}";
            if (gameModeText != null)
            {
                gameModeText.text = data.isMultiplayer ? "Multiplayer" : "Singleplayer";
            }

            slotButton.interactable = true;

            // A törlés gombot csak akkor jelenítjük meg, ha van mit törölni.
            if (deleteButton != null) deleteButton.gameObject.SetActive(true);
        }
        else
        {
            // Ha a slot üres, alapértelmezett szövegeket írunk ki.
            slotNameText.text = "Üres Hely";
            lastSavedText.text = "";
            if (gameModeText != null) gameModeText.text = "";

            slotButton.interactable = false;

            // Ha nincs mentés, a törlés gombot elrejtjük.
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
    /// Akkor hívódik meg, ha a játékos a törlés gombra kattint.
    /// </summary>
    private void OnDeleteButtonClicked()
    {
        // Elõször töröljük a mentési fájlt.
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSaveData(_slotIndex);
        }

        // Ezután frissítjük a teljes UI listát, hogy az eltûnt mentés helyén
        // már az "Üres Hely" felirat jelenjen meg.
        if (_uiManager != null)
        {
            _uiManager.RefreshSaveSlots();
        }
    }
}
