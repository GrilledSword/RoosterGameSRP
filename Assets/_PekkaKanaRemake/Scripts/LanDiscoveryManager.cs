using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class LanDiscoveryManager : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    public event Action<IPEndPoint, DiscoveryResponseData> OnServerFound;

    public string ServerName { get; set; } = "Pekka Szerver";

    // �J: Ezzel a kapcsol�val �ll�tjuk, hogy a szerver l�that�-e a h�l�zaton.
    // Alap�rtelmezetten publikus, hogy a j�t�kosok megtal�lj�k.
    public bool IsPublicServer { get; set; } = true;

    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        // �J LOGIKA: Ha a szerver nem publikus, nem v�laszolunk a keres�sre.
        if (!IsPublicServer)
        {
            response = default;
            return false; // Hamis �rt�kkel jelezz�k, hogy ne k�ldj�n v�laszt.
        }

        // Ha publikus, a r�gi logika szerint v�laszolunk.
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
