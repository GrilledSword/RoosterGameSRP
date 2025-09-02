using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System.Linq;

public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    private NetworkVariable<FixedString32Bytes> currentWorldId = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkList<FixedString32Bytes> CompletedLevelIds { get; private set; } = new NetworkList<FixedString32Bytes>();
    private List<WorldDefinition> allWorlds;
    [SerializeField] private GameObject loadingScreenPanel;
    public bool IsMultiplayerSession { get; private set; } = false;
    public static LevelNodeDefinition SelectedLevel { get; private set; }

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
        ShowLoadingScreen(true);
        if (!IsServer) return;

        // Megvárjuk, amíg minden kliens betölti a jelenetet
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!clientsCompleted.Contains(clientId))
            {
                // Még nem mindenki végzett, várunk a következõ callback-re
                return;
            }
        }

        // Ha ide eljutottunk, mindenki betöltötte a pályát.
        // Most spawnoljuk a játékosokat a megfelelõ helyekre.
        if (SpawnManager.Instance != null && sceneName != "MainMenuScene" && sceneName != "World1_MapScene")
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

        // Miután minden játékos a helyén van, elindítjuk a játékot
        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        // Itt már csak a UI-t és a vezérlést kezeljük, a teleportálás korábban megtörtént
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
    public void SetSelectedLevel(LevelNodeDefinition levelData)
    {
        SelectedLevel = levelData;
    }

    private void ShowLoadingScreen(bool show)
    {
        if (loadingScreenPanel == null)
        {
            // Robusztusabb keresés, ha a panel esetleg inaktív
            loadingScreenPanel = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g.name == "LoadingScreenPanel");
        }

        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(show);
        }
        else if (show)
        {
            Debug.LogError("LoadingScreenPanel not found in the project!");
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
    public void StartLevelServerRpc(string levelSceneName)
    {
        if (!string.IsNullOrEmpty(levelSceneName))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(levelSceneName, LoadSceneMode.Single);
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
        WorldDefinition currentWorld = GetCurrentWorldDefinition();
        if (currentWorld != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(currentWorld.worldMapSceneName, LoadSceneMode.Single);
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

