using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    private NetworkVariable<FixedString32Bytes> currentWorldId = new NetworkVariable<FixedString32Bytes>();
    public NetworkList<FixedString32Bytes> CompletedLevelIds { get; private set; } = new NetworkList<FixedString32Bytes>();
    private List<WorldDefinition> allWorlds;
    [SerializeField] private GameObject loadingScreenPanel;
    public bool IsMultiplayerSession { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllWorldDefinitions();
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            LevelNodeDefinition levelDef = FindLevelBySceneName(sceneName);
            if (levelDef != null)
            {
                levelManager.InitializeLevel(levelDef);
            }
        }

        if (SpawnManager.Instance != null && !sceneName.Contains("MapScene") && sceneName != "MainMenuScene")
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject != null && playerObject.TryGetComponent<PekkaPlayerController>(out var playerController))
                {
                    Transform spawnPoint = SpawnManager.Instance.GetNextSpawnPoint();
                    playerController.TeleportPlayerClientRpc(spawnPoint.position);
                }
            }
        }
        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        ShowLoadingScreen(false);
    }

    private void ShowLoadingScreen(bool show)
    {
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(show);
        }
    }

    private void LoadAllWorldDefinitions()
    {
        allWorlds = new List<WorldDefinition>(Resources.LoadAll<WorldDefinition>("Worlds"));
    }
    public List<WorldDefinition> GetAllWorlds() => allWorlds;
    public WorldDefinition GetCurrentWorldDefinition()
    {
        if (string.IsNullOrEmpty(currentWorldId.Value.ToString())) return null;
        return allWorlds.FirstOrDefault(world => world.worldId == currentWorldId.Value.ToString());
    }

    public void StartSingleplayerGame(string sceneName)
    {
        ResetProgress();
        IsMultiplayerSession = false;
        Debug.Log("Starting singleplayer game...");
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void StartMultiplayerGameAsHost(string sceneName)
    {
        ResetProgress();
        IsMultiplayerSession = true;
        Debug.Log("Starting multiplayer game as host...");
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    public void ResetProgress()
    {
        CompletedLevelIds.Clear();
        currentWorldId.Value = new FixedString32Bytes();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartLevelServerRpc(string levelSceneName)
    {
        if (!string.IsNullOrEmpty(levelSceneName))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(levelSceneName, LoadSceneMode.Single);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void SelectWorldServerRpc(string worldId)
    {
        this.currentWorldId.Value = new FixedString32Bytes(worldId);
        WorldDefinition worldToLoad = GetCurrentWorldDefinition();
        if (worldToLoad != null && !string.IsNullOrEmpty(worldToLoad.worldMapSceneName))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(worldToLoad.worldMapSceneName, LoadSceneMode.Single);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void CompleteLevelServerRpc(string levelId)
    {
        var fixedLevelId = new FixedString32Bytes(levelId);
        if (!CompletedLevelIds.Contains(fixedLevelId))
        {
            CompletedLevelIds.Add(fixedLevelId);
        }
    }

    public void ReturnToWorldMap()
    {
        if (!IsServer) return;
        WorldDefinition currentWorld = GetCurrentWorldDefinition();
        if (currentWorld != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(currentWorld.worldMapSceneName, LoadSceneMode.Single);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void ApplyLoadedProgressServerRpc(FixedString32Bytes[] completedIds)
    {
        CompletedLevelIds.Clear();
        foreach (var id in completedIds)
        {
            CompletedLevelIds.Add(id);
        }
    }

    private LevelNodeDefinition FindLevelBySceneName(string sceneName)
    {
        foreach (var world in allWorlds)
        {
            foreach (var level in world.levels)
            {
                if (level.levelSceneName == sceneName) return level;
            }
        }
        return null;
    }
}

