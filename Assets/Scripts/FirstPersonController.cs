using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    
    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    public float maxLookAngle = 80f;
    
    [Header("References")]
    public Camera playerCamera;
    public RaycastShooter shooter;
    
    // Private variables
    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isGrounded;
    
    // Inventory and Interaction components (added automatically)
    private InventorySystem inventorySystem;
    private InteractionSystem interactionSystem;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        
        if (shooter == null)
            shooter = GetComponentInChildren<RaycastShooter>();
        
        // Add inventory system if not present
        inventorySystem = GetComponent<InventorySystem>();
        if (inventorySystem == null)
        {
            inventorySystem = gameObject.AddComponent<InventorySystem>();
            inventorySystem.playerCamera = playerCamera;
        }
        
        // Add interaction system if not present
        interactionSystem = GetComponent<InteractionSystem>();
        if (interactionSystem == null)
        {
            interactionSystem = gameObject.AddComponent<InteractionSystem>();
            interactionSystem.playerCamera = playerCamera;
        }
        
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleShooting();
        HandleInventoryControls();
        
        // Toggle cursor lock with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }
    
void HandleInventoryControls()
{
    // Drop current item with Q
    if (Input.GetKeyDown(KeyCode.Q) && inventorySystem != null)
    {
        inventorySystem.DropCurrentItem();
    }
    
    // Toggle inventory visibility with Tab (optional)
    if (Input.GetKeyDown(KeyCode.Tab) && inventorySystem != null)
    {
        if (inventorySystem.inventoryPanel != null)
        {
            bool isActive = inventorySystem.inventoryPanel.activeSelf;
            inventorySystem.inventoryPanel.SetActive(!isActive);
        }
    }
    
    // Number keys for direct item selection (1-5)
    for (int i = 1; i <= 5; i++)
    {
        if (Input.GetKeyDown(KeyCode.Alpha0 + i) && inventorySystem != null)
        {
            inventorySystem.SelectItemByIndex(i - 1); // 0-based indexing
        }
    }
}
    
    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        if (invertY)
            mouseY = -mouseY;
        
        // Rotate the player body around the Y axis
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate the camera around the X axis
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
    
    void HandleMovement()
    {
        // Ground check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }
        
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Calculate movement direction
        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        
        // Determine speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        
        // Move the character
        controller.Move(direction * currentSpeed * Time.deltaTime);
        
        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    void HandleShooting()
    {
        if (shooter != null)
        {
            // The RaycastShooter handles its own input, but we can add additional controls here
            if (Input.GetMouseButtonDown(0)) // Left mouse button
            {
                shooter.FireRaycast();
            }
        }
    }
    
    void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    // Public methods for external control
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    public void SetWalkSpeed(float speed)
    {
        walkSpeed = speed;
    }
    
    public void SetRunSpeed(float speed)
    {
        runSpeed = speed;
    }
    
    // Inventory access methods
    public InventorySystem GetInventorySystem()
    {
        return inventorySystem;
    }
    
    public InteractionSystem GetInteractionSystem()
    {
        return interactionSystem;
    }
}