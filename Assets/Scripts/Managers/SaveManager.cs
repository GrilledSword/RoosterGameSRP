using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    [Header("Beállítások")]
    [Tooltip("Hány darab mentési slot legyen elérhetõ.")]
    [SerializeField] private int saveSlotCount = 3;
    [Tooltip("Az automatikus checkpoint mentés slotjának indexe. Ez rejtett a játékos elõl.")]
    [SerializeField] private int checkpointSlotIndex = 99;

    private string saveFileName = "savegame";
    private GameData gameData;
    private List<ISaveable> saveableEntities;

    public static SaveManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void NewGame()
    {
        this.gameData = new GameData();
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex >= saveSlotCount)
        {
            Debug.LogWarning($"Mentés a {slotIndex} indexre nem engedélyezett (checkpoint lehet).");
        }

        // 1. Adatok összegyûjtése minden menthetõ entitástól
        this.saveableEntities = FindAllSaveableEntities();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.SaveData(ref gameData);
        }

        // 2. Adatok szerializálása JSON formátumba
        string dataToStore = JsonUtility.ToJson(gameData, true);

        // 3. Fájlba írás
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

    public GameData LoadGame(int slotIndex)
    {
        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Nincs mentés a {slotIndex} sloton.");
            return null;
        }

        try
        {
            string dataToLoad = File.ReadAllText(filePath);
            this.gameData = JsonUtility.FromJson<GameData>(dataToLoad);

            // Az adatok betöltése az entitásokba a jelenet betöltése UTÁN történik majd.
            return this.gameData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Hiba a betöltés során: {e.Message}");
            return null;
        }
    }

    public void ApplyLoadedData()
    {
        if (this.gameData == null) return;

        this.saveableEntities = FindAllSaveableEntities();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.LoadData(gameData);
        }
        Debug.Log("Betöltött adatok sikeresen alkalmazva.");
    }

    public void SaveCheckpoint()
    {
        SaveGame(checkpointSlotIndex);
    }

    public GameData LoadCheckpoint()
    {
        return LoadGame(checkpointSlotIndex);
    }

    private List<ISaveable> FindAllSaveableEntities()
    {
        IEnumerable<ISaveable> saveables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ISaveable>();
        return new List<ISaveable>(saveables);
    }

    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"{saveFileName}_{slotIndex}.json");
    }

    public bool SaveSlotExists(int slotIndex)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }
}
