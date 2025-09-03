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
        if (finishedPlayers.Contains(playerId)) return; // Már célba ért

        finishedPlayers.Add(playerId);

        // Frissítsük a cél státuszt minden kliensen
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            int finished = finishedPlayers.Count;
            int total = NetworkManager.Singleton.ConnectedClientsIds.Count;
            levelManager.UpdateGoalStatusClientRpc(finished, total);
        }

        // Ha mindenki célba ért (single/multi), pálya vége
        if (finishedPlayers.Count >= NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            if (levelManager != null)
            {
                levelManager.StartLevelEndSequence();
            }
            finishedPlayers.Clear(); // reset a következõ szinthez
        }

        // Az adott cél objektumot deaktiváljuk, hogy ne triggerelhessen újra
        gameObject.SetActive(false);
    }
}