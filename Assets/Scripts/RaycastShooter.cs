using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class RaycastShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    public KeyCode shootKey = KeyCode.Space;
    public LayerMask raycastLayers = -1;
    public int maxReflections = 10;
    public float rayDistance = 1000f;
    
    [Header("Visual Settings")]
    public LineRenderer lineRenderer;
    public Material rayMaterial;
    public Color rayColor = Color.red;
    public float rayWidth = 0.05f;
    public float rayDuration = 2f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip hitSound;
    
    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public GameObject muzzleFlashPrefab;
    
    private Camera playerCamera;
    private List<Vector3> raycastPoints = new List<Vector3>();
    private float lastShotTime;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
        
        SetupLineRenderer();
        SetupAudioSource();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(shootKey))
        {
            ShootRaycast();
        }
        
        // Update line renderer visibility
        if (lineRenderer != null && Time.time - lastShotTime > rayDuration)
        {
            lineRenderer.enabled = false;
        }
    }
    
    void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        lineRenderer.material = rayMaterial;
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;
    }
    
    void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }
    
    void ShootRaycast()
    {
        raycastPoints.Clear();
        
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;
        
        // Add starting point
        raycastPoints.Add(rayOrigin);
        
        // Perform raycast with reflections
        for (int i = 0; i < maxReflections; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, raycastLayers))
            {
                raycastPoints.Add(hit.point);
                
                // Create hit effect
                CreateHitEffect(hit.point, hit.normal);
                
                // Play hit sound
                if (hitSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }
                
                // Calculate reflection
                Vector3 reflectedDirection = Vector3.Reflect(rayDirection, hit.normal);
                
                // Set up for next iteration
                rayOrigin = hit.point + hit.normal * 0.01f; // Small offset to avoid self-collision
                rayDirection = reflectedDirection;
                
                // Check if we hit something that doesn't reflect (optional)
                if (hit.collider.CompareTag("NoReflect"))
                {
                    break;
                }
            }
            else
            {
                // Ray didn't hit anything, extend to max distance
                raycastPoints.Add(rayOrigin + rayDirection * rayDistance);
                break;
            }
        }
        
        // Update visual representation
        UpdateLineRenderer();
        
        // Play shoot sound
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        // Create muzzle flash
        CreateMuzzleFlash();
        
        lastShotTime = Time.time;
        
        // Debug output
        Debug.Log($"Raycast bounced {raycastPoints.Count - 1} times");
    }
    
    void UpdateLineRenderer()
    {
        if (lineRenderer == null) return;
        
        lineRenderer.positionCount = raycastPoints.Count;
        lineRenderer.SetPositions(raycastPoints.ToArray());
        lineRenderer.enabled = true;
    }
    
    void CreateHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
        else
        {
            // Simple particle effect using built-in system
            GameObject sparkEffect = new GameObject("HitSpark");
            sparkEffect.transform.position = position;
            
            var particles = sparkEffect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = Color.yellow;
            main.startSize = 0.1f;
            main.startSpeed = 2f;
            main.maxParticles = 20;
            main.startLifetime = 0.5f;
            
            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, 20)
            });
            emission.enabled = true;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;
            
            Destroy(sparkEffect, 2f);
        }
    }
    
    void CreateMuzzleFlash()
    {
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, transform.position, transform.rotation);
            Destroy(flash, 0.1f);
        }
    }
    
    // Method to be called from UI or other scripts
    public void FireRaycast()
    {
        ShootRaycast();
    }
    
    // Gizmos for scene view debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
        
        if (raycastPoints.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < raycastPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(raycastPoints[i], raycastPoints[i + 1]);
            }
        }
    }
}