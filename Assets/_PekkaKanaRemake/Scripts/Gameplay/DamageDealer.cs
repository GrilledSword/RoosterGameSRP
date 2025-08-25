using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class DamageDealer : NetworkBehaviour
{
    [Header("Sebzés Beállítások")]
    [Tooltip("A sebzés mértéke, amit ez az objektum okoz.")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("Melyik frakcióhoz tartozik ez a sebzésforrás (pl. ki lőtte ki).")]
    [SerializeField] private Faction sourceFaction;

    [Tooltip("Mely frakció(ka)t sebezheti ez az objektum.")]
    [SerializeField] private Faction targetFactions;

    [Header("Viselkedés")]
    [Tooltip("Elpusztuljon-e az objektum, miután sebzett? (pl. lövedékek esetén igen, tüskéknél nem).")]
    [SerializeField] private bool destroyOnImpact = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        
        IDamageable damageableTarget = other.GetComponentInParent<IDamageable>();

        if (damageableTarget != null)
        {
            if ((targetFactions & damageableTarget.Faction) != 0)
            {
 damageableTarget.TakeDamage(damageAmount, sourceFaction);

                // Ha az objektumnak el kell pusztulnia, despawnoljuk.
                if (destroyOnImpact)
                {
                    if (TryGetComponent<NetworkObject>(out var networkObject))
                    {
                        networkObject.Despawn();
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
