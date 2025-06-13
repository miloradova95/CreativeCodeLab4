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
    [SerializeField] private float idleActionInterval = 2f;
    [SerializeField] private float chaseTimeout = 60f; // 1 minute
    
    [Header("Horde Behavior")]
    [SerializeField] private float playerAttractionStrength = 0.7f; // Increased for more aggressive movement toward player
    [SerializeField] private float avoidanceStrength = 1.0f;
    [SerializeField] private float proactiveMovementRange = 12f; // Increased range
    [SerializeField] private float hordeSpreadRadius = 3f; // How spread out the horde should be
    
    [Header("Chase Behavior")]
    [SerializeField] private float chaseUpdateInterval = 0.1f; // How often to update chase target
    [SerializeField] private float chaseSpreadDistance = 4f; // Distance zombies spread around player
    [SerializeField] private float chaseAvoidanceRadius = 2f; // Radius for avoiding other zombies while chasing
    
    [Header("Attack System")]
    [SerializeField] private float attackRange = 2.5f; // Range to trigger attack
    [SerializeField] private float attackDuration = 1.2f; // How long attack lasts
    [SerializeField] private float attackCooldown = 2f; // Cooldown between attacks
    [SerializeField] private float collisionDamageRadius = 1f; // Radius for collision damage
    
    [Header("Detection")]
    [SerializeField] private LayerMask playerLayer = -1;
    [SerializeField] private LayerMask zombieLayer = -1;
    [SerializeField] private LayerMask obstacleLayer = -1;
    
    // State management
    public enum ZombieState { Idling, Pushing, Chasing, Attacking }
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
    
    // Movement and turning
    private bool isTurningToPath;
    private float turnStartTime;
    
    // Idle behavior
    private Vector3 idleTarget;
    private bool isMovingToIdleTarget;
    private float turnDirection;
    private bool isTurning;
    
    // Pushing behavior
    private List<ZombieController> nearbyZombies = new List<ZombieController>();
    
    // Chase behavior
    private Vector3 chaseTarget;
    private float lastChaseUpdateTime;
    private int zombieIndex; // Unique index for horde positioning
    
    // Attack system
    private bool isAttacking;
    private float attackStartTime;
    private float lastAttackTime;
    private bool hasDealtCollisionDamage;
    
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
        navAgent.angularSpeed = 0f; // Disable NavMesh rotation
        navAgent.updateRotation = false; // Manual rotation control
        navAgent.stoppingDistance = 0.1f;
        
        // Fix capsule positioning
        FixCapsulePosition();
        
        // Add to global zombie list and assign index
        allZombies.Add(this);
        zombieIndex = allZombies.Count - 1;
        
        // Initialize idle behavior
        InitializeIdleBehavior();
    }
    
    void OnDestroy()
    {
        allZombies.Remove(this);
    }
    
    void Update()
    {
        // Always check for player detection (unless already chasing or attacking)
        if (currentState != ZombieState.Chasing && currentState != ZombieState.Attacking)
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
            case ZombieState.Attacking:
                HandleAttackingState();
                break;
        }
        
        // Check for zombie avoidance (not during attack)
        if (currentState != ZombieState.Attacking)
        {
            CheckZombieAvoidance();
        }
        
        // Check for collision damage
        CheckCollisionDamage();
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
            case ZombieState.Attacking:
                ExitAttackingState();
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
            case ZombieState.Attacking:
                EnterAttackingState();
                break;
        }
    }
    
    #endregion
    
    #region Movement and Rotation Control
    
    private void FixCapsulePosition()
    {
        if (capsuleCollider != null)
        {
            float capsuleHeight = capsuleCollider.height;
            capsuleCollider.center = new Vector3(0, capsuleHeight * 0.5f, 0);
        }
    }
    
    private void HandleMovementAndRotation()
{
    if (currentState == ZombieState.Attacking)
    {
        navAgent.isStopped = true;
        return;
    }
    
    if (!navAgent.hasPath || navAgent.remainingDistance < 0.1f)
    {
        isTurningToPath = false;
        if (navAgent.isStopped)
            navAgent.isStopped = false;
        return;
    }
    
    Vector3 pathDirection = (navAgent.steeringTarget - transform.position).normalized;
    pathDirection.y = 0;
    
    if (pathDirection.magnitude < 0.1f)
    {
        return;
    }
    
    float angleToPath = Vector3.SignedAngle(transform.forward, pathDirection, Vector3.up);
    float maxTurnAngle = 45f;
    
    if (Mathf.Abs(angleToPath) > maxTurnAngle)
    {
        if (!isTurningToPath)
        {
            isTurningToPath = true;
            navAgent.isStopped = true;
            turnStartTime = Time.time;
        }
        
        if (Time.time - turnStartTime > 2f)
        {
            isTurningToPath = false;
            navAgent.isStopped = false;
            return;
        }
        
        // Smooth turning with delta time
        float turnDirection = Mathf.Sign(angleToPath);
        float maxTurnThisFrame = turnSpeed * Time.deltaTime;
        float actualTurn = Mathf.Min(maxTurnThisFrame, Mathf.Abs(angleToPath)) * turnDirection;
        
        transform.Rotate(0, actualTurn, 0);
        
        float newAngleToPath = Vector3.SignedAngle(transform.forward, pathDirection, Vector3.up);
        if (Mathf.Abs(newAngleToPath) <= maxTurnAngle * 0.7f)
        {
            isTurningToPath = false;
            navAgent.isStopped = false;
        }
    }
    else
    {
        if (isTurningToPath)
        {
            isTurningToPath = false;
            navAgent.isStopped = false;
        }
        
        // Gradual turning while moving with delta time
        if (Mathf.Abs(angleToPath) > 5f)
        {
            float turnDirection = Mathf.Sign(angleToPath);
            float maxTurnThisFrame = (turnSpeed * 0.3f) * Time.deltaTime;
            float actualTurn = Mathf.Min(maxTurnThisFrame, Mathf.Abs(angleToPath)) * turnDirection;
            
            transform.Rotate(0, actualTurn, 0);
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
    if (Time.time - lastActionTime >= idleActionInterval)
    {
        PerformIdleAction();
        lastActionTime = Time.time;
    }
    
    if (isTurning)
    {
        float maxTurnThisFrame = turnSpeed * Time.deltaTime;
        float turnAmount = maxTurnThisFrame * turnDirection;
        transform.Rotate(0, turnAmount, 0);
        
        if (Time.time - lastActionTime >= Random.Range(0.5f, 1.5f))
        {
            isTurning = false;
        }
    }
    
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
    // Much more idle behavior - mostly standing still with occasional movement
    int action = Random.Range(0, 100);
    
    if (action < 40) // 40% chance to just stand still
    {
        // Do nothing - just idle
        return;
    }
    else if (action < 60) // 20% chance to turn in place
    {
        StartTurning();
    }
    else if (action < 80) // 20% chance to shuffle slightly toward player
    {
        ShuffleTowardPlayer();
    }
    else if (action < 95) // 15% chance to avoid nearby zombies
    {
        if (HasNearbyZombies())
        {
            MoveAwayFromNearbyZombies();
        }
        else
        {
            ShuffleTowardPlayer(); // Fallback to shuffling
        }
    }
    else // 5% chance for random shuffle
    {
        RandomShuffle();
    }
}

private bool HasNearbyZombies()
{
    Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, personalSpaceRadius * 0.8f, zombieLayer);
    return nearbyColliders.Length > 1; // More than just this zombie
}

private void RandomShuffle()
{
    float shuffleRange = 1.5f;
    Vector3 randomDirection = Random.insideUnitSphere;
    randomDirection.y = 0;
    randomDirection.Normalize();
    
    Vector3 targetPosition = transform.position + randomDirection * shuffleRange;
    targetPosition.y = transform.position.y;
    
    NavMeshHit hit;
    if (NavMesh.SamplePosition(targetPosition, out hit, shuffleRange, NavMesh.AllAreas))
    {
        idleTarget = hit.position;
        navAgent.SetDestination(idleTarget);
        isMovingToIdleTarget = true;
    }
}

private void ShuffleTowardPlayer()
{
    if (player == null) return;
    
    // Much smaller movement range for shuffling
    float shuffleRange = 2f; // Reduced from proactiveMovementRange
    
    Vector3 directionToPlayer = (player.position - transform.position).normalized;
    
    // Add slight randomness to avoid all zombies moving in exactly the same direction
    Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
    randomOffset.y = 0;
    
    Vector3 shuffleDirection = (directionToPlayer * 0.8f + randomOffset * 0.2f).normalized;
    Vector3 targetPosition = transform.position + shuffleDirection * shuffleRange;
    targetPosition.y = transform.position.y;
    
    NavMeshHit hit;
    if (NavMesh.SamplePosition(targetPosition, out hit, shuffleRange, NavMesh.AllAreas))
    {
        idleTarget = hit.position;
        navAgent.SetDestination(idleTarget);
        isMovingToIdleTarget = true;
    }
}
    
    private void StartTurning()
    {
        isTurning = true;
        turnDirection = Random.Range(-1f, 1f);
        lastActionTime = Time.time;
    }
    
private void MoveTowardPlayerBiasedPosition()
{
    MoveTowardPlayerBiasedPosition(proactiveMovementRange);
}

    private void MoveTowardPlayerBiasedPosition(float rangeOverride)
    {
        if (player == null) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 randomOffset = Random.insideUnitSphere * hordeSpreadRadius;
        randomOffset.y = 0;

        float angle = (zombieIndex * 137.5f) * Mathf.Deg2Rad;
        Vector3 spreadOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * hordeSpreadRadius;

        Vector3 biasedDirection = (directionToPlayer * playerAttractionStrength +
                                  (randomOffset + spreadOffset) * (1f - playerAttractionStrength)).normalized;

        Vector3 targetPosition = transform.position + biasedDirection * rangeOverride;
        targetPosition.y = transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, rangeOverride, NavMesh.AllAreas))
        {
            idleTarget = hit.position;
            navAgent.SetDestination(idleTarget);
            isMovingToIdleTarget = true;
        }
    }
    
   private void MoveAwayFromNearbyZombies()
{
    Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, personalSpaceRadius, zombieLayer);
    
    if (nearbyColliders.Length <= 1)
    {
        ShuffleTowardPlayer(); // Changed from MoveTowardPlayerBiasedPosition
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
                float influence = personalSpaceRadius / distance;
                avoidanceDirection += directionAway.normalized * influence;
                zombieCount++;
            }
        }
    }
    
    if (zombieCount > 0)
    {
        avoidanceDirection = avoidanceDirection.normalized;
        
        // Still mix with player attraction but much more subtle
        if (player != null)
        {
            Vector3 playerDirection = (player.position - transform.position).normalized;
            avoidanceDirection = (avoidanceDirection * avoidanceStrength + 
                                playerDirection * playerAttractionStrength * 0.3f).normalized;
        }
        
        // Smaller movement range for avoidance
        float avoidanceRange = 3f;
        Vector3 targetPosition = transform.position + avoidanceDirection * avoidanceRange;
        targetPosition.y = transform.position.y;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, avoidanceRange, NavMesh.AllAreas))
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
                    float influence = personalSpaceRadius / distance;
                    avoidanceDirection += directionAway.normalized * influence;
                }
            }
        }
        
        if (avoidanceDirection != Vector3.zero)
        {
            avoidanceDirection.Normalize();
            
            // Add player attraction
            if (player != null)
            {
                Vector3 playerDirection = (player.position - transform.position).normalized;
                avoidanceDirection = (avoidanceDirection * 0.7f + playerDirection * 0.3f).normalized;
            }
            
            Vector3 targetPosition = transform.position + avoidanceDirection * proactiveMovementRange;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, proactiveMovementRange, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
        }
        
        // Check if we've moved far enough away
        bool stillTooClose = false;
        foreach (ZombieController zombie in nearbyZombies)
        {
            if (zombie != null && Vector3.Distance(transform.position, zombie.transform.position) < personalSpaceRadius * 0.7f)
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
    
    #region Chasing State - Completely Rewritten
    
    private void EnterChasingState()
    {
        navAgent.speed = chaseSpeed;
        chaseStartTime = Time.time;
        hasSeenPlayerThisChase = false;
        lastPlayerSeenTime = Time.time;
        lastChaseUpdateTime = 0f;
        hasDealtCollisionDamage = false;
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
    float distanceToPlayer = Vector3.Distance(transform.position, player.position);
    
    // Check if we should attack
    if (canSeePlayer && distanceToPlayer <= attackRange && CanAttack())
    {
        SetState(ZombieState.Attacking);
        return;
    }
    
    if (canSeePlayer)
    {
        hasSeenPlayerThisChase = true;
        lastPlayerSeenTime = Time.time;
    }
    
    // Always update chase target - this was the main issue
    if (Time.time - lastChaseUpdateTime >= chaseUpdateInterval)
    {
        UpdateChaseTarget();
        lastChaseUpdateTime = Time.time;
    }
    
    // Always set destination - even if we're close, keep trying to get closer
    navAgent.SetDestination(chaseTarget);
    
    // Check if we should stop chasing
    if (hasSeenPlayerThisChase && !canSeePlayer)
    {
        float timeSincePlayerSeen = Time.time - lastPlayerSeenTime;
        if (timeSincePlayerSeen >= chaseTimeout)
        {
            SetState(ZombieState.Idling);
        }
    }
}
    
    private void UpdateChaseTarget()
{
    if (player == null) return;
    
    Vector3 playerPos = player.position;
    float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
    
    // If we're very close, try to get even closer instead of maintaining formation
    if (distanceToPlayer <= attackRange * 1.5f)
    {
        // Try to get as close as possible, with slight offset to avoid all zombies going to exact same spot
        Vector3 directApproach = playerPos + Random.insideUnitSphere * 0.5f;
        directApproach.y = playerPos.y;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(directApproach, out hit, 2f, NavMesh.AllAreas))
        {
            chaseTarget = hit.position;
        }
        else
        {
            chaseTarget = playerPos;
        }
        return;
    }
    
    // Normal formation behavior for longer distances
    float angle = (zombieIndex * 45f) * Mathf.Deg2Rad;
    float radius = chaseSpreadDistance + (zombieIndex % 3) * 1.5f;
    
    Vector3 formationOffset = new Vector3(
        Mathf.Cos(angle) * radius,
        0,
        Mathf.Sin(angle) * radius
    );
    
    Vector3 targetPosition = playerPos + formationOffset;
    
    // Avoid other zombies
    Vector3 avoidanceOffset = CalculateChaseAvoidance();
    targetPosition += avoidanceOffset;
    
    // Ensure target is on NavMesh
    NavMeshHit navHit;
    if (NavMesh.SamplePosition(targetPosition, out navHit, 10f, NavMesh.AllAreas))
    {
        chaseTarget = navHit.position;
    }
    else
    {
        chaseTarget = playerPos;
    }
}

    
    private Vector3 CalculateChaseAvoidance()
    {
        Vector3 avoidanceOffset = Vector3.zero;
        
        // Find nearby zombies
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, chaseAvoidanceRadius, zombieLayer);
        
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
                    avoidanceOffset += directionAway.normalized * influence * 2f;
                }
            }
        }
        
        return avoidanceOffset;
    }
    
    #endregion
    
    #region Attacking State - New
    
    private void EnterAttackingState()
    {
        navAgent.isStopped = true;
        isAttacking = true;
        attackStartTime = Time.time;
        lastAttackTime = Time.time;

        if (player != null)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            directionToPlayer.y = 0;
            if (directionToPlayer.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        StartAttackEffect();
    }
    
    private void ExitAttackingState()
    {
        navAgent.isStopped = false;
        isAttacking = false;
    }
    
    private void HandleAttackingState()
    {
        float elapsed = Time.time - attackStartTime;

        if (elapsed >= attackDuration)
        {
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    SetState(ZombieState.Attacking);
                }
                else
                {
                    SetState(ZombieState.Chasing);
                }
            }
            else
            {
                SetState(ZombieState.Chasing);
            }
            return;
        }

        float attackProgress = elapsed / attackDuration;
        if (attackProgress >= 0.4f && attackProgress <= 0.7f)
        {
            CheckAttackHit();
        }
    }
    
    private bool CanAttack()
    {
        return !isAttacking && (Time.time - lastAttackTime >= attackCooldown);
    }
    
    private void StartAttackEffect()
    {
        // Add visual/audio effects for attack start
        // This is where you'd trigger attack animations
        Debug.Log($"Zombie {name} starting attack!");
    }
    
    private void CheckAttackHit()
    {
        if (player == null) return;
        
        // Create attack hitbox in front of zombie
        Vector3 attackCenter = transform.position + transform.forward * (attackRange * 0.7f);
        
        // Check if player is in attack range
        if (Vector3.Distance(attackCenter, player.position) <= attackRange * 0.8f)
        {
            // Deal attack damage (placeholder)
            DealAttackDamage();
        }
    }
    
    private void DealAttackDamage()
    {
        // Placeholder for attack damage
        // This is where you'd subtract HP from player
        Debug.Log($"Zombie {name} dealt attack damage to player!");
    }
    
    #endregion
    
    #region Collision Damage System - New
    
    private void CheckCollisionDamage()
    {
        if (player == null || hasDealtCollisionDamage || currentState == ZombieState.Attacking) return;
        
        // Check if player is within collision damage radius
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= collisionDamageRadius)
        {
            // Deal collision damage (placeholder)
            DealCollisionDamage();
            hasDealtCollisionDamage = true;
            
            // Reset collision damage flag after a short delay
            StartCoroutine(ResetCollisionDamageFlag());
        }
    }
    
    private void DealCollisionDamage()
    {
        // Placeholder for collision damage
        // This is where you'd subtract HP from player
        Debug.Log($"Zombie {name} dealt collision damage to player!");
    }
    
    private IEnumerator ResetCollisionDamageFlag()
    {
        yield return new WaitForSeconds(1f); // 1 second cooldown on collision damage
        hasDealtCollisionDamage = false;
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
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;
        
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToPlayer, out hit, visionRadius, obstacleLayer))
        {
            return false;
        }
        
        return true;
    }
    
    private void CheckZombieAvoidance()
    {
        if (currentState == ZombieState.Chasing || currentState == ZombieState.Attacking)
        {
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
        
        if (nearbyZombies.Count > 0 && currentState == ZombieState.Idling)
        {
            SetState(ZombieState.Pushing);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void StartChasing()
    {
        SetState(ZombieState.Chasing);
    }
    
    public void StopChasing()
    {
        if (currentState == ZombieState.Chasing)
        {
            SetState(ZombieState.Idling);
        }
    }
    
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
        
        // Attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Collision damage radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, collisionDamageRadius);
        
        // Chase avoidance radius (when chasing)
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
        
        // Attack hitbox visualization
        if (currentState == ZombieState.Attacking)
        {
            Gizmos.color = Color.red;
            Vector3 attackCenter = transform.position + transform.forward * (attackRange * 0.7f);
            Gizmos.DrawWireSphere(attackCenter, attackRange * 0.8f);
        }
    }
    
    #endregion
}