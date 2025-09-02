using UnityEngine;
using UnityEngine.Audio; // Sz�ks�ges az AudioMixerGroup-hoz!
using Unity.Netcode;

/// <summary>
/// A j�t�kos karakter�hez tartoz� �sszes hang lej�tsz�s��rt felel�s h�l�zati komponens.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : NetworkBehaviour
{
    [Header("Komponens Referenci�k")]
    [Tooltip("Az AudioSource, ami a hangokat lej�tssza.")]
    [SerializeField] public AudioSource audioSource;

    [Header("Audio Mixer Be�ll�t�s")]
    [Tooltip("Az AudioMixer csoport, amihez ez a hangforr�s tartozik (pl. 'SFX').")]
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

    // --- ClientRpc Met�dusok ---
    // A szerver h�vja meg ezeket, �s minden kliensen lefutnak.

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

    // Ezt az Animation Event h�vja meg, de csak a helyi g�pen.
    public void AnimEvent_PlayFootstepSound()
    {
        if (footsteps != null && footsteps.Length > 0)
        {
            audioSource.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)]);
        }
    }
}
