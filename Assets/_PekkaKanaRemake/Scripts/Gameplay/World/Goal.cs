using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Goal : NetworkBehaviour
{
    [SerializeField] private string playerTag = "Player";
    private static HashSet<ulong> finishedPlayers = new HashSet<ulong>();

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !other.CompareTag(playerTag)) return;

        PekkaPlayerController playerController = other.GetComponent<PekkaPlayerController>();
        if (playerController == null) return;

        ulong playerId = playerController.OwnerClientId;
        if (finishedPlayers.Contains(playerId)) return; // M�r c�lba �rt

        finishedPlayers.Add(playerId);

        // Friss�ts�k a c�l st�tuszt minden kliensen
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            int finished = finishedPlayers.Count;
            int total = NetworkManager.Singleton.ConnectedClientsIds.Count;
            levelManager.UpdateGoalStatusClientRpc(finished, total);
        }

        // Ha mindenki c�lba �rt (single/multi), p�lya v�ge
        if (finishedPlayers.Count >= NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            if (levelManager != null)
            {
                levelManager.StartLevelEndSequence();
            }
            finishedPlayers.Clear(); // reset a k�vetkez� szinthez
        }

        // Az adott c�l objektumot deaktiv�ljuk, hogy ne triggerelhessen �jra
        gameObject.SetActive(false);
    }
}