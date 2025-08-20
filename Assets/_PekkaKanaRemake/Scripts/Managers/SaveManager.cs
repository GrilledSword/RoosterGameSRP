using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Be�ll�t�sok")]
    [SerializeField] private int saveSlotCount = 3;
    public int SaveSlotCount => saveSlotCount;

    public GameData CurrentlyLoadedData { get; private set; }
    private string saveFileName = "savegame";
    private bool isLoading = false;

    void Awake()
    {
        // JAV�TVA: Singleton minta a duplik�ci� elker�l�s�re.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void NewGame()
    {
        CurrentlyLoadedData = new GameData();
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex >= saveSlotCount)
        {
            Debug.LogWarning($"Ment�s a {slotIndex} indexre nem enged�lyezett.");
            return;
        }

        var saveableEntities = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        GameData newSaveData = new GameData();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.SaveData(ref newSaveData);
        }

        newSaveData.lastSceneName = SceneManager.GetActiveScene().name;

        if (GameFlowManager.Instance != null)
        {
            newSaveData.completedLevelIds = new List<string>();
            foreach (var id in GameFlowManager.Instance.CompletedLevelIds)
            {
                newSaveData.completedLevelIds.Add(id.ToString());
            }
        }

        string dataToStore = JsonUtility.ToJson(newSaveData, true);
        string filePath = GetSaveFilePath(slotIndex);
        try
        {
            File.WriteAllText(filePath, dataToStore);
            Debug.Log($"J�t�k sikeresen mentve ide: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Hiba a ment�s sor�n: {e.Message}");
        }
    }

    public GameData LoadGameDataFromFile(int slotIndex)
    {
        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath)) return null;

        try
        {
            string dataToLoad = File.ReadAllText(filePath);
            return JsonUtility.FromJson<GameData>(dataToLoad);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Hiba a bet�lt�s sor�n: {e.Message}");
            return null;
        }
    }

    public bool LoadGameData(int slotIndex)
    {
        GameData loadedData = LoadGameDataFromFile(slotIndex);
        if (loadedData != null)
        {
            CurrentlyLoadedData = loadedData;
            return true;
        }
        return false;
    }

    public void ClearLoadedData()
    {
        CurrentlyLoadedData = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isLoading)
        {
            ApplyLoadedData();
            isLoading = false;
        }
    }

    private void ApplyLoadedData()
    {
        if (CurrentlyLoadedData == null) return;
        var saveableEntities = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.LoadData(CurrentlyLoadedData);
        }
        Debug.Log("Bet�lt�tt adatok sikeresen alkalmazva.");
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"{saveFileName}_{slotIndex}.json");
    }
}
