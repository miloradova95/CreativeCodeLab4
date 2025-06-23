using UnityEngine;

public class SpawnerZoneController : MonoBehaviour
{
    [Header("Zone References")]
    public ZombieSpawnerZone[] spawnerZones;
    
    [Header("Event Triggers")]
    public KeyCode activateAllKey = KeyCode.Alpha1;
    public KeyCode deactivateAllKey = KeyCode.Alpha2;
    public KeyCode increaseSpeedKey = KeyCode.Alpha3;
    public KeyCode decreaseSpeedKey = KeyCode.Alpha4;
    public KeyCode clearAllKey = KeyCode.Alpha5;
    
    [Header("Time-Based Control")]
    public bool useTimeBasedControl = false;
    public float gameStartTime;
    public AnimationCurve spawnIntervalOverTime = AnimationCurve.Linear(0f, 3f, 60f, 0.5f);
    
    void Start()
    {
        // Auto-find all spawner zones if not manually assigned
        if (spawnerZones == null || spawnerZones.Length == 0)
        {
            spawnerZones = FindObjectsOfType<ZombieSpawnerZone>();
        }
        
        gameStartTime = Time.time;
        
        Debug.Log($"Found {spawnerZones.Length} spawner zones to control");
    }
    
    void Update()
    {
        HandleInput();
        
        if (useTimeBasedControl)
        {
            HandleTimeBasedControl();
        }
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(activateAllKey))
        {
            SetAllZonesActive(true);
            Debug.Log("Activated all spawner zones");
        }
        
        if (Input.GetKeyDown(deactivateAllKey))
        {
            SetAllZonesActive(false);
            Debug.Log("Deactivated all spawner zones");
        }
        
        if (Input.GetKeyDown(increaseSpeedKey))
        {
            AdjustAllSpawnIntervals(0.8f); // 20% faster
            Debug.Log("Increased spawn speed");
        }
        
        if (Input.GetKeyDown(decreaseSpeedKey))
        {
            AdjustAllSpawnIntervals(1.25f); // 25% slower
            Debug.Log("Decreased spawn speed");
        }
        
        if (Input.GetKeyDown(clearAllKey))
        {
            ClearAllZombies();
            Debug.Log("Cleared all zombies");
        }
    }
    
    void HandleTimeBasedControl()
    {
        float gameTime = Time.time - gameStartTime;
        float targetInterval = spawnIntervalOverTime.Evaluate(gameTime);
        
        foreach (ZombieSpawnerZone zone in spawnerZones)
        {
            if (zone != null)
            {
                zone.SetSpawnInterval(targetInterval);
            }
        }
    }
    
    public void SetAllZonesActive(bool active)
    {
        foreach (ZombieSpawnerZone zone in spawnerZones)
        {
            if (zone != null)
            {
                zone.SetActive(active);
            }
        }
    }
    
    public void AdjustAllSpawnIntervals(float multiplier)
    {
        foreach (ZombieSpawnerZone zone in spawnerZones)
        {
            if (zone != null)
            {
                float currentInterval = zone.spawnInterval;
                zone.SetSpawnInterval(currentInterval * multiplier);
            }
        }
    }
    
    public void SetSpecificZoneInterval(int zoneIndex, float interval)
    {
        if (zoneIndex >= 0 && zoneIndex < spawnerZones.Length && spawnerZones[zoneIndex] != null)
        {
            spawnerZones[zoneIndex].SetSpawnInterval(interval);
        }
    }
    
    public void ActivateZoneByIndex(int zoneIndex, bool active)
    {
        if (zoneIndex >= 0 && zoneIndex < spawnerZones.Length && spawnerZones[zoneIndex] != null)
        {
            spawnerZones[zoneIndex].SetActive(active);
        }
    }
    
    public void ClearAllZombies()
    {
        foreach (ZombieSpawnerZone zone in spawnerZones)
        {
            if (zone != null)
            {
                zone.ClearAllZombies();
            }
        }
    }
    
    // Example: Trigger zones based on player events
    public void OnPlayerEnteredArea(string areaName)
    {
        switch (areaName)
        {
            case "SafeZone":
                SetAllZonesActive(false);
                break;
            case "DangerZone":
                SetAllZonesActive(true);
                AdjustAllSpawnIntervals(0.5f); // Double spawn speed
                break;
            case "BossArea":
                // Activate specific zones with different settings
                for (int i = 0; i < spawnerZones.Length; i++)
                {
                    spawnerZones[i].SetActive(true);
                    spawnerZones[i].SetSpawnInterval(1f); // Fast spawning
                    spawnerZones[i].SetMaxZombies(20); // More zombies
                }
                break;
        }
    }
}
