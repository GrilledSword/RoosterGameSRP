using UnityEngine;
using UnityEngine.UI; // Szükséges a Button komponensekhez
using Unity.Netcode; // Szükséges a NetworkManagerhez

// Ez a szkript kezeli a hálózati kapcsolat indítását (Host, Client, Server)
// UI gombok segítségével.
public class NetworkConnectionManager : MonoBehaviour
{
    [Header("UI Gombok")]
    [Tooltip("A szerver UI canvas referenciája.")]
    public GameObject serverCanvas;
    [Header("UI Gombok")]
    [Tooltip("A Host gomb referenciája.")]
    public Button hostButton;
    [Tooltip("A Client (Join) gomb referenciája.")]
    public Button joinButton;
    [Tooltip("A Server gomb referenciája.")]
    public Button serverButton;

    [Header("Hálózati Referenciák")]
    [Tooltip("A jelenet NetworkManager komponense.")]
    public NetworkManager networkManager;

    void Awake()
    {
        // Gyõzõdjünk meg róla, hogy a NetworkManager referencia be van állítva.
        if (networkManager == null)
        {
            networkManager = FindAnyObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkConnectionManager: Nem található NetworkManager a jelenetben!");
                enabled = false; // Letiltjuk a szkriptet, ha nincs NetworkManager
                return;
            }
        }

        // Feliratkozás a gombok OnClick eseményére
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
        // Leiratkozás az eseményekrõl a hibák elkerülése érdekében
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

    // Elindítja a játékot Hostként (Server + Client)
    public void StartHost()
    {
        if (networkManager.StartHost())
        {
            Debug.Log("NetworkConnectionManager: Host indítva.");
            DisableButtons();
        }
        else
        {
            Debug.LogError("NetworkConnectionManager: Host indítása sikertelen!");
        }
    }

    // Elindítja a játékot Clientként (csatlakozik egy Hosthoz/Serverhez)
    public void StartClient()
    {
        if (networkManager.StartClient())
        {
            Debug.Log("NetworkConnectionManager: Kliens indítva.");
            DisableButtons();
        }
        else
        {
            Debug.LogError("NetworkConnectionManager: Kliens indítása sikertelen!");
        }
    }

    // Elindítja a játékot dedikált Serverként
    public void StartServer()
    {
        if (networkManager.StartServer())
        {
            Debug.Log("NetworkConnectionManager: Szerver indítva.");
            DisableButtons();
        }
        else
        {
            Debug.LogError("NetworkConnectionManager: Szerver indítása sikertelen!");
        }
    }

    // Letiltja a gombokat, miután a kapcsolat létrejött
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