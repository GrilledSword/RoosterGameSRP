using UnityEngine;
using UnityEngine.Audio; // Szükséges az AudioMixerGroup-hoz!
using Unity.Netcode;

/// <summary>
/// A játékos karakteréhez tartozó összes hang lejátszásáért felelõs hálózati komponens.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : NetworkBehaviour
{
    [Header("Komponens Referenciák")]
    [Tooltip("Az AudioSource, ami a hangokat lejátssza.")]
    [SerializeField] public AudioSource audioSource;

    [Header("Audio Mixer Beállítás")]
    [Tooltip("Az AudioMixer csoport, amihez ez a hangforrás tartozik (pl. 'SFX').")]
    [SerializeField] private AudioMixerGroup outputAudioMixerGroup;

    [Header("Karakter Hangok")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip[] footsteps;

    void Update()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (outputAudioMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = outputAudioMixerGroup;
        }
    }

    // --- ClientRpc Metódusok ---
    // A szerver hívja meg ezeket, és minden kliensen lefutnak.

    [ClientRpc]
    public void PlayJumpSoundClientRpc()
    {
        if (jumpSound != null) audioSource.PlayOneShot(jumpSound);
    }
    
    [ClientRpc]
    public void PlayLandSoundClientRpc()
    {
        if (landSound != null) audioSource.PlayOneShot(landSound);
    }
    
    [ClientRpc]
    public void PlayDamageSoundClientRpc()
    {
        if (damageSound != null) audioSource.PlayOneShot(damageSound);
    }
    
    [ClientRpc]
    public void PlayDeathSoundClientRpc()
    {
        if (deathSound != null) audioSource.PlayOneShot(deathSound);
    }

    [ClientRpc]
    public void PlayItemSoundClientRpc(int itemID, bool isPickup)
    {
        ItemDefinition itemDef = ItemManager.Instance.GetItemDefinition(itemID);
        if (itemDef == null) return;

        AudioClip clipToPlay = isPickup ? itemDef.pickupSound : itemDef.useSound;
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    // Ezt az Animation Event hívja meg, de csak a helyi gépen.
    public void AnimEvent_PlayFootstepSound()
    {
        if (footsteps != null && footsteps.Length > 0)
        {
            audioSource.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)]);
        }
    }
}
