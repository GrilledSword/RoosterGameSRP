using UnityEngine;

/// <summary>
/// A játékos karakteréhez tartozó összes hang lejátszásáért felelõs komponens.
/// Egy helyen kezeli az AudioSource-t és a hangfájlokat.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : MonoBehaviour
{
    [Header("Komponens Referenciák")]
    [Tooltip("Az AudioSource, ami a hangokat lejátssza.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Karakter Hangok")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip[] footsteps;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // --- Publikus metódusok, amiket más szkriptek hívhatnak ---

    public void PlayJumpSound()
    {
        if (jumpSound != null) audioSource.PlayOneShot(jumpSound);
    }

    public void PlayLandSound()
    {
        if (landSound != null) audioSource.PlayOneShot(landSound);
    }

    public void PlayDamageSound()
    {
        if (damageSound != null) audioSource.PlayOneShot(damageSound);
    }

    public void PlayDeathSound()
    {
        if (deathSound != null) audioSource.PlayOneShot(deathSound);
    }

    public void PlayFootstepSound()
    {
        if (footsteps != null && footsteps.Length > 0)
        {
            audioSource.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)]);
        }
    }

    public void PlayItemSound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
