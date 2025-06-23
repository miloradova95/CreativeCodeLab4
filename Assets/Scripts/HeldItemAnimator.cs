using UnityEngine;

public class HeldItemAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float rotationSpeed = 30f; // Degrees per second
    public float bobAmount = 0.1f; // How much to bob up and down
    public float bobSpeed = 2f; // Speed of bobbing
    
    private Vector3 startPosition;
    
    void Start()
    {
        startPosition = transform.localPosition;
    }
    
    void Update()
    {
        // Rotate the item slowly
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Add subtle bobbing motion
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = startPosition + Vector3.up * bobOffset;
    }
}