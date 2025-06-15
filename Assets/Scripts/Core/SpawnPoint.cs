using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private static List<SpawnPoint> spawnPoints = new List<SpawnPoint>();


    private void OnEnable()
    {
        // Add this spawn point to the list when it is enabled
        spawnPoints.Add(this);
    }

    private void OnDisable()
    {
        // Remove this spawn point from the list when it is disabled
        spawnPoints.Remove(this);
    }
    public static Vector3 GetRandomSpawnPos()
    {
        if(spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points available!");
            return Vector3.zero; // Return a default value if no spawn points are available
        }

        return spawnPoints[Random.Range(0, spawnPoints.Count)].transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a sphere at the spawn point position for visualization in the editor
        Gizmos.color = Color.blue;

        Gizmos.DrawSphere(transform.position, 1);
    }
}
