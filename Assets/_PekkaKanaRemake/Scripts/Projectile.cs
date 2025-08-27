using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Projectile : NetworkBehaviour
{
    [Tooltip("A lövedék haladási sebessége.")]
    [SerializeField] private float speed = 20f;

    private Vector3 startPosition;
    private float travelDistance;

    /// <summary>
    /// A szerver hívja meg ezt a metódust a lövedék létrehozásakor.
    /// </summary>
    public void Initialize(float distance)
    {
        this.travelDistance = distance;
        startPosition = transform.position;
    }

    void Update()
    {
        // A mozgást és az élettartamot csak a szerver vezérli.
        if (!IsServer) return;

        // Elõre mozgatjuk a lövedéket a saját lokális "elõre" irányába.
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Ha a lövedék megtette a maximális távolságot, megsemmisítjük.
        if (Vector3.Distance(startPosition, transform.position) >= travelDistance)
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
    }

    // A sebzést a lövedéken lévõ DamageDealer komponens fogja kezelni az OnTriggerEnter eseménnyel.
}
