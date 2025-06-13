using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float forwardSpeedMultiplier = 1.2f; // Multiplier for forward movement
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float debuffSpeedMultiplier = 0.6f; // Speed when below debuff threshold
    
    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    public float maxLookAngle = 80f;
    
    [Header("Health System")]
    public float maxHealth = 100f;
    public float healthRegenRate = 2f; // Health per second
    public float healthDebuffThreshold = 30f; // Below this value, movement is slowed
    public float healthRegenCooldown = 15f; // Seconds before health starts regenerating
    
    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f; // Stamina per second
    public float staminaDebuffThreshold = 20f; // Warning threshold for breathing sounds (not used for debuff)
    public float staminaRegenCooldown = 10f; // Seconds before stamina starts regenerating
    public float sprintStaminaDrain = 20f; // Stamina per second while sprinting
    public float jumpStaminaCost = 15f; // Stamina cost per jump
    
    [Header("Camera Effects")]
    public float walkSwayAmount = 0.005f;
    public float walkSwaySpeed = 6f;
    public float walkSwayRotation = 1f;
    public float forwardSwaySpeedMultiplier = 1.3f; // Multiplier for forward sway speed
    public float sprintSwayMultiplier = 1.5f;
    public float debuffSwayMultiplier = 2f;
    public float shakeIntensity = 0.3f;
    public float shakeDuration = 0.5f;
    
    [Header("Sprint Acceleration")]
    public float sprintAcceleration = 3f; // How fast player accelerates to sprint speed
    public float sprintDeceleration = 5f; // How fast player decelerates from sprint speed
    
    [Header("References")]
    public Camera playerCamera;
    public RaycastShooter shooter;
    
    // Private variables
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 horizontalVelocity; // Separate horizontal velocity for air momentum
    private float xRotation = 0f;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    
    // Sprint acceleration
    private float currentSprintProgress = 0f; // 0 = walk speed, 1 = full sprint speed
    
    // Health and Stamina
    private float currentHealth;
    private float currentStamina;
    private float lastHealthDamageTime;
    private float lastStaminaDrainTime;
    private bool isStaminaEmpty = false; // Tracks if stamina hit 0
    
    // Camera effects
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private float swayTimer = 0f;
    private bool isShaking = false;
    private float shakeTimer = 0f;
    private Vector3 shakeOffset = Vector3.zero;
    
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
        
        // Store original camera position for effects
        if (playerCamera != null)
        {
            originalCameraPos = playerCamera.transform.localPosition;
            originalCameraRot = playerCamera.transform.localRotation;
        }
        
        // Add inventory system if not present
        inventorySystem = GetComponent<InventorySystem>();
        if (inventorySystem == null)
        {
            inventorySystem = gameObject.AddComponent<InventorySystem>();
        }
        
        // Set camera reference AFTER creating the component
        inventorySystem.playerCamera = playerCamera;
        
        // Add interaction system if not present
        interactionSystem = GetComponent<InteractionSystem>();
        if (interactionSystem == null)
        {
            interactionSystem = gameObject.AddComponent<InteractionSystem>();
            interactionSystem.playerCamera = playerCamera;
        }
        
        // Set camera reference AFTER creating the component
        interactionSystem.playerCamera = playerCamera;
        
        // Initialize health and stamina
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        lastHealthDamageTime = -healthRegenCooldown;
        lastStaminaDrainTime = -staminaRegenCooldown;
        
        // Initialize velocity
        velocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;
        
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        // Store previous grounded state
        wasGroundedLastFrame = isGrounded;
        
        HandleMouseLook();
        HandleMovement();
        HandleShooting();
        HandleInventoryControls();
        UpdateHealthAndStamina();
        UpdateCameraEffects();
        
        // Toggle cursor lock with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }
    
    void UpdateHealthAndStamina()
    {
        // Health regeneration
        if (Time.time - lastHealthDamageTime >= healthRegenCooldown && currentHealth < maxHealth)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
        
        // Stamina regeneration
        if (Time.time - lastStaminaDrainTime >= staminaRegenCooldown && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            
            // Check if stamina is fully recovered (removes debuff)
            if (currentStamina >= maxStamina && isStaminaEmpty)
            {
                isStaminaEmpty = false;
            }
        }
    }
    
    void UpdateCameraEffects()
    {
        if (playerCamera == null) return;
        
        Vector3 targetCameraPos = originalCameraPos;
        Quaternion targetCameraRot = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Handle camera shake
        if (isShaking)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                isShaking = false;
                shakeOffset = Vector3.zero;
            }
            else
            {
                shakeOffset = Random.insideUnitSphere * shakeIntensity * (shakeTimer / shakeDuration);
            }
        }
        
        // Handle walking sway
        Vector3 swayOffset = Vector3.zero;
        Vector3 swayRotation = Vector3.zero;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isMoving = horizontal != 0f || vertical != 0f;
        
        if (isGrounded)
        {
            float swaySpeedMultiplier = 1f;
            float swayMultiplier = 1f;
            
            if (isMoving)
            {
                // Check if moving forward or diagonally forward for sway speed bonus
                bool isMovingForward = vertical > 0f;
                if (isMovingForward)
                {
                    swaySpeedMultiplier *= forwardSwaySpeedMultiplier;
                }
                
                // Normal movement sway
                if (IsDebuffed())
                {
                    swayMultiplier = debuffSwayMultiplier;
                }
                else if (currentSprintProgress > 0f)
                {
                    // Sprint sway follows the acceleration curve
                    float sprintSwayProgress = Mathf.Lerp(1f, sprintSwayMultiplier, currentSprintProgress);
                    swayMultiplier = sprintSwayProgress;
                    swaySpeedMultiplier *= sprintSwayProgress;
                }
            }
            else
            {
                // Idle sway - much slower and subtler
                swaySpeedMultiplier = 0.3f; // Slower idle sway
                swayMultiplier = 0.4f; // Less intense idle sway
            }
            
            swayTimer += Time.deltaTime * walkSwaySpeed * swaySpeedMultiplier;
            
            // Smoother sway with different frequencies for more natural movement
            float swayX = Mathf.Sin(swayTimer) * walkSwayAmount * swayMultiplier;
            float swayY = Mathf.Sin(swayTimer * 2f) * walkSwayAmount * 0.7f * swayMultiplier;
            float swayZ = Mathf.Sin(swayTimer * 1.5f) * walkSwayAmount * 0.3f * swayMultiplier;
            
            swayOffset = new Vector3(swayX, swayY, swayZ);
            
            // Add subtle rotation sway
            float rotX = Mathf.Sin(swayTimer * 1.2f) * walkSwayRotation * swayMultiplier * 0.5f;
            float rotY = Mathf.Sin(swayTimer * 0.8f) * walkSwayRotation * swayMultiplier * 0.3f;
            float rotZ = Mathf.Sin(swayTimer) * walkSwayRotation * swayMultiplier;
            
            swayRotation = new Vector3(rotX, rotY, rotZ);
        }
        
        // Apply all offsets
        targetCameraPos += swayOffset + shakeOffset;
        targetCameraRot *= Quaternion.Euler(swayRotation);
        
        // Smooth interpolation for natural movement
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, targetCameraPos, Time.deltaTime * 8f);
        playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, targetCameraRot, Time.deltaTime * 8f);
    }
    
    void HandleInventoryControls()
    {
        // Drop current item with Q
        if (Input.GetKeyDown(KeyCode.Q) && inventorySystem != null)
        {
            inventorySystem.DropCurrentItem();
        }
    }
    
    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * 60f;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * 60f;
        
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
        
        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }
        
        // Only handle horizontal movement input when grounded
        if (isGrounded)
        {
            // Get raw input (using GetAxisRaw for immediate response)
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            
            // Calculate horizontal movement
            if (horizontal != 0f || vertical != 0f)
            {
                // Calculate movement direction (normalized to prevent faster diagonal movement)
                Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
                Vector3 worldDirection = transform.TransformDirection(inputDirection);
                
                // Check if player wants to sprint and can sprint
                bool wantsSprint = Input.GetKey(KeyCode.LeftShift);
                bool canSprint = CanSprint(horizontal, vertical) && currentStamina > 0f && !IsDebuffed();
                bool shouldSprint = wantsSprint && canSprint;
                
                // Handle sprint acceleration/deceleration
                if (shouldSprint)
                {
                    currentSprintProgress += sprintAcceleration * Time.deltaTime;
                }
                else
                {
                    currentSprintProgress -= sprintDeceleration * Time.deltaTime;
                }
                currentSprintProgress = Mathf.Clamp01(currentSprintProgress);
                
                // Interpolate between walk and run speed based on sprint progress
                float currentSpeed = Mathf.Lerp(walkSpeed, runSpeed, currentSprintProgress);
                
                // Apply forward movement bonus
                if (vertical > 0) // Moving forward
                {
                    currentSpeed *= forwardSpeedMultiplier;
                }
                
                // Apply debuff if health or stamina is below threshold
                if (IsDebuffed())
                {
                    currentSpeed *= debuffSpeedMultiplier;
                }
                
                // Set horizontal velocity
                horizontalVelocity = worldDirection * currentSpeed;
                
                // Drain stamina while sprinting (based on sprint progress)
                if (shouldSprint && currentSprintProgress > 0f)
                {
                    DrainStamina(sprintStaminaDrain * currentSprintProgress * Time.deltaTime);
                }
            }
            else
            {
                // No input - stop horizontal movement immediately and reset sprint progress
                horizontalVelocity = Vector3.zero;
                currentSprintProgress -= sprintDeceleration * Time.deltaTime;
                currentSprintProgress = Mathf.Clamp01(currentSprintProgress);
            }
        }
        // When airborne, horizontal velocity remains unchanged (momentum preservation)
        
        // Jumping (only when grounded)
        if (Input.GetButtonDown("Jump") && isGrounded && !IsDebuffed())
        {
            if (currentStamina >= jumpStaminaCost)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                DrainStamina(jumpStaminaCost);
            }
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        
        // Combine horizontal and vertical movement into a single move call
        Vector3 totalMovement = horizontalVelocity + new Vector3(0, velocity.y, 0);
        controller.Move(totalMovement * Time.deltaTime);
    }
    
    bool CanSprint(float horizontal, float vertical)
    {
        // Can only sprint when moving forward or diagonally forward
        return vertical > 0f;
    }
    
    bool IsDebuffed()
    {
        bool healthDebuff = currentHealth < healthDebuffThreshold;
        bool staminaDebuff = isStaminaEmpty; // Only debuffed when stamina hit 0
        return healthDebuff || staminaDebuff;
    }
    
    void DrainStamina(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        lastStaminaDrainTime = Time.time;
        
        // Mark stamina as empty if it hits 0
        if (currentStamina <= 0f)
        {
            isStaminaEmpty = true;
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        lastHealthDamageTime = Time.time;
        
        // Trigger camera shake when taking damage
        TriggerCameraShake();
    }
    
    public void TriggerCameraShake()
    {
        isShaking = true;
        shakeTimer = shakeDuration;
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
    
    // Health and Stamina getters
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    public float GetCurrentStamina()
    {
        return currentStamina;
    }
    
    public float GetMaxStamina()
    {
        return maxStamina;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }
    
    public bool IsPlayerDebuffed()
    {
        return IsDebuffed();
    }
    
    // Additional getter for breathing sound system
    public bool IsStaminaBelowThreshold()
    {
        return currentStamina < staminaDebuffThreshold;
    }
    
    public bool IsStaminaEmpty()
    {
        return isStaminaEmpty;
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