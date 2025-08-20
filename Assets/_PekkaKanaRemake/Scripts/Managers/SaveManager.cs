using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Beállítások")]
    [SerializeField] private int saveSlotCount = 3;
    public int SaveSlotCount => saveSlotCount;
    [SerializeField] private int checkpointSlotIndex = 99;

    public GameData CurrentlyLoadedData { get; private set; }
    private string saveFileName = "savegame";
    private List<ISaveable> saveableEntities;
    private bool isLoading = false;

    void Awake()
    {
        // JAVÍTVA: Singleton minta a duplikáció elkerülésére.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // A OnEnable/OnDisable a legbiztosabb a feliratkozáshoz DontDestroyOnLoad objektumoknál.
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
        this.CurrentlyLoadedData = new GameData();
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex >= saveSlotCount && slotIndex != checkpointSlotIndex)
        {
            Debug.LogWarning($"Mentés a {slotIndex} indexre nem engedélyezett.");
            return;
        }

        this.saveableEntities = FindAllSaveableEntities();
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
            Debug.Log($"Játék sikeresen mentve ide: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Hiba a mentés során: {e.Message}");
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
            Debug.LogError($"Hiba a betöltés során: {e.Message}");
            return null;
        }
    }

    public bool LoadGameData(int slotIndex)
    {
        GameData loadedData = LoadGameDataFromFile(slotIndex);
        if (loadedData != null)
        {
            this.CurrentlyLoadedData = loadedData;
            return true;
        }
        return false;
    }

    public void ClearLoadedData()
    {
        CurrentlyLoadedData = null;
    }

    public void StartLoadingProcess(GameData data)
    {
        this.CurrentlyLoadedData = data;
        this.isLoading = true;

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(CurrentlyLoadedData.lastSceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(CurrentlyLoadedData.lastSceneName);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this.isLoading)
        {
            ApplyLoadedData();
            this.isLoading = false;
        }
    }

    private void ApplyLoadedData()
    {
        if (this.CurrentlyLoadedData == null) return;

        this.saveableEntities = FindAllSaveableEntities();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.LoadData(CurrentlyLoadedData);
        }
        Debug.Log("Betöltött adatok sikeresen alkalmazva.");
    }

    private List<ISaveable> FindAllSaveableEntities()
    {
        // JAVÍTVA: Elavult metódus cseréje
        return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>().ToList();
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"{saveFileName}_{slotIndex}.json");
    }
}
