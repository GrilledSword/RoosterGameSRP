using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class Goal : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    private bool triggered = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag(playerTag)) return;

        PekkaPlayerController playerController = other.GetComponent<PekkaPlayerController>();
        if (playerController != null)
        {
            triggered = true;
            playerController.CompleteLevelServerRpc();

            gameObject.SetActive(false);
        }
    }
}

