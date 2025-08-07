using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Collections; // Sz�ks�ges a FixedString-hez

/// <summary>
/// A j�t�kmenet folyamat�t �s a j�t�kosok halad�s�t kezel� k�zponti, h�l�zati menedzser.
/// Singleton, ami a Main Menu ut�n is megmarad.
/// </summary>
public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    // A jelenleg kiv�lasztott vil�g azonos�t�ja.
    private NetworkVariable<FixedString32Bytes> currentWorldId = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // JAV�TVA: A NetworkList-et a deklar�ci�n�l inicializ�ljuk, nem az Awake-ben.
    // Ez a lista t�rolja a teljes�tett p�ly�k azonos�t�it.
    public NetworkList<FixedString32Bytes> CompletedLevelIds { get; private set; } = new NetworkList<FixedString32Bytes>();

    // Az �sszes el�rhet� vil�g defin�ci�j�t t�rolja, amit a Resources mapp�b�l t�lt�nk be.
    private List<WorldDefinition> allWorlds;

    void Awake()
    {
        // Singleton minta biztos�t�sa
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
    /// Bet�lti az �sszes WorldDefinition ScriptableObject-et a "Resources/Worlds" mapp�b�l.
    /// </summary>
    private void LoadAllWorldDefinitions()
    {
        // A bet�lt�s most m�r robusztusabb, nem okoz hib�t, ha a mappa �res.
        var loadedWorlds = Resources.LoadAll<WorldDefinition>("Worlds");
        allWorlds = new List<WorldDefinition>(loadedWorlds);

        if (allWorlds.Count == 0)
        {
            Debug.LogWarning("GameFlowManager: Nem tal�lhat� egyetlen WorldDefinition sem a 'Resources/Worlds' mapp�ban!");
        }
        else
        {
            Debug.Log($"GameFlowManager: Bet�ltve {allWorlds.Count} vil�g defin�ci�.");
        }
    }

    /// <summary>
    /// Visszaadja az �sszes bet�lt�tt vil�g list�j�t. A UI haszn�lja a gombok gener�l�s�hoz.
    /// </summary>
    public List<WorldDefinition> GetAllWorlds()
    {
        return allWorlds;
    }

    /// <summary>
    /// Visszaadja az aktu�lisan kiv�lasztott vil�g defin�ci�j�t az ID alapj�n.
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
    /// [ServerRpc] A Host h�vja meg a vil�gv�laszt� k�perny�r�l.
    /// Be�ll�tja az aktu�lis vil�got �s bet�lti a vil�gt�rk�pet minden kliens sz�m�ra.
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
            Debug.LogError($"A(z) '{worldId}' vil�ghoz nem tartozik �rv�nyes t�rk�p jelenet!");
        }
    }

    /// <summary>
    /// [ServerRpc] A Host h�vja meg, amikor egy p�lya elindul a vil�gt�rk�pr�l.
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
    /// [ServerRpc] A Host h�vja meg, amikor egy p�lya teljes�lt.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void CompleteLevelServerRpc(string levelId)
    {
        var fixedLevelId = new FixedString32Bytes(levelId);
        if (!CompletedLevelIds.Contains(fixedLevelId))
        {
            CompletedLevelIds.Add(fixedLevelId);
        }

        // Visszat�ltj�k a vil�gt�rk�pet.
        WorldDefinition currentWorld = GetCurrentWorldDefinition();
        if (currentWorld != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(currentWorld.worldMapSceneName, LoadSceneMode.Single);
        }
    }
}
