using UnityEngine;
using TMPro;

public class LoadGameUIManager : MonoBehaviour
{
    [Header("UI Referenci�k")]
    [Tooltip("A ment�si slot gombokat tartalmaz� sz�l� objektum.")]
    [SerializeField] private Transform saveSlotContainer;
    [Tooltip("A ment�si slot gomb prefabja.")]
    [SerializeField] private GameObject saveSlotButtonPrefab;

    private MainMenuUIManager mainMenuUIManager;

    void Awake()
    {
        mainMenuUIManager = FindFirstObjectByType<MainMenuUIManager>();
    }
    void OnEnable()
    {
        PopulateSaveSlots();
    }
    private void PopulateSaveSlots()
    {
        foreach (Transform child in saveSlotContainer)
        {
            Destroy(child.gameObject);
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager.Instance nem tal�lhat�!");
            return;
        }
        for (int i = 0; i < SaveManager.Instance.SaveSlotCount; i++)
        {
            GameObject buttonGO = Instantiate(saveSlotButtonPrefab, saveSlotContainer);
            SaveSlotUI saveSlotUI = buttonGO.GetComponent<SaveSlotUI>();

            if (saveSlotUI != null)
            {
                GameData saveData = SaveManager.Instance.LoadGameDataFromFile(i);
                saveSlotUI.Setup(i, saveData, (slotIndex) => {
                    mainMenuUIManager.StartLoadFlow(slotIndex);
                });
            }
        }
    }
}
