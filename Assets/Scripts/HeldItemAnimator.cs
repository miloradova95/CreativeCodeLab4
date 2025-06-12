using UnityEngine;

/// <summary>
/// Simple animator for held items to make them feel more dynamic
/// </summary>
public class HeldItemAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public bool enableRotation = true;
    public bool enableBobbing = true;
    public bool enableSway = false; // Sway with mouse movement
    
    [Header("Rotation")]
    public Vector3 rotationSpeed = new Vector3(0, 30, 0); // Degrees per second
    
    [Header("Bobbing")]
    public float bobbingSpeed = 2f;
    public float bobbingAmount = 0.05f;
    
    [Header("Sway (if enabled)")]
    public float swayAmount = 0.1f;
    public float swaySpeed = 2f;
    
    private Vector3 startPosition;
    private Vector3 previousMousePosition;
    
    void Start()
    {
        startPosition = transform.localPosition;
        previousMousePosition = Input.mousePosition;
    }
    
    void Update()
    {
        Vector3 newPosition = startPosition;
        Vector3 newRotation = transform.localEulerAngles;
        
        // Rotation animation
        if (enableRotation)
        {
            newRotation += rotationSpeed * Time.deltaTime;
        }
        
        // Bobbing animation
        if (enableBobbing)
        {
            float bobOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
            newPosition.y = startPosition.y + bobOffset;
        }
        
        // Mouse sway (optional - can make it feel more immersive)
        if (enableSway)
        {
            Vector3 mouseDelta = Input.mousePosition - previousMousePosition;
            float swayX = -mouseDelta.x * swayAmount * Time.deltaTime;
            float swayY = -mouseDelta.y * swayAmount * Time.deltaTime;
            
            newPosition.x = startPosition.x + Mathf.Lerp(transform.localPosition.x - startPosition.x, swayX, Time.deltaTime * swaySpeed);
            newPosition.z = startPosition.z + Mathf.Lerp(transform.localPosition.z - startPosition.z, swayY, Time.deltaTime * swaySpeed);
        }
        
        // Apply transformations
        transform.localPosition = newPosition;
        transform.localEulerAngles = newRotation;
        
        previousMousePosition = Input.mousePosition;
    }
}