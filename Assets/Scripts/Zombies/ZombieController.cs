using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    [Header("Zombie Stats")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float visionRadius = 5f;
    [SerializeField] private float personalSpaceRadius = 2f;
    [SerializeField] private float turnSpeed = 90f; // degrees per second
    
    [Header("Behavior Timings")]
    [SerializeField] private float idleActionInterval = 3f;
    [SerializeField] private float chaseTimeout = 60f; // 1 minute
    
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
        navAgent.angularSpeed = 0f; // We'll handle rotation manually
        navAgent.updateRotation = false; // Disable automatic rotation
        
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
        
        // Check for zombie avoidance (always active except when chasing)
        if (currentState != ZombieState.Chasing)
        {
            CheckZombieAvoidance();
        }
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
            return;
        }
        
        // Get the direction we need to move
        Vector3 pathDirection = (navAgent.steeringTarget - transform.position).normalized;
        pathDirection.y = 0; // Keep movement on horizontal plane
        
        // Calculate the angle between current forward direction and path direction
        float angleToPath = Vector3.SignedAngle(transform.forward, pathDirection, Vector3.up);
        
        // Check if we need to turn more than 45 degrees
        if (Mathf.Abs(angleToPath) > 45f)
        {
            // Stop moving and turn on the spot
            if (!isTurningToPath)
            {
                isTurningToPath = true;
                navAgent.isStopped = true;
                turnStartTime = Time.time;
            }
            
            // Turn towards the path direction
            float turnDirection = Mathf.Sign(angleToPath);
            float turnAmount = turnSpeed * Time.deltaTime * turnDirection;
            transform.Rotate(0, turnAmount, 0);
            
            // Check if we've turned enough to continue moving
            float newAngleToPath = Vector3.SignedAngle(transform.forward, pathDirection, Vector3.up);
            if (Mathf.Abs(newAngleToPath) <= 45f)
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
                float gentleTurnAmount = (turnSpeed * 0.3f) * Time.deltaTime * turnDirection; // 30% of normal turn speed
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
        int action = Random.Range(0, 3);
        
        switch (action)
        {
            case 0: // Stand still (do nothing)
                break;
            case 1: // Turn slowly
                StartTurning();
                break;
            case 2: // Move to nearby random position
                MoveToRandomNearbyPosition();
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
        
        // Calculate push direction away from nearby zombies
        Vector3 avoidanceDirection = Vector3.zero;
        foreach (ZombieController zombie in nearbyZombies)
        {
            if (zombie != null)
            {
                Vector3 directionAway = transform.position - zombie.transform.position;
                directionAway.y = 0;
                avoidanceDirection += directionAway.normalized / directionAway.magnitude;
            }
        }
        
        if (avoidanceDirection != Vector3.zero)
        {
            avoidanceDirection.Normalize();
            Vector3 targetPosition = transform.position + avoidanceDirection * 2f;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, 2f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
        }
        
        // Check if we've moved far enough away
        bool stillTooClose = false;
        foreach (ZombieController zombie in nearbyZombies)
        {
            if (zombie != null && Vector3.Distance(transform.position, zombie.transform.position) < personalSpaceRadius)
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
        
        // Always chase the player's current position (not last known position)
        navAgent.SetDestination(player.position);
        
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
        
        // Current target
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, navAgent.destination);
            Gizmos.DrawWireCube(navAgent.destination, Vector3.one * 0.5f);
        }
    }
    
    #endregion
}