using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class LanDiscoveryManager : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    public event Action<IPEndPoint, DiscoveryResponseData> OnServerFound;

    public string ServerName { get; set; } = "Pekka Szerver";

    // ÚJ: Ezzel a kapcsolóval állítjuk, hogy a szerver látható-e a hálózaton.
    // Alapértelmezetten publikus, hogy a játékosok megtalálják.
    public bool IsPublicServer { get; set; } = true;

    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        // ÚJ LOGIKA: Ha a szerver nem publikus, nem válaszolunk a keresésre.
        if (!IsPublicServer)
        {
            response = default;
            return false; // Hamis értékkel jelezzük, hogy ne küldjön választ.
        }

        // Ha publikus, a régi logika szerint válaszolunk.
        response = new DiscoveryResponseData()
        {
            Port = NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port,
            ServerName = this.ServerName
        };
        return true;
    }

    protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        OnServerFound?.Invoke(sender, response);
    }
}
