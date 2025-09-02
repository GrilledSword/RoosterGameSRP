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

    private NetworkVariable<FixedString32Bytes> currentWorldId = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkList<FixedString32Bytes> CompletedLevelIds { get; private set; } = new NetworkList<FixedString32Bytes>();
    private List<WorldDefinition> allWorlds;
    [SerializeField] private GameObject loadingScreenPanel;
    public bool IsMultiplayerSession { get; private set; } = false;
    private bool _isLoadingFromSave = false;
    private LevelNodeDefinition _selectedLevel;

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
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
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
        if (_isLoadingFromSave && SaveManager.Instance?.CurrentlyLoadedData != null)
        {
            var saveData = SaveManager.Instance.CurrentlyLoadedData;
            var saveableEntities = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
            foreach (ISaveable entity in saveableEntities)
            {
                entity.LoadData(saveData);
            }
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject != null && saveData.playersData.TryGetValue(clientId.ToString(), out PlayerData playerData))
                {
                    var playerController = playerObject.GetComponent<PekkaPlayerController>();
                    if (playerController != null)
                    {
                        playerController.TeleportPlayerClientRpc(playerData.position);
                    }
                }
            }

            _isLoadingFromSave = false;
        }
        else
        {
            PositionPlayersAtSpawnPoints();
        }
        StartGameClientRpc();
    }
    private void PositionPlayersAtSpawnPoints()
    {
        if (!IsServer) return;

        var spawnManager = FindFirstObjectByType<SpawnManager>();
        if (spawnManager != null)
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject != null)
                {
                    Transform spawnPoint = spawnManager.GetNextSpawnPoint();
                    var playerController = playerObject.GetComponent<PekkaPlayerController>();
                    if (playerController != null)
                    {
                        playerController.TeleportPlayerClientRpc(spawnPoint.position);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("SpawnManager not found in the current scene. Players will not be moved to spawn points.");
        }
    }
    [ClientRpc]
    private void StartGameClientRpc()
    {
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            PekkaPlayerController localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PekkaPlayerController>();
            if (localPlayer != null)
            {
                localPlayer.SetPlayerControlActive(true);
            }
        }
        ShowLoadingScreen(false);
    }
    public void StartGameFromLoad(string sceneName)
    {
        _isLoadingFromSave = true;
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    public void SetSelectedLevel(LevelNodeDefinition level)
    {
        _selectedLevel = level;
    }
    public LevelNodeDefinition GetSelectedLevel()
    {
        return _selectedLevel;
    }
    private void ShowLoadingScreen(bool show)
    {
        if (loadingScreenPanel == null)
        {
            loadingScreenPanel = GameObject.Find("LoadingScreenPanel");
        }

        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(show);
        }
        else if (show)
        {
            Debug.LogError("LoadingScreenPanel not found in the scene!");
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
    public void StartLevelServerRpc(string levelSceneName)
    {
        if (!string.IsNullOrEmpty(levelSceneName))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(levelSceneName, LoadSceneMode.Single);
        }
    }
    [ServerRpc]
    public void CompleteLevelServerRpc(string levelId)
    {
        var fixedLevelId = new FixedString32Bytes(levelId);
        if (!CompletedLevelIds.Contains(fixedLevelId))
        {
            CompletedLevelIds.Add(fixedLevelId);
        }
    }
    public void StartSingleplayerGame(string sceneName)
    {
        IsMultiplayerSession = false;
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    public void StartMultiplayerGameAsHost(string sceneName)
    {
        IsMultiplayerSession = true;
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    public void LoadLevelAsHost(string sceneName)
    {
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            StartSingleplayerGame(sceneName);
        }
    }
}

