using UnityEngine;

public class KeySpawner : MonoBehaviour
{
    [Header("Key Settings")]
    public GameObject keyPrefab;

    [Header("Spawn Locations")]
    public Transform[] spawnPoints; // empty GameObjects

    void Start()
    {
        if (keyPrefab == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("KeySpawner is missing keyPrefab or spawn points.");
            return;
        }

        // Pick a random spawn point
        int index = Random.Range(0, spawnPoints.Length);
        Transform chosenSpawn = spawnPoints[index];

        // Instantiate key at the chosen location
        Instantiate(keyPrefab, chosenSpawn.position, chosenSpawn.rotation);
    }
}
