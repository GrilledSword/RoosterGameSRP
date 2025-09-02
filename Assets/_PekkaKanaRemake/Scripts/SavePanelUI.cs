using UnityEngine;
using TMPro;

public class SavePanelUI : MonoBehaviour
{
    [Header("UI Elemek")]
    [SerializeField] private TMP_InputField saveNameInputField;
    [SerializeField] private InGameMenuUI inGameMenuUI;

    private int _currentSlotIndex = 0;

    public void OnSaveButtonClicked()
    {
        string saveName = saveNameInputField.text;
        SaveManager.Instance.SaveGame(_currentSlotIndex, saveName);

        if (inGameMenuUI != null)
        {
            inGameMenuUI.ShowFeedback("Játék mentve!");
            inGameMenuUI.OnBackFromSaveButtonClicked();
        }
    }
    public void SetSlotIndex(int slotIndex)
    {
        _currentSlotIndex = slotIndex;
    }
}