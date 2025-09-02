using UnityEngine;

/// <summary>
/// This component marks a transform as a spawn point for players.
/// It also draws a gizmo in the editor for easy visualization.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        // Draw a semi-transparent blue sphere gizmo at the spawn point's position
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.5f);

        // Draw a wireframe sphere to outline it
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
