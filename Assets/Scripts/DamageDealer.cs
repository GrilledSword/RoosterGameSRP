using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Ezt a komponenst b�rmilyen GameObject-re r�teheted, ami sebz�st okozhat �rint�skor.
/// A logika a szerveren fut a csal�sok elker�l�se v�gett.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DamageDealer : NetworkBehaviour
{
    [Header("Sebz�s Be�ll�t�sok")]
    [Tooltip("A sebz�s m�rt�ke, amit ez az objektum okoz.")]
    [SerializeField] private float damageAmount = 10f;

    [Tooltip("Melyik frakci�hoz tartozik ez a sebz�sforr�s (pl. ki l�tte ki).")]
    [SerializeField] private Faction sourceFaction;

    [Tooltip("Mely frakci�(ka)t sebezheti ez az objektum.")]
    [SerializeField] private Faction targetFactions;

    [Header("Viselked�s")]
    [Tooltip("Elpusztuljon-e az objektum, miut�n sebzett? (pl. l�ved�kek eset�n igen, t�sk�kn�l nem).")]
    [SerializeField] private bool destroyOnImpact = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // JAV�T�S: Nem csak a neki�tk�z�tt objektumot, hanem annak sz�leit is ellen�rizz�k.
        // Ez megoldja azt a probl�m�t, ha a Collider egy gyerek-objektumon van.
        IDamageable damageableTarget = other.GetComponentInParent<IDamageable>();

        if (damageableTarget != null)
        {
            // Ellen�rizz�k, hogy a c�lpont frakci�ja szerepel-e a sebezhet� frakci�k k�z�tt.
            if ((targetFactions & damageableTarget.Faction) != 0)
            {
                // Alkalmazzuk a sebz�st.
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
