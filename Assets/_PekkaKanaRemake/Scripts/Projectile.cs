using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Projectile : NetworkBehaviour
{
    [Tooltip("A l�ved�k halad�si sebess�ge.")]
    [SerializeField] private float speed = 20f;

    private Vector3 startPosition;
    private float travelDistance;

    /// <summary>
    /// A szerver h�vja meg ezt a met�dust a l�ved�k l�trehoz�sakor.
    /// </summary>
    public void Initialize(float distance)
    {
        this.travelDistance = distance;
        startPosition = transform.position;
    }

    void Update()
    {
        // A mozg�st �s az �lettartamot csak a szerver vez�rli.
        if (!IsServer) return;

        // El�re mozgatjuk a l�ved�ket a saj�t lok�lis "el�re" ir�ny�ba.
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Ha a l�ved�k megtette a maxim�lis t�vols�got, megsemmis�tj�k.
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

    // A sebz�st a l�ved�ken l�v� DamageDealer komponens fogja kezelni az OnTriggerEnter esem�nnyel.
}
