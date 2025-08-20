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
        // T�r�lj�k a r�gi gombokat.
        foreach (Transform child in saveSlotContainer)
        {
            Destroy(child.gameObject);
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager.Instance nem tal�lhat�!");
            return;
        }

        // �jra l�trehozzuk a gombokat a friss adatok alapj�n.
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

    // JAV�TVA: Hozz�adtuk a hi�nyz� met�dust, amit a SaveSlotUI h�v, amikor r�kattintanak.
    public void OnSaveSlotClicked(int slotIndex)
    {
        if (mainMenuUIManager != null)
        {
            mainMenuUIManager.StartLoadFlow(slotIndex);
        }
        else
        {
            Debug.LogError("MainMenuUIManager referencia nincs be�ll�tva!");
        }
    }
}
