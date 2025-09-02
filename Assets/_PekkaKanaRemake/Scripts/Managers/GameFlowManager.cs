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

        // Megv�rjuk, am�g minden kliens bet�lti a jelenetet
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!clientsCompleted.Contains(clientId))
            {
                // M�g nem mindenki v�gzett, v�runk a k�vetkez� callback-re
                return;
            }
        }

        // Ha ide eljutottunk, mindenki bet�lt�tte a p�ly�t.
        // Most spawnoljuk a j�t�kosokat a megfelel� helyekre.
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

        // Miut�n minden j�t�kos a hely�n van, elind�tjuk a j�t�kot
        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        // Itt m�r csak a UI-t �s a vez�rl�st kezelj�k, a teleport�l�s kor�bban megt�rt�nt
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
            // Robusztusabb keres�s, ha a panel esetleg inakt�v
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

