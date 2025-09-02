using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Projectile : NetworkBehaviour
{
    [Tooltip("A l�ved�k halad�si sebess�ge.")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LayerMask collisionLayerMask;

    private float maxDistance;
    private Vector3 startPosition;
    private Faction ownerFaction;

    public void Initialize(float distance, Faction ownerFaction)
    {
        this.maxDistance = distance;
        this.ownerFaction = ownerFaction;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        startPosition = transform.position;
        rb.linearVelocity = transform.forward * speed;
    }

    void Update()
    {
        if (!IsServer) return;

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            DestroyProjectile();
        }
    }

    // JAV�TVA: OnTriggerEnter haszn�lata az �tk�z�shez
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Ellen�rizz�k, hogy a r�teg benne van-e a megadott maszkban
        if ((collisionLayerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                if (damageable.Faction != ownerFaction)
                {
                    //         Sebezz�k az ellenfelet
                }
            }

            DestroyProjectile();
        }
    }

    private void DestroyProjectile()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // A sebz�st a l�ved�ken l�v� DamageDealer komponens fogja kezelni az OnTriggerEnter esem�nnyel.
}
