using UnityEngine;

public class LoadGameUIManager : MonoBehaviour
{
    [Header("UI Referenci�k")]
    [SerializeField] private Transform saveSlotContainer;
    [SerializeField] private GameObject saveSlotButtonPrefab;

    private MainMenuUIManager mainMenuUIManager;

    void Awake()
    {
        mainMenuUIManager = FindFirstObjectByType<MainMenuUIManager>();
    }

    void OnEnable()
    {
        RefreshSaveSlots();
    }

    public void RefreshSaveSlots()
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
                saveSlotUI.Setup(i, saveData, this);
            }
        }
    }
    public void OnSaveSlotClicked(int slotIndex)
    {
        Debug.Log($"Ment�si hely {slotIndex} bet�lt�se.");
        if (SaveManager.Instance.LoadGameData(slotIndex))
        {
            string loadedSceneName = SaveManager.Instance.CurrentlyLoadedData.lastSceneName;
            GameFlowManager.Instance.StartGameFromLoad(loadedSceneName);
        }
    }
}
