using UnityEngine;

public class CircleMovement : MonoBehaviour
{
    [Header("Circle Movement Settings")]
    public float radius = 5f;
    [Tooltip("Speed in units per second")]
    public float speed = 5f;
    [Tooltip("Animation speed multiplier based on movement speed")]
    public float animationSpeedMultiplier = 1f;
    
    [Header("Gliding Behavior")]
    [Tooltip("Chance per animation loop completion to start gliding (0-1)")]
    public float glidingChance = 0.3f;
    
    private float angle = 0f;
    private Vector3 centerPoint;
    private Animator animator;
    
    private bool isGliding = false;
    private float glidingTimer = 0f;
    private float glidingDuration = 0f;
    private float normalAnimationSpeed = 1f;
    private bool hasCheckedThisLoop = false;
    
    void Start()
    {
        // Set the center point to the object's starting position
        centerPoint = transform.position;
        
        // Get animator component if it exists
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        // Calculate angular speed to maintain constant linear speed
        float angularSpeed = speed / radius;
        
        // Increment the angle based on angular speed and time
        angle += angularSpeed * Time.deltaTime;
        
        // Calculate new position using sin and cos
        float x = centerPoint.x + Mathf.Cos(angle) * radius;
        float z = centerPoint.z + Mathf.Sin(angle) * radius;
        
        // Store current position to calculate direction
        Vector3 oldPosition = transform.position;
        
        // Set new position
        transform.position = new Vector3(x, centerPoint.y, z);
        
        // Calculate direction and make object face that direction
        Vector3 direction = (transform.position - oldPosition).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // Update animation speed based on movement speed
        if (animator != null)
        {
            normalAnimationSpeed = (speed * animationSpeedMultiplier) / 5f; // Normalized to default speed of 5
            
            // Handle gliding behavior
            if (isGliding)
            {
                animator.speed = 0f;
                glidingTimer += Time.deltaTime;
                
                if (glidingTimer >= glidingDuration)
                {
                    // Stop gliding
                    isGliding = false;
                    glidingTimer = 0f;
                    hasCheckedThisLoop = false; // Reset flag when gliding ends
                }
            }
            else
            {
                animator.speed = normalAnimationSpeed;
                
                // Check if we're at the end of an animation loop to potentially start gliding
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                
                if (stateInfo.normalizedTime >= 1f && !hasCheckedThisLoop)
                {
                    hasCheckedThisLoop = true;
                    if (Random.Range(0f, 1f) < glidingChance)
                    {
                        StartGliding();
                    }
                }
                
                // Reset the check flag when we're in the middle of the animation
                if (stateInfo.normalizedTime < 0.9f)
                {
                    hasCheckedThisLoop = false;
                }
            }
        }
    }
    
    private void StartGliding()
    {
        isGliding = true;
        glidingTimer = 0f;
        glidingDuration = Random.Range(2f, 5f);
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw the circle in the editor
        Vector3 center = Application.isPlaying ? centerPoint : transform.position;
        
        Gizmos.color = Color.yellow;
        
        // Draw circle using line segments
        int segments = 64;
        float angleStep = 2f * Mathf.PI / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float currentAngle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;
            
            Vector3 currentPoint = center + new Vector3(
                Mathf.Cos(currentAngle) * radius,
                0,
                Mathf.Sin(currentAngle) * radius
            );
            
            Vector3 nextPoint = center + new Vector3(
                Mathf.Cos(nextAngle) * radius,
                0,
                Mathf.Sin(nextAngle) * radius
            );
            
            Gizmos.DrawLine(currentPoint, nextPoint);
        }
        
        // Draw center point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.2f);
    }
}