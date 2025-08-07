using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using System.Net;
using UnityEngine.SceneManagement; // Sz�ks�ges a jelenetv�lt�shoz

/// <summary>
/// Kezeli a LAN szerverek keres�s�t, list�z�s�t, valamint a host ind�t�s�t �s a szerverhez val� csatlakoz�st.
/// </summary>
public class ServerListManager : MonoBehaviour
{
    [Header("Komponens Referenci�k")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private LanDiscoveryManager lanDiscovery;

    [Header("UI Referenci�k")]
    [SerializeField] private GameObject serverButtonPrefab;
    [SerializeField] private Transform serverListContent;

    // A MainMenuUIManager �ll�tja be a UI-b�l kapott �rt�k alapj�n.
    [HideInInspector] public string ServerNameToHost { get; set; }

    // �J: A MainMenuUIManager �ll�tja be a UI-b�l kapott �rt�k alapj�n.
    [HideInInspector] public bool HostAsPublic { get; set; } = true;

    private Dictionary<IPEndPoint, DiscoveryResponseData> discoveredServers = new Dictionary<IPEndPoint, DiscoveryResponseData>();

    void Awake()
    {
        // Biztons�gi keres�s, ha az Inspectorban nincsenek be�ll�tva a referenci�k.
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
    /// Elind�tja a szerverek keres�s�t a h�l�zaton �s t�rli az el�z� tal�latokat.
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
    /// Le�ll�tja a kliens oldali szerverkeres�st.
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

        // A kliensnek nem kell jelenetet t�ltenie, a Netcode automatikusan szinkroniz�lja a Host-�hoz.
        // A UI panelek elrejt�s�t a MainMenuUIManager v�gzi.
    }

    public void StartHostOnly()
    {
        // �J: �tadjuk a be�ll�t�st a LanDiscovery-nek, miel�tt elind�tjuk.
        lanDiscovery.IsPublicServer = this.HostAsPublic;
        lanDiscovery.ServerName = this.ServerNameToHost;

        networkManager.StartHost();
        lanDiscovery.StartServer();
    }
}
