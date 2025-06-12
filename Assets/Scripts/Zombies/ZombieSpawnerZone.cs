using UnityEngine;

public class ZombieSpawnerZone : MonoBehaviour
{
    [Header("Spawner Settings")]
    public bool isActive = true;
    public float spawnInterval = 2.0f;
    public int maxZombies = 10;
    public float spawnRadius = 5.0f;
    
    [Header("Zombie Prefab")]
    public GameObject zombiePrefab;
    
    [Header("Spawn Area Visualization")]
    public bool showSpawnArea = true;
    public Color gizmoColor = Color.red;
    
    private float lastSpawnTime;
    private int currentZombieCount;
    
    void Start()
    {
        lastSpawnTime = Time.time;
        
        // If no zombie prefab is assigned, try to find one in Resources
        if (zombiePrefab == null)
        {
            zombiePrefab = Resources.Load<GameObject>("Zombie");
            if (zombiePrefab == null)
            {
                Debug.LogWarning("No zombie prefab assigned to " + gameObject.name + ". Please assign a zombie prefab.");
            }
        }
    }
    
    void Update()
    {
        if (isActive && zombiePrefab != null && ShouldSpawnZombie())
        {
            SpawnZombie();
        }
    }
    
    bool ShouldSpawnZombie()
    {
        return Time.time >= lastSpawnTime + spawnInterval && 
               currentZombieCount < maxZombies;
    }
    
    void SpawnZombie()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject newZombie = Instantiate(zombiePrefab, spawnPosition, transform.rotation);
        
        // Track this zombie for counting purposes
        ZombieTracker tracker = newZombie.AddComponent<ZombieTracker>();
        tracker.spawnerZone = this;
        
        currentZombieCount++;
        lastSpawnTime = Time.time;
        
        Debug.Log("Spawned zombie at " + spawnPosition + " from zone " + gameObject.name);
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
    
    // Call this when a zombie dies or is destroyed
    public void OnZombieDestroyed()
    {
        currentZombieCount = Mathf.Max(0, currentZombieCount - 1);
    }
    
    // Public methods for external control
    public void SetActive(bool active)
    {
        isActive = active;
    }
    
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.1f, interval);
    }
    
    public void SetMaxZombies(int max)
    {
        maxZombies = Mathf.Max(0, max);
    }
    
    public void SetSpawnRadius(float radius)
    {
        spawnRadius = Mathf.Max(0.1f, radius);
    }
    
    public void ForceSpawn()
    {
        if (zombiePrefab != null && currentZombieCount < maxZombies)
        {
            SpawnZombie();
        }
    }
    
    public void ClearAllZombies()
    {
        // Find all zombies with our tracker and destroy them
        ZombieTracker[] allTrackers = FindObjectsOfType<ZombieTracker>();
        foreach (ZombieTracker tracker in allTrackers)
        {
            if (tracker.spawnerZone == this)
            {
                Destroy(tracker.gameObject);
            }
        }
        currentZombieCount = 0;
    }
    
    // Visualization in Scene view
    void OnDrawGizmos()
    {
        if (showSpawnArea)
        {
            Gizmos.color = isActive ? gizmoColor : Color.gray;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            // Draw a smaller solid sphere at the center
            Gizmos.color = isActive ? new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw additional info when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2, Vector3.one * 0.5f);
    }
}

// Helper component to track which spawner zone created each zombie
public class ZombieTracker : MonoBehaviour
{
    [HideInInspector]
    public ZombieSpawnerZone spawnerZone;
    
    void Start()
    {
        // Listen for when this zombie is destroyed to notify the spawner
        ZombieController zombie = GetComponent<ZombieController>();
        if (zombie != null)
        {
            // We'll use OnDestroy to handle cleanup since ZombieController doesn't have death events
        }
    }
    
    void OnDestroy()
    {
        // Notify the spawner that this zombie was destroyed
        if (spawnerZone != null)
        {
            spawnerZone.OnZombieDestroyed();
        }
    }
}