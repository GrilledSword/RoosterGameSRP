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

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }

        if (data != null)
        {
            slotNameText.text = string.IsNullOrWhiteSpace(data.saveName) ? $"Mentés {slotIndex + 1}" : data.saveName;

            lastSavedText.text = string.IsNullOrEmpty(data.lastUpdated) ? "" : $"Mentve: {data.lastUpdated}";
            if (gameModeText != null)
            {
                gameModeText.text = data.isMultiplayer ? "Multiplayer" : "Singleplayer";
            }

            slotButton.interactable = true;
            if (deleteButton != null) deleteButton.gameObject.SetActive(true);
        }
        else
        {
            slotNameText.text = "Üres Hely";
            lastSavedText.text = "";
            if (gameModeText != null) gameModeText.text = "";

            slotButton.interactable = false;

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
    private void OnDeleteButtonClicked()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSaveData(_slotIndex);
        }

        if (_uiManager != null)
        {
            _uiManager.RefreshSaveSlots();
        }
    }
}