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
    public float healthRegenCooldown = 15f; // Seconds before health starts regenerating

    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f; // Stamina per second
    public float staminaDebuffThreshold = 20f; // Warning threshold for breathing sounds (not used for debuff)
    public float staminaRegenCooldown = 10f; // Seconds before stamina starts regenerating
    public float sprintStaminaDrain = 20f; // Stamina per second while sprinting
    public float jumpStaminaCost = 15f; // Stamina cost per jump

    [Header("Fall Damage")]
    public float fallDamageThreshold = 8f; // Height at which fall damage starts
    public float fallDamageMultiplier = 10f; // Damage per unit of fall distance

    [Header("Death System")]
    public float deathCameraTiltAngle = -90f; // Angle to tilt camera when dying
    public float deathCameraTiltSpeed = 2f; // Speed of camera tilt animation

    [Header("Breathing System")]
    public string breathingEventName = "Play_Breathing"; // Wwise event name for breathing blend container
    public float breathingUpdateRate = 0.1f; // How often to update breathing level (in seconds)

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

    // Breathing system enums
    public enum BreathingLevel
    {
        Passive = 0,        // 0-19 RTCP range
        Light = 1,          // 20-39 RTCP range
        Medium = 2,         // 40-59 RTCP range
        Heavy = 3,          // 60-79 RTCP range
        VeryHeavy = 4       // 80-100 RTCP range
    }

    public enum PlayerState
    {
        Alive,
        Dying,
        Dead
    }

    private float currentStepRate;
    private float healthDebuffThreshold; // Now calculated as 20% of maxHealth
    private float staminaDebuffThreshold_Calculated; // Now calculated as 20% of maxStamina

    // Private variables
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 horizontalVelocity; // Separate horizontal velocity for air momentum
    private float xRotation = 0f;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private float walkFootstepInterval = 0.5f;
    private float sprintFootstepInterval = 0.3f;
    private float footstepInterval; // This will be updated based on sprinting
    private float footstepTimer = 0f;
    private bool isSprinting = false; // Tracks if player is currently sprinting
    private CallEvent callEvent;

    // Sprint acceleration
    private float currentSprintProgress = 0f; // 0 = walk speed, 1 = full sprint speed

    // Health and Stamina
    private float currentHealth;
    private float currentStamina;
    private float lastHealthDamageTime;
    private float lastStaminaDrainTime;
    private bool isStaminaEmpty = false; // Tracks if stamina hit 0

    // HitStun
    private float zombieHitCooldown = 1.0f; // Invulnerability duration
    private float lastZombieHitTime = -999f;

    // Fall damage tracking
    private float lastGroundedHeight;
    private bool wasFalling = false;

    // Death system
    private PlayerState currentState = PlayerState.Alive;
    private float deathCameraTiltTarget = 0f;
    private Quaternion deathCameraStartRotation;
    private HealthUIManager healthUIManager;
    private DeathScreenManager deathScreenManager;

    // Breathing system variables
    private BreathingLevel currentBreathingLevel = BreathingLevel.Passive;
    private float breathingUpdateTimer = 0f;
    private bool isBreathingEventPlaying = false;
    private bool isStaminaRegenerating = false;

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
        callEvent = GetComponent<CallEvent>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (shooter == null)
            shooter = GetComponentInChildren<RaycastShooter>();

        // Find UI managers
        healthUIManager = FindObjectOfType<HealthUIManager>();
        deathScreenManager = FindObjectOfType<DeathScreenManager>();

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

        // Apply settings from SettingsManager
        ApplyGameSettings();

        // Calculate dynamic thresholds (20% of max values)
        healthDebuffThreshold = maxHealth * 0.2f;
        staminaDebuffThreshold_Calculated = maxStamina * 0.2f;

        // Initialize health and stamina
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        lastHealthDamageTime = -healthRegenCooldown;
        lastStaminaDrainTime = -staminaRegenCooldown;

        // Initialize velocity
        velocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;

        // Initialize fall damage tracking
        lastGroundedHeight = transform.position.y;

        // Start breathing system
        StartBreathingSystem();

        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ApplyGameSettings()
    {
        if (SettingsManager.Instance != null)
        {
            // Apply mouse sensitivity
            mouseSensitivity = SettingsManager.Instance.mouseSensitivity;

            // Apply difficulty settings
            var difficulty = SettingsManager.Instance.difficulty;
            maxHealth = SettingsManager.DifficultyStats.GetMaxHealth(difficulty);
            maxStamina = SettingsManager.DifficultyStats.GetMaxStamina(difficulty);

            Debug.Log($"Applied settings - Sensitivity: {mouseSensitivity}, Health: {maxHealth}, Stamina: {maxStamina}");
        }
    }

    void Update()
    {
        if (currentState == PlayerState.Dead) return;

        if (currentState == PlayerState.Dying)
        {
            HandleDeathAnimation();
            return;
        }

        // Store previous grounded state
        wasGroundedLastFrame = isGrounded;

        HandleMouseLook();
        HandleMovement();
        HandleShooting();
        HandleInventoryControls();
        UpdateHealthAndStamina();
        UpdateBreathingSystem();
        UpdateCameraEffects();
        HandleFallDamage();

        // Check for death
        if (currentHealth <= 0f && currentState == PlayerState.Alive)
        {
            StartDying();
        }

        // Toggle cursor lock with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    void HandleFallDamage()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            if (wasFalling)
            {
                float fallDistance = lastGroundedHeight - transform.position.y;
                
                if (fallDistance > fallDamageThreshold)
                {
                    float damage = (fallDistance - fallDamageThreshold) * fallDamageMultiplier;
                    TakeDamage(damage);
                    Debug.Log($"Fall damage: {damage} (fell {fallDistance} units)");
                }
                
                wasFalling = false;
            }
            
            lastGroundedHeight = transform.position.y;
        }
        else if (velocity.y < -5f) // Only consider it falling if moving downward fast enough
        {
            wasFalling = true;
        }
    }

    void StartDying()
    {

        callEvent.Callevent("Death");
        currentState = PlayerState.Dying;
        
        // Stop camera effects
        isShaking = false;
        
        // Store the starting rotation for death animation
        deathCameraStartRotation = playerCamera.transform.localRotation;
        deathCameraTiltTarget = deathCameraTiltAngle;
        
        // Unlock cursor movement during death
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log("Player started dying");
    }

    void HandleDeathAnimation()
    {
        if (playerCamera == null) return;

        // Animate camera tilt
        float targetZ = deathCameraTiltTarget;
        Vector3 currentEuler = playerCamera.transform.localRotation.eulerAngles;
        
        // Handle angle wrapping
        if (currentEuler.z > 180f)
            currentEuler.z -= 360f;
            
        float newZ = Mathf.MoveTowards(currentEuler.z, targetZ, deathCameraTiltSpeed * 90f * Time.deltaTime);
        
        playerCamera.transform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, newZ);
        
        // Check if animation is complete
        if (Mathf.Abs(newZ - targetZ) < 1f)
        {
            currentState = PlayerState.Dead;
            
            if (deathScreenManager != null)
            {


                deathScreenManager.StartDeathSequence();
            }
            
            Debug.Log("Player is now dead");
        }
    }

    void UpdateHealthAndStamina()
    {
        // Check if stamina is regenerating
        bool wasRegenerating = isStaminaRegenerating;
        isStaminaRegenerating = false;

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
            isStaminaRegenerating = true;

            // Check if stamina is fully recovered (removes debuff)
            if (currentStamina >= maxStamina && isStaminaEmpty)
            {
                isStaminaEmpty = false;
            }
        }
    }

    void StartBreathingSystem()
    {
        if (callEvent != null)
        {
            callEvent.Callevent(breathingEventName);
            isBreathingEventPlaying = true;
            Debug.Log("Breathing system started");
        }
    }

    void StopBreathingSystem()
    {
        if (callEvent != null && isBreathingEventPlaying)
        {
            callEvent.Callevent("Stop_Breathing");
            isBreathingEventPlaying = false;
            Debug.Log("Breathing system stopped");
        }
    }

    void UpdateBreathingSystem()
    {
        breathingUpdateTimer += Time.deltaTime;
        
        if (breathingUpdateTimer >= breathingUpdateRate)
        {
            breathingUpdateTimer = 0f;
            
            BreathingLevel newLevel = CalculateBreathingLevel();
            
            if (newLevel != currentBreathingLevel)
            {
                currentBreathingLevel = newLevel;
                UpdateBreathingSound();
            }
        }
    }

    BreathingLevel CalculateBreathingLevel()
    {
        // Check for very heavy breathing first (debuffed state)
        if (IsDebuffed())
        {
            return BreathingLevel.VeryHeavy;
        }

        // Check if stamina is below 50% (heavy breathing)
        if (currentStamina < maxStamina * 0.5f)
        {
            return BreathingLevel.Heavy;
        }

        // Check if sprinting (medium breathing)
        if (isSprinting)
        {
            return BreathingLevel.Medium;
        }

        // Check if stamina is regenerating and above 50% (light breathing)
        if (isStaminaRegenerating && currentStamina >= maxStamina * 0.5f)
        {
            return BreathingLevel.Light;
        }

        // Default to passive breathing (standing still or walking, not sprinting)
        return BreathingLevel.Passive;
    }

    void UpdateBreathingSound()
    {
        if (callEvent == null || !isBreathingEventPlaying) return;

        // Calculate RTCP value based on breathing level
        // Each level uses 20 units: 0-19, 20-39, 40-59, 60-79, 80-99
        int rtcpValue = (int)currentBreathingLevel * 20 + 10; // +10 to center in the range
        rtcpValue = Mathf.Clamp(rtcpValue, 0, 100);

        // Set the RTCP parameter in Wwise
        // You'll need to replace "BreathingIntensity" with your actual RTCP parameter name
        AkUnitySoundEngine.SetRTPCValue("BreathIntensity", rtcpValue, gameObject);
        
        Debug.Log($"Breathing level: {currentBreathingLevel}, RTCP value: {rtcpValue}");
    }

    void UpdateCameraEffects()
    {
        if (playerCamera == null || currentState != PlayerState.Alive) return;

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
        if (Cursor.lockState != CursorLockMode.Locked || currentState != PlayerState.Alive) return;

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
        if (currentState != PlayerState.Alive) return;

        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (isGrounded)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (horizontal != 0f || vertical != 0f)
            {
                Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
                Vector3 worldDirection = transform.TransformDirection(inputDirection);

                bool wantsSprint = Input.GetKey(KeyCode.LeftShift);
                bool canSprint = CanSprint(horizontal, vertical) && currentStamina > 0f && !IsDebuffed();
                bool shouldSprint = wantsSprint && canSprint;

                // Sprint audio logic - removed Sprint and StopSprint calls as breathing system handles it
                bool wasSprinting = isSprinting;
                isSprinting = shouldSprint && (horizontal != 0f || vertical != 0f);

                // Sprint speed interpolation
                if (shouldSprint)
                    currentSprintProgress += sprintAcceleration * Time.deltaTime;
                else
                    currentSprintProgress -= sprintDeceleration * Time.deltaTime;
                currentSprintProgress = Mathf.Clamp01(currentSprintProgress);

                float currentSpeed = Mathf.Lerp(walkSpeed, runSpeed, currentSprintProgress);

                if (vertical > 0)
                    currentSpeed *= forwardSpeedMultiplier;

                if (IsDebuffed())
                    currentSpeed *= debuffSpeedMultiplier;

                horizontalVelocity = worldDirection * currentSpeed;

                if (shouldSprint && currentSprintProgress > 0f)
                    DrainStamina(sprintStaminaDrain * currentSprintProgress * Time.deltaTime);

                // Footstep logic
                footstepInterval = shouldSprint ? sprintFootstepInterval : walkFootstepInterval;
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    if (callEvent != null)
                    {
                        callEvent.Callevent("Footsteps");
                    }
                    footstepTimer = footstepInterval;
                }
            }
            else
            {
                horizontalVelocity = Vector3.zero;
                currentSprintProgress -= sprintDeceleration * Time.deltaTime;
                currentSprintProgress = Mathf.Clamp01(currentSprintProgress);

                // Update sprint status when player stops moving
                isSprinting = false;
            }
        }

        if (Input.GetButtonDown("Jump") && isGrounded && !IsDebuffed())
        {
            if (currentStamina >= jumpStaminaCost)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                DrainStamina(jumpStaminaCost);
                if (callEvent != null)
                {
                    callEvent.Callevent("Jump");
                }
            }
        }

        velocity.y += gravity * Time.deltaTime;

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
        if (currentStamina <= 0f && !isStaminaEmpty)
        {
            isStaminaEmpty = true;
            // Removed OutOfStamina call as breathing system handles it
        }
    }

    public void TakeDamage(float damage)
    {
        if (currentState != PlayerState.Alive) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        lastHealthDamageTime = Time.time;

        // Call Hurt event for Wwise
        if (callEvent != null)
        {
            callEvent.Callevent("Hurt");
        }

        // Show damage flash on UI
        if (healthUIManager != null)
        {
            healthUIManager.ShowDamageFlash();
        }

        // Trigger camera shake when taking damage
        TriggerCameraShake();

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");
    }

    public void TriggerCameraShake()
    {
        if (currentState == PlayerState.Alive)
        {
            isShaking = true;
            shakeTimer = shakeDuration;
        }
    }

    void HandleShooting()
    {
        if (shooter != null && currentState == PlayerState.Alive)
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
        if (currentState != PlayerState.Alive) return;

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

    void OnDestroy()
    {
        // Stop breathing system when object is destroyed
        StopBreathingSystem();
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

    // Breathing system getters
    public BreathingLevel GetCurrentBreathingLevel()
    {
        return currentBreathingLevel;
    }

    public bool IsBreathingSystemActive()
    {
        return isBreathingEventPlaying;
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

    // State management
    public PlayerState GetPlayerState()
    {
        return currentState;
    }

    public bool IsPlayerAlive()
    {
        return currentState == PlayerState.Alive;
    }
    
    public void ReceiveZombieAttack(float damage)
{
    if (Time.time - lastZombieHitTime < zombieHitCooldown || currentState != PlayerState.Alive) return;

    callEvent.Callevent("DamageSound");
    TakeDamage(damage);
    lastZombieHitTime = Time.time;
}
}