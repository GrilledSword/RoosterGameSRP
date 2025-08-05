using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Ezt a komponenst bármilyen GameObject-re ráteheted, ami sebzést okozhat érintéskor.
/// A logika a szerveren fut a csalások elkerülése végett.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DamageDealer : NetworkBehaviour
{
    [Header("Sebzés Beállítások")]
    [Tooltip("A sebzés mértéke, amit ez az objektum okoz.")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("Melyik frakcióhoz tartozik ez a sebzésforrás (pl. ki lõtte ki).")]
    [SerializeField] private Faction sourceFaction;

    [Tooltip("Mely frakció(ka)t sebezheti ez az objektum.")]
    [SerializeField] private Faction targetFactions;

    [Header("Viselkedés")]
    [Tooltip("Elpusztuljon-e az objektum, miután sebzett? (pl. lövedékek esetén igen, tüskéknél nem).")]
    [SerializeField] private bool destroyOnImpact = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // JAVÍTÁS: Nem csak a nekiütközött objektumot, hanem annak szüleit is ellenõrizzük.
        // Ez megoldja azt a problémát, ha a Collider egy gyerek-objektumon van.
        IDamageable damageableTarget = other.GetComponentInParent<IDamageable>();

        if (damageableTarget != null)
        {
            // Ellenõrizzük, hogy a célpont frakciója szerepel-e a sebezhetõ frakciók között.
            if ((targetFactions & damageableTarget.Faction) != 0)
            {
                // Alkalmazzuk a sebzést.
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
