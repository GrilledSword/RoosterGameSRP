using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Collections; // Szükséges a FixedString-hez

/// <summary>
/// A játékmenet folyamatát és a játékosok haladását kezelõ központi, hálózati menedzser.
/// Singleton, ami a Main Menu után is megmarad.
/// </summary>
public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    // A jelenleg kiválasztott világ azonosítója.
    private NetworkVariable<FixedString32Bytes> currentWorldId = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // JAVÍTVA: A NetworkList-et a deklarációnál inicializáljuk, nem az Awake-ben.
    // Ez a lista tárolja a teljesített pályák azonosítóit.
    public NetworkList<FixedString32Bytes> CompletedLevelIds { get; private set; } = new NetworkList<FixedString32Bytes>();

    // Az összes elérhetõ világ definícióját tárolja, amit a Resources mappából töltünk be.
    private List<WorldDefinition> allWorlds;

    void Awake()
    {
        // Singleton minta biztosítása
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllWorldDefinitions();
    }

    /// <summary>
    /// Betölti az összes WorldDefinition ScriptableObject-et a "Resources/Worlds" mappából.
    /// </summary>
    private void LoadAllWorldDefinitions()
    {
        // A betöltés most már robusztusabb, nem okoz hibát, ha a mappa üres.
        var loadedWorlds = Resources.LoadAll<WorldDefinition>("Worlds");
        allWorlds = new List<WorldDefinition>(loadedWorlds);

        if (allWorlds.Count == 0)
        {
            Debug.LogWarning("GameFlowManager: Nem található egyetlen WorldDefinition sem a 'Resources/Worlds' mappában!");
        }
        else
        {
            Debug.Log($"GameFlowManager: Betöltve {allWorlds.Count} világ definíció.");
        }
    }

    /// <summary>
    /// Visszaadja az összes betöltött világ listáját. A UI használja a gombok generálásához.
    /// </summary>
    public List<WorldDefinition> GetAllWorlds()
    {
        return allWorlds;
    }

    /// <summary>
    /// Visszaadja az aktuálisan kiválasztott világ definícióját az ID alapján.
    /// </summary>
    public WorldDefinition GetCurrentWorldDefinition()
    {
        if (string.IsNullOrEmpty(currentWorldId.Value.ToString())) return null;

        foreach (var world in allWorlds)
        {
            if (world.worldId == currentWorldId.Value.ToString())
            {
                return world;
            }
        }
        return null;
    }

    /// <summary>
    /// [ServerRpc] A Host hívja meg a világválasztó képernyõrõl.
    /// Beállítja az aktuális világot és betölti a világtérképet minden kliens számára.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void SelectWorldServerRpc(string worldId)
    {
        this.currentWorldId.Value = new FixedString32Bytes(worldId);

        WorldDefinition worldToLoad = GetCurrentWorldDefinition();
        if (worldToLoad != null && !string.IsNullOrEmpty(worldToLoad.worldMapSceneName))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(worldToLoad.worldMapSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError($"A(z) '{worldId}' világhoz nem tartozik érvényes térkép jelenet!");
        }
    }

    /// <summary>
    /// [ServerRpc] A Host hívja meg, amikor egy pálya elindul a világtérképrõl.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void StartLevelServerRpc(string levelSceneName)
    {
        if (!string.IsNullOrEmpty(levelSceneName))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(levelSceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// [ServerRpc] A Host hívja meg, amikor egy pálya teljesült.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void CompleteLevelServerRpc(string levelId)
    {
        var fixedLevelId = new FixedString32Bytes(levelId);
        if (!CompletedLevelIds.Contains(fixedLevelId))
        {
            CompletedLevelIds.Add(fixedLevelId);
        }

        // Visszatöltjük a világtérképet.
        WorldDefinition currentWorld = GetCurrentWorldDefinition();
        if (currentWorld != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(currentWorld.worldMapSceneName, LoadSceneMode.Single);
        }
    }
}
