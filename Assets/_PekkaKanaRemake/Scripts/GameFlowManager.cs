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

    void Awake()
    {
        // JAVÍTVA: Singleton minta a duplikáció elkerülésére.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllWorldDefinitions();
    }

    private void LoadAllWorldDefinitions()
    {
        var loadedWorlds = Resources.LoadAll<WorldDefinition>("Worlds");
        allWorlds = new List<WorldDefinition>(loadedWorlds);
    }

    public List<WorldDefinition> GetAllWorlds()
    {
        return allWorlds;
    }

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
}
