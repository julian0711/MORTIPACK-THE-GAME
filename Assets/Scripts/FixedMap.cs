using UnityEngine;

public class FixedMap : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform playerSpawnPoint;
    [Header("Debug")]
    public bool debugUseThisSpawn = true;

    // Future expansions: Enemy spawn points, Item spawn points, etc.

    private void OnDrawGizmos()
    {
        if (playerSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 0.5f);
            Gizmos.DrawIcon(playerSpawnPoint.position, "Player Icon", true);
        }
    }
}
