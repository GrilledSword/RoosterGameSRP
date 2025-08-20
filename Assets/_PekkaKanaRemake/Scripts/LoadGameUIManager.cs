using UnityEngine;
using TMPro;

public class LoadGameUIManager : MonoBehaviour
{
    [Header("UI Referenciák")]
    [Tooltip("A mentési slot gombokat tartalmazó szülõ objektum.")]
    [SerializeField] private Transform saveSlotContainer;
    [Tooltip("A mentési slot gomb prefabja.")]
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
            Debug.LogError("SaveManager.Instance nem található!");
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
