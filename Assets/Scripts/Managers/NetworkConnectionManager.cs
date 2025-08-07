using UnityEngine;
using UnityEngine.UI; // Sz�ks�ges a Button komponensekhez
using Unity.Netcode; // Sz�ks�ges a NetworkManagerhez

// Ez a szkript kezeli a h�l�zati kapcsolat ind�t�s�t (Host, Client, Server)
// UI gombok seg�ts�g�vel.
public class NetworkConnectionManager : MonoBehaviour
{
    [Header("UI Gombok")]
    [Tooltip("A szerver UI canvas referenci�ja.")]
    public GameObject serverCanvas;
    [Header("UI Gombok")]
    [Tooltip("A Host gomb referenci�ja.")]
    public Button hostButton;
    [Tooltip("A Client (Join) gomb referenci�ja.")]
    public Button joinButton;
    [Tooltip("A Server gomb referenci�ja.")]
    public Button serverButton;

    [Header("H�l�zati Referenci�k")]
    [Tooltip("A jelenet NetworkManager komponense.")]
    public NetworkManager networkManager;

    void Awake()
    {
        // Gy�z�dj�nk meg r�la, hogy a NetworkManager referencia be van �ll�tva.
        if (networkManager == null)
        {
            networkManager = FindAnyObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkConnectionManager: Nem tal�lhat� NetworkManager a jelenetben!");
                enabled = false; // Letiltjuk a szkriptet, ha nincs NetworkManager
                return;
            }
        }

        // Feliratkoz�s a gombok OnClick esem�ny�re
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(StartHost);
        }
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(StartClient);
        }
        if (serverButton != null)
        {
            serverButton.onClick.AddListener(StartServer);
        }
    }

    void OnDestroy()
    {
        // Leiratkoz�s az esem�nyekr�l a hib�k elker�l�se �rdek�ben
        if (hostButton != null)
        {
            hostButton.onClick.RemoveListener(StartHost);
        }
        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(StartClient);
        }
        if (serverButton != null)
        {
            serverButton.onClick.RemoveListener(StartServer);
        }
    }

    // Elind�tja a j�t�kot Hostk�nt (Server + Client)
    public void StartHost()
    {
        if (networkManager.StartHost())
        {
            Debug.Log("NetworkConnectionManager: Host ind�tva.");
            DisableButtons();
        }
        else
        {
            Debug.LogError("NetworkConnectionManager: Host ind�t�sa sikertelen!");
        }
    }

    // Elind�tja a j�t�kot Clientk�nt (csatlakozik egy Hosthoz/Serverhez)
    public void StartClient()
    {
        if (networkManager.StartClient())
        {
            Debug.Log("NetworkConnectionManager: Kliens ind�tva.");
            DisableButtons();
        }
        else
        {
            Debug.LogError("NetworkConnectionManager: Kliens ind�t�sa sikertelen!");
        }
    }

    // Elind�tja a j�t�kot dedik�lt Serverk�nt
    public void StartServer()
    {
        if (networkManager.StartServer())
        {
            Debug.Log("NetworkConnectionManager: Szerver ind�tva.");
            DisableButtons();
        }
        else
        {
            Debug.LogError("NetworkConnectionManager: Szerver ind�t�sa sikertelen!");
        }
    }

    // Letiltja a gombokat, miut�n a kapcsolat l�trej�tt
    private void DisableButtons()
    {
        if (hostButton != null) hostButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;
        if (serverButton != null) serverButton.interactable = false;
        if (serverCanvas != null)
        {
            serverCanvas.SetActive(false);
        }
    }
}