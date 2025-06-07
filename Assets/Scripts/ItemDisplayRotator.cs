using UnityEngine;

public class ItemDisplayRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Vector3 rotationSpeed = new Vector3(0, 30, 0); // Degrees per second
    public bool randomizeStartRotation = true;
    
    void Start()
    {
        if (randomizeStartRotation)
        {
            transform.rotation = Random.rotation;
        }
    }
    
    void Update()
    {
        // Rotate the item for visual appeal
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}