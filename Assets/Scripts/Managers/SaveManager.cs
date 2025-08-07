using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    [Header("Be�ll�t�sok")]
    [Tooltip("H�ny darab ment�si slot legyen el�rhet�.")]
    [SerializeField] private int saveSlotCount = 3;
    [Tooltip("Az automatikus checkpoint ment�s slotj�nak indexe. Ez rejtett a j�t�kos el�l.")]
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
            Debug.LogWarning($"Ment�s a {slotIndex} indexre nem enged�lyezett (checkpoint lehet).");
        }

        // 1. Adatok �sszegy�jt�se minden menthet� entit�st�l
        this.saveableEntities = FindAllSaveableEntities();
        foreach (ISaveable saveable in saveableEntities)
        {
            saveable.SaveData(ref gameData);
        }

        // 2. Adatok szerializ�l�sa JSON form�tumba
        string dataToStore = JsonUtility.ToJson(gameData, true);

        // 3. F�jlba �r�s
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

    public GameData LoadGame(int slotIndex)
    {
        string filePath = GetSaveFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Nincs ment�s a {slotIndex} sloton.");
            return null;
        }

        try
        {
            string dataToLoad = File.ReadAllText(filePath);
            this.gameData = JsonUtility.FromJson<GameData>(dataToLoad);

            // Az adatok bet�lt�se az entit�sokba a jelenet bet�lt�se UT�N t�rt�nik majd.
            return this.gameData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Hiba a bet�lt�s sor�n: {e.Message}");
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
        Debug.Log("Bet�lt�tt adatok sikeresen alkalmazva.");
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
