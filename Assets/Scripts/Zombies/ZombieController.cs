using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    [Header("Zombie Stats")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float visionRadius = 50f;
    [SerializeField] private float personalSpaceRadius = 20f;
    [SerializeField] private float turnSpeed = 120f; // degrees per second
    
    [Header("Behavior Timings")]
    [SerializeField] private float idleActionInterval = 3f;
    [SerializeField] private float chaseTimeout = 60f; // 1 minute
    
    [Header("Horde Behavior")]
    [SerializeField] private float playerAttractionStrength = 0.3f; // How much zombies are drawn to player while idling
    [SerializeField] private float avoidanceStrength = 1.0f; // How strongly zombies avoid each other
    [SerializeField] private float proactiveMovementRange = 8f; // How far zombies will move proactively
    
    [Header("Chase Avoidance")]
    [SerializeField] private float chaseAvoidanceRadius = 5f; // Smaller radius for chase avoidance
    [SerializeField] private float chaseAvoidanceStrength = 0.3f; // How much to deviate from direct path to player
    
    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer = -1;
    [SerializeField] private LayerMask zombieLayer = -1;
    [SerializeField] private LayerMask obstacleLayer = -1;
    
    // State management
    public enum ZombieState { Idling, Pushing, Chasing }
    private ZombieState currentState = ZombieState.Idling;
    
    // Components
    private NavMeshAgent navAgent;
    private Transform player;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    
    // Timers and flags
    private float lastActionTime;
    private float chaseStartTime;
    private float lastPlayerSeenTime;
    private bool hasSeenPlayerThisChase;
    private Vector3 lastKnownPlayerPosition;
    
    // Movement and turning
    private bool isTurningToPath;
    private Vector3 targetMoveDirection;
    private float turnStartTime;
    
    // Idle behavior
    private Vector3 idleTarget;
    private bool isMovingToIdleTarget;
    private float turnDirection;
    private bool isTurning;
    
    // Pushing behavior
    private List<ZombieController> nearbyZombies = new List<ZombieController>();
    private Vector3 pushDirection;
    
    // Chase avoidance
    private Vector3 chaseTarget;
    private float lastChaseTargetUpdateTime;
    private float chaseTargetUpdateInterval = 0.2f; // Update chase target 5 times per second
    
    // Static list to track all zombies
    private static List<ZombieController> allZombies = new List<ZombieController>();
    
    void Start()
    {
        // Get components
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        if (navAgent == null)
        {
            Debug.LogError("NavMeshAgent component required on zombie!");
            return;
        }
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        // Setup NavMeshAgent
        navAgent.speed = moveSpeed;
        navAgent.acceleration = 8f;
        navAgent.angularSpeed = 90f; // Re-enable some angular speed to help with navigation
        navAgent.updateRotation = false; // Keep manual rotation but allow some NavMesh assistance
        
        // Fix capsule positioning
        FixCapsulePosition();
        
        // Add to global zombie list
        allZombies.Add(this);
        
        // Initialize idle behavior
        InitializeIdleBehavior();
    }
    
    void OnDestroy()
    {
        allZombies.Remove(this);
    }
    
    void Update()
    {
        // Always check for player detection (unless already chasing)
        if (currentState != ZombieState.Chasing)
        {
            CheckPlayerDetection();
        }
        
        // Handle movement and rotation
        HandleMovementAndRotation();
        
        // Handle state-specific behavior
        switch (currentState)
        {
            case ZombieState.Idling:
                HandleIdleState();
                break;
            case ZombieState.Pushing:
                HandlePushingState();
                break;
            case ZombieState.Chasing:
                HandleChasingState();
                break;
        }
        
        // Check for zombie avoidance - now includes chase avoidance
        CheckZombieAvoidance();
    }
    
    #region State Management
    
    private void SetState(ZombieState newState)
    {
        if (currentState == newState) return;
        
        // Exit current state
        switch (currentState)
        {
            case ZombieState.Idling:
                ExitIdleState();
                break;
            case ZombieState.Pushing:
                ExitPushingState();
                break;
            case ZombieState.Chasing:
                ExitChasingState();
                break;
        }
        
        currentState = newState;
        
        // Enter new state
        switch (newState)
        {
            case ZombieState.Idling:
                EnterIdleState();
                break;
            case ZombieState.Pushing:
                EnterPushingState();
                break;
            case ZombieState.Chasing:
                EnterChasingState();
                break;
        }
    }
    
    #endregion
    
    #region Movement and Rotation Control
    
    private void FixCapsulePosition()
    {
        if (capsuleCollider != null)
        {
            // Adjust the capsule so it sits properly on the ground
            float capsuleHeight = capsuleCollider.height;
            capsuleCollider.center = new Vector3(0, capsuleHeight * 0.5f, 0);
        }
    }
    
    private void HandleMovementAndRotation()
    {
        if (!navAgent.hasPath || navAgent.remainingDistance < 0.1f)
        {
            isTurningToPath = false;
            if (navAgent.isStopped)
                navAgent.isStopped = false;
            return;
        }
        
        // Get the direction we need to move
        Vector3 pathDirection = (navAgent.steeringTarget - transform.position).normalized;
        pathDirection.y = 0; // Keep movement on horizontal plane
        
        // Skip rotation handling if direction is invalid
        if (pathDirection.magnitude < 0.1f)
        {
            return;
        }
        
        // Calculate the angle between current forward direction and path direction
        float angleToPath = Vector3.SignedAngle(transform.forward, pathDirection, Vector3.up);
        
        // More aggressive turning thresholds and timeout system
        float maxTurnAngle = (currentState == ZombieState.Chasing) ? 60f : 45f; // Allow wider turns during chase
        
        // Check if we need to turn more than the threshold
        if (Mathf.Abs(angleToPath) > maxTurnAngle)
        {
            // Stop moving and turn on the spot
            if (!isTurningToPath)
            {
                isTurningToPath = true;
                navAgent.isStopped = true;
                turnStartTime = Time.time;
            }
            
            // Add timeout to prevent infinite turning
            if (Time.time - turnStartTime > 3f) // 3 second timeout
            {
                // Force resume movement and let NavMesh handle it
                isTurningToPath = false;
                navAgent.isStopped = false;
                return;
            }
            
            // Turn towards the path direction with faster turning during chase
            float currentTurnSpeed = (currentState == ZombieState.Chasing) ? turnSpeed * 1.5f : turnSpeed;
            float turnDirection = Mathf.Sign(angleToPath);
            float turnAmount = currentTurnSpeed * Time.deltaTime * turnDirection;
            transform.Rotate(0, turnAmount, 0);
            
            // Check if we've turned enough to continue moving (more lenient)
            float newAngleToPath = Vector3.SignedAngle(transform.forward, pathDirection, Vector3.up);
            if (Mathf.Abs(newAngleToPath) <= maxTurnAngle * 0.8f) // Resume at 80% of max angle
            {
                isTurningToPath = false;
                navAgent.isStopped = false;
            }
        }
        else
        {
            // We can move forward, make sure we're not stopped
            if (isTurningToPath)
            {
                isTurningToPath = false;
                navAgent.isStopped = false;
            }
            
            // Gradually turn while moving (small adjustments)
            if (Mathf.Abs(angleToPath) > 5f) // Only adjust if we're off by more than 5 degrees
            {
                float turnDirection = Mathf.Sign(angleToPath);
                float gentleTurnMultiplier = (currentState == ZombieState.Chasing) ? 0.5f : 0.3f; // Faster gentle turns during chase
                float gentleTurnAmount = (turnSpeed * gentleTurnMultiplier) * Time.deltaTime * turnDirection;
                transform.Rotate(0, gentleTurnAmount, 0);
            }
        }
    }
    
    #endregion
    
    #region Idle State
    
    private void EnterIdleState()
    {
        navAgent.speed = moveSpeed;
        InitializeIdleBehavior();
    }
    
    private void ExitIdleState()
    {
        navAgent.ResetPath();
        isTurning = false;
        isMovingToIdleTarget = false;
    }
    
    private void HandleIdleState()
    {
        // Perform idle actions at intervals
        if (Time.time - lastActionTime >= idleActionInterval)
        {
            PerformIdleAction();
            lastActionTime = Time.time;
        }
        
        // Handle turning
        if (isTurning)
        {
            float turnAmount = turnSpeed * Time.deltaTime * turnDirection;
            transform.Rotate(0, turnAmount, 0);
            
            // Stop turning after a random duration
            if (Time.time - lastActionTime >= Random.Range(1f, 3f))
            {
                isTurning = false;
            }
        }
        
        // Handle movement to idle target
        if (isMovingToIdleTarget && navAgent.hasPath)
        {
            if (navAgent.remainingDistance < 0.5f)
            {
                isMovingToIdleTarget = false;
                navAgent.ResetPath();
            }
        }
    }
    
    private void InitializeIdleBehavior()
    {
        lastActionTime = Time.time;
        turnDirection = Random.Range(-1f, 1f);
    }
    
    private void PerformIdleAction()
    {
        // More frequent movement with player attraction
        int action = Random.Range(0, 4);
        
        switch (action)
        {
            case 0: // Stand still (less common now)
                break;
            case 1: // Turn slowly
                StartTurning();
                break;
            case 2: // Move to nearby random position with player bias
                MoveTowardPlayerBiasedPosition();
                break;
            case 3: // Actively move away from nearby zombies
                MoveAwayFromNearbyZombies();
                break;
        }
    }
    
    private void StartTurning()
    {
        isTurning = true;
        turnDirection = Random.Range(-1f, 1f);
        lastActionTime = Time.time;
    }
    
    private void MoveToRandomNearbyPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 3f;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y; // Keep same height
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 3f, NavMesh.AllAreas))
        {
            idleTarget = hit.position;
            navAgent.SetDestination(idleTarget);
            isMovingToIdleTarget = true;
        }
    }
    
    private void MoveTowardPlayerBiasedPosition()
    {
        if (player == null)
        {
            MoveToRandomNearbyPosition();
            return;
        }
        
        // Calculate direction toward player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        
        // Add some randomness to avoid direct movement
        Vector3 randomOffset = Random.insideUnitSphere * 2f;
        randomOffset.y = 0;
        
        // Combine player direction with randomness
        Vector3 biasedDirection = (directionToPlayer * playerAttractionStrength + randomOffset * (1f - playerAttractionStrength)).normalized;
        
        // Calculate target position
        Vector3 targetPosition = transform.position + biasedDirection * proactiveMovementRange;
        targetPosition.y = transform.position.y;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, proactiveMovementRange, NavMesh.AllAreas))
        {
            idleTarget = hit.position;
            navAgent.SetDestination(idleTarget);
            isMovingToIdleTarget = true;
        }
        else
        {
            // Fallback to random movement if biased position is invalid
            MoveToRandomNearbyPosition();
        }
    }
    
    private void MoveAwayFromNearbyZombies()
    {
        // Find all nearby zombies within personal space
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, personalSpaceRadius, zombieLayer);
        
        if (nearbyColliders.Length <= 1) // Only ourselves
        {
            MoveTowardPlayerBiasedPosition();
            return;
        }
        
        Vector3 avoidanceDirection = Vector3.zero;
        int zombieCount = 0;
        
        foreach (Collider col in nearbyColliders)
        {
            ZombieController otherZombie = col.GetComponent<ZombieController>();
            if (otherZombie != null && otherZombie != this)
            {
                Vector3 directionAway = transform.position - otherZombie.transform.position;
                directionAway.y = 0;
                float distance = directionAway.magnitude;
                
                if (distance > 0)
                {
                    // Closer zombies have more influence
                    float influence = personalSpaceRadius / distance;
                    avoidanceDirection += directionAway.normalized * influence;
                    zombieCount++;
                }
            }
        }
        
        if (zombieCount > 0)
        {
            avoidanceDirection = avoidanceDirection.normalized;
            
            // Mix avoidance with slight player attraction
            if (player != null)
            {
                Vector3 playerDirection = (player.position - transform.position).normalized;
                avoidanceDirection = (avoidanceDirection * avoidanceStrength + playerDirection * playerAttractionStrength * 0.3f).normalized;
            }
            
            Vector3 targetPosition = transform.position + avoidanceDirection * proactiveMovementRange;
            targetPosition.y = transform.position.y;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, proactiveMovementRange, NavMesh.AllAreas))
            {
                idleTarget = hit.position;
                navAgent.SetDestination(idleTarget);
                isMovingToIdleTarget = true;
            }
        }
    }
    
    #endregion
    
    #region Pushing State
    
    private void EnterPushingState()
    {
        navAgent.speed = moveSpeed;
    }
    
    private void ExitPushingState()
    {
        navAgent.ResetPath();
    }
    
    private void HandlePushingState()
    {
        if (nearbyZombies.Count == 0)
        {
            SetState(ZombieState.Idling);
            return;
        }
        
        // Calculate more aggressive push direction away from nearby zombies
        Vector3 avoidanceDirection = Vector3.zero;
        foreach (ZombieController zombie in nearbyZombies)
        {
            if (zombie != null)
            {
                Vector3 directionAway = transform.position - zombie.transform.position;
                directionAway.y = 0;
                float distance = directionAway.magnitude;
                
                if (distance > 0)
                {
                    // Stronger influence for closer zombies
                    float influence = personalSpaceRadius / distance;
                    avoidanceDirection += directionAway.normalized * influence;
                }
            }
        }
        
        if (avoidanceDirection != Vector3.zero)
        {
            avoidanceDirection.Normalize();
            
            // Add slight player attraction even while pushing
            if (player != null)
            {
                Vector3 playerDirection = (player.position - transform.position).normalized;
                avoidanceDirection = (avoidanceDirection * 0.8f + playerDirection * 0.2f).normalized;
            }
            
            Vector3 targetPosition = transform.position + avoidanceDirection * proactiveMovementRange;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, proactiveMovementRange, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
        }
        
        // Check if we've moved far enough away (more lenient check)
        bool stillTooClose = false;
        foreach (ZombieController zombie in nearbyZombies)
        {
            if (zombie != null && Vector3.Distance(transform.position, zombie.transform.position) < personalSpaceRadius * 0.8f)
            {
                stillTooClose = true;
                break;
            }
        }
        
        if (!stillTooClose)
        {
            SetState(ZombieState.Idling);
        }
    }
    
    #endregion
    
    #region Chasing State
    
    private void EnterChasingState()
    {
        navAgent.speed = chaseSpeed;
        chaseStartTime = Time.time;
        hasSeenPlayerThisChase = false;
        lastPlayerSeenTime = Time.time;
        lastChaseTargetUpdateTime = 0f; // Force immediate update
    }
    
    private void ExitChasingState()
    {
        navAgent.ResetPath();
        hasSeenPlayerThisChase = false;
    }
    
    private void HandleChasingState()
    {
        if (player == null) return;
        
        bool canSeePlayer = CanSeePlayer();
        
        if (canSeePlayer)
        {
            hasSeenPlayerThisChase = true;
            lastPlayerSeenTime = Time.time;
        }
        
        // Update chase target periodically to include avoidance
        if (Time.time - lastChaseTargetUpdateTime >= chaseTargetUpdateInterval)
        {
            CalculateChaseTarget();
            lastChaseTargetUpdateTime = Time.time;
        }
        
        // Use the calculated chase target instead of directly chasing player
        navAgent.SetDestination(chaseTarget);
        
        // Check if we should stop chasing - only if we've seen the player at least once
        // and they've been outside our vision bubble for the full timeout duration
        if (hasSeenPlayerThisChase && !canSeePlayer)
        {
            float timeSincePlayerSeen = Time.time - lastPlayerSeenTime;
            if (timeSincePlayerSeen >= chaseTimeout)
            {
                SetState(ZombieState.Idling);
            }
        }
    }
    
    private void CalculateChaseTarget()
{
    if (player == null) return;
    
    Vector3 directPlayerDirection = (player.position - transform.position).normalized;
    Vector3 adjustedDirection = directPlayerDirection;
    
    // Find nearby zombies during chase
    Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, chaseAvoidanceRadius, zombieLayer);
    
    Vector3 avoidanceDirection = Vector3.zero;
    int nearbyCount = 0;
    
    foreach (Collider col in nearbyColliders)
    {
        ZombieController otherZombie = col.GetComponent<ZombieController>();
        if (otherZombie != null && otherZombie != this)
        {
            Vector3 directionAway = transform.position - otherZombie.transform.position;
            directionAway.y = 0;
            float distance = directionAway.magnitude;
            
            if (distance > 0 && distance < chaseAvoidanceRadius)
            {
                // Closer zombies have more influence
                float influence = (chaseAvoidanceRadius - distance) / chaseAvoidanceRadius;
                avoidanceDirection += directionAway.normalized * influence;
                nearbyCount++;
            }
        }
    }
    
    // Calculate target position
    Vector3 targetPosition = player.position;
    
    if (nearbyCount > 0)
    {
        avoidanceDirection = avoidanceDirection.normalized;
        
        // Add offset to avoid clustering, now using chaseAvoidanceStrength
        Vector3 offset = avoidanceDirection * chaseAvoidanceStrength * Mathf.Min(2f, chaseAvoidanceRadius * 0.4f);
        targetPosition = player.position + offset;
    }
    
    // Only update chase target if the new target is significantly different
    // This prevents constant micro-adjustments that cause turning issues
    if (Vector3.Distance(chaseTarget, targetPosition) > 1f)
    {
        // Ensure the target is on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
        {
            chaseTarget = hit.position;
        }
        else
        {
            // Fallback to direct player position if offset position is invalid
            chaseTarget = player.position;
        }
    }
    // If the change is small, keep the existing chase target to maintain path stability
}
    
    #endregion
    
    #region Detection and Vision
    
    private void CheckPlayerDetection()
    {
        if (player == null) return;
        
        if (CanSeePlayer())
        {
            SetState(ZombieState.Chasing);
        }
    }
    
    private bool CanSeePlayer()
    {
        if (player == null) return false;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > visionRadius) return false;
        
        // Check if there are obstacles blocking vision
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToPlayer, out hit, visionRadius, obstacleLayer))
        {
            return false; // Obstacle is blocking vision
        }
        
        return true;
    }
    
    private void CheckZombieAvoidance()
    {
        if (currentState == ZombieState.Chasing)
        {
            // Chase state handles its own avoidance through CalculateChaseTarget()
            return;
        }
        
        nearbyZombies.Clear();
        
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, personalSpaceRadius, zombieLayer);
        
        foreach (Collider col in nearbyColliders)
        {
            ZombieController otherZombie = col.GetComponent<ZombieController>();
            if (otherZombie != null && otherZombie != this)
            {
                nearbyZombies.Add(otherZombie);
            }
        }
        
        // If we detect zombies too close and we're not already pushing, start pushing
        if (nearbyZombies.Count > 0 && currentState == ZombieState.Idling)
        {
            SetState(ZombieState.Pushing);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Force this zombie to start chasing (can be called from external events)
    /// </summary>
    public void StartChasing()
    {
        SetState(ZombieState.Chasing);
    }
    
    /// <summary>
    /// Stop chasing and return to idle
    /// </summary>
    public void StopChasing()
    {
        if (currentState == ZombieState.Chasing)
        {
            SetState(ZombieState.Idling);
        }
    }
    
    /// <summary>
    /// Get current state for debugging or external systems
    /// </summary>
    public ZombieState GetCurrentState()
    {
        return currentState;
    }
    
    #endregion
    
    #region Debug Visualization
    
    void OnDrawGizmosSelected()
    {
        // Vision radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
        
        // Personal space radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, personalSpaceRadius);
        
        // Chase avoidance radius (only when chasing)
        if (currentState == ZombieState.Chasing)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, chaseAvoidanceRadius);
        }
        
        // Current target
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, navAgent.destination);
            Gizmos.DrawWireCube(navAgent.destination, Vector3.one * 0.5f);
        }
        
        // Chase target (when chasing)
        if (currentState == ZombieState.Chasing)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(chaseTarget, Vector3.one * 0.3f);
        }
    }
    
    #endregion
}