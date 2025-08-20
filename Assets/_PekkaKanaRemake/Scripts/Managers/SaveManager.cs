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
    [SerializeField] private int checkpointSlotIndex = 99;

    public GameData CurrentlyLoadedData { get; private set; }
    private string saveFileName = "savegame";
    private List<ISaveable> saveableEntities;
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

    // A OnEnable/OnDisable a legbiztosabb a feliratkoz�shoz DontDestroyOnLoad objektumokn�l.
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
            Debug.LogWarning($"Ment�s a {slotIndex} indexre nem enged�lyezett.");
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
        Debug.Log("Bet�lt�tt adatok sikeresen alkalmazva.");
    }

    private List<ISaveable> FindAllSaveableEntities()
    {
        // JAV�TVA: Elavult met�dus cser�je
        return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>().ToList();
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"{saveFileName}_{slotIndex}.json");
    }
}
