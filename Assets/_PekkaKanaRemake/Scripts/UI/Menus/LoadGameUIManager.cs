using UnityEngine;

public class LoadGameUIManager : MonoBehaviour
{
    [Header("UI Referenciák")]
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
                saveSlotUI.Setup(i, saveData, this);
            }
        }
    }
    public void OnSaveSlotClicked(int slotIndex)
    {
        if (mainMenuUIManager != null)
        {
            mainMenuUIManager.StartLoadFlow(slotIndex);
        }
        else
        {
            Debug.LogError("MainMenuUIManager referencia nincs beállítva!");
        }
    }
}
