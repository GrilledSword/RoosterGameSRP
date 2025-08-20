using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using System.Net;
using UnityEngine.SceneManagement; // Szükséges a jelenetváltáshoz

/// <summary>
/// Kezeli a LAN szerverek keresését, listázását, valamint a host indítását és a szerverhez való csatlakozást.
/// </summary>
public class ServerListManager : MonoBehaviour
{
    [Header("Komponens Referenciák")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private LanDiscoveryManager lanDiscovery;

    [Header("UI Referenciák")]
    [SerializeField] private GameObject serverButtonPrefab;
    [SerializeField] private Transform serverListContent;

    // A MainMenuUIManager állítja be a UI-ból kapott érték alapján.
    [HideInInspector] public string ServerNameToHost { get; set; }

    // ÚJ: A MainMenuUIManager állítja be a UI-ból kapott érték alapján.
    [HideInInspector] public bool HostAsPublic { get; set; } = true;

    private Dictionary<IPEndPoint, DiscoveryResponseData> discoveredServers = new Dictionary<IPEndPoint, DiscoveryResponseData>();

    void Awake()
    {
        // Biztonsági keresés, ha az Inspectorban nincsenek beállítva a referenciák.
        if (networkManager == null) networkManager = FindAnyObjectByType<NetworkManager>();
        if (lanDiscovery == null) lanDiscovery = FindAnyObjectByType<LanDiscoveryManager>();
    }

    void OnEnable()
    {
        if (lanDiscovery != null) lanDiscovery.OnServerFound += HandleServerFound;
    }

    void OnDisable()
    {
        if (lanDiscovery != null)
        {
            lanDiscovery.OnServerFound -= HandleServerFound;
            if (lanDiscovery.IsRunning)
            {
                lanDiscovery.StopDiscovery();
            }
        }
    }

    /// <summary>
    /// Elindítja a szerverek keresését a hálózaton és törli az elõzõ találatokat.
    /// </summary>
    public void RefreshServerList()
    {
        discoveredServers.Clear();
        foreach (Transform child in serverListContent)
        {
            Destroy(child.gameObject);
        }

        lanDiscovery.StartClient();
        lanDiscovery.ClientBroadcast(new DiscoveryBroadcastData());
    }

    /// <summary>
    /// Leállítja a kliens oldali szerverkeresést.
    /// </summary>
    public void StopClientDiscovery()
    {
        if (lanDiscovery != null && lanDiscovery.IsClient)
        {
            lanDiscovery.StopDiscovery();
        }
    }

    private void HandleServerFound(IPEndPoint sender, DiscoveryResponseData response)
    {
        if (discoveredServers.ContainsKey(sender)) return;

        discoveredServers.Add(sender, response);

        GameObject serverButtonInstance = Instantiate(serverButtonPrefab, serverListContent);
        var serverButtonUI = serverButtonInstance.GetComponent<ServerButtonUI>();

        if (serverButtonUI != null)
        {
            serverButtonUI.Setup(sender, response, () => {
                JoinServer(sender, response);
            });
        }
    }

    private void JoinServer(IPEndPoint sender, DiscoveryResponseData response)
    {
        StopClientDiscovery();

        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        transport.SetConnectionData(sender.Address.ToString(), response.Port);
        networkManager.StartClient();

        // A kliensnek nem kell jelenetet töltenie, a Netcode automatikusan szinkronizálja a Host-éhoz.
        // A UI panelek elrejtését a MainMenuUIManager végzi.
    }

    public void StartHostOnly()
    {
        // ÚJ: Átadjuk a beállítást a LanDiscovery-nek, mielõtt elindítjuk.
        lanDiscovery.IsPublicServer = this.HostAsPublic;
        lanDiscovery.ServerName = this.ServerNameToHost;

        networkManager.StartHost();
        lanDiscovery.StartServer();
    }
}
