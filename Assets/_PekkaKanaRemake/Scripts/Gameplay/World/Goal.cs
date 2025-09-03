using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Goal : NetworkBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private static HashSet<ulong> finishedPlayers = new HashSet<ulong>();

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !other.CompareTag(playerTag)) return;

        PekkaPlayerController playerController = other.GetComponent<PekkaPlayerController>();
        if (playerController == null) return;

        ulong clientId = playerController.OwnerClientId;
        if (finishedPlayers.Contains(clientId)) return;

        finishedPlayers.Add(clientId);
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            int finished = finishedPlayers.Count;
            int total = NetworkManager.Singleton.ConnectedClientsIds.Count;
            levelManager.UpdateGoalStatusClientRpc(finished, total);
        }

        if (finishedPlayers.Count >= NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            if (levelManager != null)
            {
                levelManager.StartLevelEndSequence();
            }
            finishedPlayers.Clear();
        }

        gameObject.SetActive(false);
    }
}