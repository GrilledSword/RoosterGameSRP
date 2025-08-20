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
            // Kiírjuk a szerver nevét és az IP címét.
            serverNameText.text = $"{info.ServerName} ({sender.Address})";
        }

        if (button != null)
        {
            button.onClick.AddListener(() => joinAction?.Invoke());
        }
    }
}
