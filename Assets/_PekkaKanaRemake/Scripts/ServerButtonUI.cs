using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Net;

public class ServerButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI serverNameText;

    public void Setup(IPEndPoint sender, DiscoveryResponseData info, Action joinAction)
    {
        if (serverNameText != null)
        {
            // Ki�rjuk a szerver nev�t �s az IP c�m�t.
            serverNameText.text = $"{info.ServerName} ({sender.Address})";
        }

        if (button != null)
        {
            button.onClick.AddListener(() => joinAction?.Invoke());
        }
    }
}
