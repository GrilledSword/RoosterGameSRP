using UnityEngine;

/// <summary>
/// A pálya végét jelzõ cél. Amikor a játékos hozzáér, befejezi a pályát.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Goal : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    private bool triggered = false;

    private void Awake()
    {
        // Biztosítjuk, hogy a collider trigger legyen
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag(playerTag))
        {
            triggered = true;

            // Megkeressük a LevelManager-t és jelezzük neki, hogy a pálya kész
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.StartLevelEndSequence();
            }
            else
            {
                Debug.LogError("Cél aktiválva, de nem található LevelManager a jelenetben!");
            }

            // Deaktiváljuk a célt, hogy ne lehessen újra aktiválni
            gameObject.SetActive(false);
        }
    }
}
