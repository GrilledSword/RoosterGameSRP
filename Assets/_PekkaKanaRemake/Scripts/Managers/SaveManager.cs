using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Beállítások")]
    [SerializeField] private int saveSlotCount = 3;
    public int SaveSlotCount => saveSlotCount;

    public GameData CurrentlyLoadedData { get; private set; }
    private string saveFileName = "savegame";
    public bool IsLoading { get; set; } = false;

    private List<string> _permanentlyCollectedItemIds = new List<string>();
    private List<string> _collectedItemsThisLevel = new List<string>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    public void NewGame()
    {
        CurrentlyLoadedData = new GameData();
        _permanentlyCollectedItemIds.Clear();
        _collectedItemsThisLevel.Clear();
    }
    public void MarkItemAsCollected(string itemId)
    {
        if (!_collectedItemsThisLevel.Contains(itemId))
        {
            _collectedItemsThisLevel.Add(itemId);
        }
    }
    public void CommitLevelProgress()
    {
        foreach (var itemId in _collectedItemsThisLevel)
        {
            if (!_permanentlyCollectedItemIds.Contains(itemId))
            {
                _permanentlyCollectedItemIds.Add(itemId);
            }
        }
        _collectedItemsThisLevel.Clear();
    }
    public void ResetLevelProgress()
    {
        _collectedItemsThisLevel.Clear();
    }
    public void SaveGame(int slotIndex)
    {
        if (slotIndex >= saveSlotCount) return;
        CommitLevelProgress();

        GameData newSaveData = new GameData();
        if (GameFlowManager.Instance != null)
        {
            newSaveData.isMultiplayer = GameFlowManager.Instance.IsMultiplayerSession;
        }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            newSaveData.isMultiplayer = NetworkManager.Singleton.ConnectedClientsIds.Count > 1;
        }
        else
        {
            newSaveData.isMultiplayer = false;
        }

        var saveableEntities = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.SaveData(ref newSaveData);
        }

        newSaveData.lastSceneName = SceneManager.GetActiveScene().name;
        newSaveData.lastUpdated = DateTime.Now.ToString("yyyy.MM.dd HH:mm");
        newSaveData.collectedItemIds = new List<string>(_permanentlyCollectedItemIds);

        string dataToStore = JsonUtility.ToJson(newSaveData, true);
        string filePath = GetSaveFilePath(slotIndex);
        try
        {
            File.WriteAllText(filePath, dataToStore);
        }
        catch (Exception e) { Debug.LogError($"Hiba a mentés során: {e.Message}"); }
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
        catch (Exception e)
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
            CurrentlyLoadedData = loadedData;
            _permanentlyCollectedItemIds = new List<string>(loadedData.collectedItemIds);
            _collectedItemsThisLevel.Clear();
            return true;
        }
        return false;
    }
    public void ClearLoadedData()
    {
        CurrentlyLoadedData = null;
        _permanentlyCollectedItemIds.Clear();
        _collectedItemsThisLevel.Clear();
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ezt a logikát a GameFlowManager és a PlayerController kezeli, itt már nincs rá szükség.
    }
    public bool IsItemCollected(string itemId)
    {
        return _permanentlyCollectedItemIds.Contains(itemId) || _collectedItemsThisLevel.Contains(itemId);
    }
    private void ApplyLoadedData()
    {
        if (CurrentlyLoadedData == null) return;

        var saveableEntities = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.LoadData(CurrentlyLoadedData);
        }

        if (NetworkManager.Singleton.IsServer)
        {
            var allPickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
            foreach (var pickup in allPickups)
            {
                string id = pickup.GetComponent<UniqueId>().Id;
                if (_permanentlyCollectedItemIds.Contains(id))
                {
                    pickup.GetComponent<NetworkObject>().Despawn();
                }
            }
        }

        Debug.Log("Betöltött adatok sikeresen alkalmazva.");
    }
    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"{saveFileName}_{slotIndex}.json");
    }
    public void DeleteSaveData(int slotIndex)
    {
        string filePath = GetSaveFilePath(slotIndex);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"Mentési fájl sikeresen törölve: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Hiba a mentési fájl törlése közben: {e.Message}");
            }
        }
    }
}
