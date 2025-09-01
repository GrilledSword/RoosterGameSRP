using UnityEngine;

/// <summary>
/// A p�lya v�g�t jelz� c�l. Amikor a j�t�kos hozz��r, befejezi a p�ly�t.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Goal : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    private bool triggered = false;

    private void Awake()
    {
        // Biztos�tjuk, hogy a collider trigger legyen
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag(playerTag))
        {
            triggered = true;

            // Megkeress�k a LevelManager-t �s jelezz�k neki, hogy a p�lya k�sz
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.StartLevelEndSequence();
            }
            else
            {
                Debug.LogError("C�l aktiv�lva, de nem tal�lhat� LevelManager a jelenetben!");
            }

            // Deaktiv�ljuk a c�lt, hogy ne lehessen �jra aktiv�lni
            gameObject.SetActive(false);
        }
    }
}
