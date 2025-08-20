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

    [Header("Be�ll�t�sok")]
    [SerializeField] private int saveSlotCount = 3;
    public int SaveSlotCount => saveSlotCount;

    public GameData CurrentlyLoadedData { get; private set; }
    private string saveFileName = "savegame";
    public bool IsLoading { get; set; } = false;

    // �J: Ez a lista gy�jti a felvett t�rgyak azonos�t�it a ment�sig.
    private List<string> _collectedItemsInSession = new List<string>();

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
        _collectedItemsInSession.Clear();
    }

    // �J: Ezt a met�dust h�vja az ItemPickup.
    public void MarkItemAsCollected(string itemId)
    {
        if (!_collectedItemsInSession.Contains(itemId))
        {
            _collectedItemsInSession.Add(itemId);
        }
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex >= saveSlotCount) return;

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

        // JAV�TVA: A felvett t�rgyak list�j�t is elmentj�k.
        newSaveData.collectedItemIds = new List<string>(_collectedItemsInSession);

        // ... (a t�bbi ment�si logika, pl. completedLevelIds) ...

        string dataToStore = JsonUtility.ToJson(newSaveData, true);
        string filePath = GetSaveFilePath(slotIndex);
        try
        {
            File.WriteAllText(filePath, dataToStore);
        }
        catch (Exception e) { Debug.LogError($"Hiba a ment�s sor�n: {e.Message}"); }
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
            // Bet�ltj�k a felvett t�rgyak list�j�t is.
            _collectedItemsInSession = new List<string>(loadedData.collectedItemIds);
            return true;
        }
        return false;
    }

    public void ClearLoadedData()
    {
        CurrentlyLoadedData = null;
        _collectedItemsInSession.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // JAV�TVA: Ezt a logik�t �thelyezt�k a PlayerController-be,
        // hogy a megfelel� id�ben fusson le. Itt m�r nincs r� sz�ks�g.
        // if (IsLoading)
        // {
        //     ApplyLoadedData();
        //     IsLoading = false;
        // }
    }

    private void ApplyLoadedData()
    {
        if (CurrentlyLoadedData == null) return;

        // 1. A j�t�kos �s egy�b menthet� adatok vissza�ll�t�sa.
        var saveableEntities = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.LoadData(CurrentlyLoadedData);
        }

        // 2. A m�r felvett t�rgyak elt�ntet�se.
        // Ezt csak a szerver teheti meg.
        if (NetworkManager.Singleton.IsServer)
        {
            var allPickups = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
            foreach (var pickup in allPickups)
            {
                string id = pickup.GetComponent<UniqueId>().Id;
                if (CurrentlyLoadedData.collectedItemIds.Contains(id))
                {
                    // Ha a t�rgy ID-ja a mentett list�ban van, despawnoljuk.
                    pickup.GetComponent<NetworkObject>().Despawn();
                }
            }
        }

        Debug.Log("Bet�lt�tt adatok sikeresen alkalmazva.");
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
                Debug.Log($"Ment�si f�jl sikeresen t�r�lve: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Hiba a ment�si f�jl t�rl�se k�zben: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"A(z) {filePath} ment�si f�jl nem l�tezik, nincs mit t�r�lni.");
        }
    }
}
