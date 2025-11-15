using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum EnemyState {
    Idle,
    Patrol,
    Chase,
    Attack,
    SearchForPlayer
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour {
    [Header("Target")]
    [Tooltip("The player transform (auto-finds if empty)")]
    public Transform player;
    private PlayerHealth playerHealth;

    [Header("Detection Settings")]
    [Tooltip("Distance to detect player")]
    public float detectionRange = 15f;
    [Tooltip("Field of view angle")]
    public float fieldOfView = 120f;
    [Tooltip("Distance to lose player")]
    public float loseTargetRange = 25f;
    [Tooltip("Time to search before giving up")]
    public float searchTime = 5f;
    [Tooltip("Check for obstacles between enemy and player")]
    public bool useLineOfSight = true;
    [Tooltip("Layer mask for obstacles")]
    public LayerMask obstacleMask;

    [Header("Patrol Settings")]
    [Tooltip("Enable patrol behavior")]
    public bool enablePatrol = true;
    [Tooltip("Patrol points")]
    public Transform[] patrolPoints;
    [Tooltip("Wait time at each patrol point")]
    public float patrolWaitTime = 2f;
    [Tooltip("Random patrol if no points set")]
    public bool randomPatrol = true;
    [Tooltip("Random patrol radius")]
    public float randomPatrolRadius = 10f;

    [Header("Combat Settings")]
    [Tooltip("Distance to start attacking")]
    public float attackRange = 2f;
    [Tooltip("Damage per attack")]
    public float attackDamage = 10f;
    [Tooltip("Time between attacks")]
    public float attackCooldown = 1.5f;
    [Tooltip("Stop moving when attacking")]
    public bool stopWhenAttacking = true;

    [Header("Movement Settings")]
    [Tooltip("Chase speed")]
    public float chaseSpeed = 5f;
    [Tooltip("Patrol speed")]
    public float patrolSpeed = 2f;
    [Tooltip("Stop distance from target")]
    public float stoppingDistance = 1.5f;

    [Header("Animation")]
    [Tooltip("Animator component")]
    public Animator animator;
    [Tooltip("Speed parameter name")]
    public string speedParamName = "Speed";
    [Tooltip("Attack trigger name")]
    public string attackTriggerName = "Attack";

    [Header("Audio")]
    [Tooltip("Alert sound")]
    public AudioSource alertSound;
    [Tooltip("Attack sound")]
    public AudioSource attackSound;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    // State
    public EnemyState currentState = EnemyState.Idle;
    [HideInInspector] public bool isAggro = false;
    
    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private float lastAttackTime;
    private float lastSeenPlayerTime;
    private Vector3 lastKnownPlayerPosition;
    private int currentPatrolIndex = 0;
    private bool isWaitingAtPatrol = false;
    private Vector3 startPosition;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        // Find player if not assigned
        if (!player) {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) {
                player = playerObj.transform;
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        startPosition = transform.position;
        agent.stoppingDistance = stoppingDistance;
        
        if (enablePatrol && patrolPoints.Length > 0) {
            currentState = EnemyState.Patrol;
        } else if (enablePatrol && randomPatrol) {
            currentState = EnemyState.Patrol;
        } else {
            currentState = EnemyState.Idle;
        }
    }

    void Update() {
        if (enemyHealth.IsDead() || !player || (playerHealth && playerHealth.IsDead())) {
            agent.isStopped = true;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // State machine
        switch (currentState) {
            case EnemyState.Idle:
                HandleIdle(distanceToPlayer);
                break;
            case EnemyState.Patrol:
                HandlePatrol(distanceToPlayer);
                break;
            case EnemyState.Chase:
                HandleChase(distanceToPlayer);
                break;
            case EnemyState.Attack:
                HandleAttack(distanceToPlayer);
                break;
            case EnemyState.SearchForPlayer:
                HandleSearch(distanceToPlayer);
                break;
        }

        UpdateAnimation();
    }

    void HandleIdle(float distanceToPlayer) {
        agent.isStopped = true;
        agent.speed = patrolSpeed;

        if (CanSeePlayer(distanceToPlayer)) {
            StartChasing();
        } else if (enablePatrol) {
            currentState = EnemyState.Patrol;
        }
    }

    void HandlePatrol(float distanceToPlayer) {
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        if (CanSeePlayer(distanceToPlayer)) {
            StartChasing();
            return;
        }

        if (isWaitingAtPatrol) return;

        // Patrol with waypoints
        if (patrolPoints.Length > 0) {
            if (!agent.pathPending && agent.remainingDistance < 0.5f) {
                StartCoroutine(WaitAtPatrolPoint());
            }
        }
        // Random patrol
        else if (randomPatrol) {
            if (!agent.pathPending && agent.remainingDistance < 0.5f) {
                StartCoroutine(WaitAtPatrolPoint());
            }
        }
    }

    IEnumerator WaitAtPatrolPoint() {
        isWaitingAtPatrol = true;
        yield return new WaitForSeconds(patrolWaitTime);
        
        if (patrolPoints.Length > 0) {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        } else if (randomPatrol) {
            Vector3 randomPoint = GetRandomPatrolPoint();
            agent.SetDestination(randomPoint);
        }
        
        isWaitingAtPatrol = false;
    }

    Vector3 GetRandomPatrolPoint() {
        Vector3 randomDirection = Random.insideUnitSphere * randomPatrolRadius;
        randomDirection += startPosition;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, randomPatrolRadius, NavMesh.AllAreas)) {
            return hit.position;
        }
        return startPosition;
    }

    void HandleChase(float distanceToPlayer) {
        agent.isStopped = false;
        agent.speed = chaseSpeed;

        if (distanceToPlayer <= attackRange) {
            currentState = EnemyState.Attack;
            return;
        }

        if (CanSeePlayer(distanceToPlayer)) {
            lastSeenPlayerTime = Time.time;
            lastKnownPlayerPosition = player.position;
            agent.SetDestination(player.position);
        } else if (distanceToPlayer > loseTargetRange || Time.time - lastSeenPlayerTime > 2f) {
            currentState = EnemyState.SearchForPlayer;
            agent.SetDestination(lastKnownPlayerPosition);
        }
    }

    void HandleAttack(float distanceToPlayer) {
        if (stopWhenAttacking) {
            agent.isStopped = true;
        } else {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // Look at player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero) {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        if (distanceToPlayer > attackRange * 1.2f) {
            currentState = EnemyState.Chase;
            return;
        }

        if (Time.time - lastAttackTime > attackCooldown) {
            PerformAttack();
        }
    }

    void HandleSearch(float distanceToPlayer) {
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        if (CanSeePlayer(distanceToPlayer)) {
            StartChasing();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f) {
            if (Time.time - lastSeenPlayerTime > searchTime) {
                isAggro = false;
                currentState = enablePatrol ? EnemyState.Patrol : EnemyState.Idle;
            } else {
                // Search nearby
                Vector3 searchPoint = lastKnownPlayerPosition + Random.insideUnitSphere * 5f;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(searchPoint, out hit, 5f, NavMesh.AllAreas)) {
                    agent.SetDestination(hit.position);
                }
            }
        }
    }

    bool CanSeePlayer(float distanceToPlayer) {
        if (distanceToPlayer > detectionRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > fieldOfView / 2f && !isAggro) return false;

        if (useLineOfSight) {
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up;
            Vector3 rayDirection = player.position + Vector3.up - rayStart;
            
            if (Physics.Raycast(rayStart, rayDirection, out hit, detectionRange, ~obstacleMask)) {
                if (hit.transform == player || hit.transform.root == player) {
                    return true;
                }
            }
            return false;
        }

        return true;
    }

    void StartChasing() {
        if (!isAggro) {
            isAggro = true;
            if (alertSound) {
                alertSound.Play();
            }
            Debug.Log(gameObject.name + " detected player!");
        }
        currentState = EnemyState.Chase;
        lastSeenPlayerTime = Time.time;
    }

    void PerformAttack() {
        lastAttackTime = Time.time;

        if (animator) {
            animator.SetTrigger(attackTriggerName);
        }

        if (attackSound) {
            attackSound.Play();
        }

        // Deal damage (called from animation event ideally)
        StartCoroutine(DealDamageAfterDelay(0.5f));
    }

    IEnumerator DealDamageAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        
        float currentDistance = Vector3.Distance(transform.position, player.position);
        if (currentDistance <= attackRange * 1.5f) {
            // Try to get PlayerHealth
            if (playerHealth == null) {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
            
            if (playerHealth) {
                Debug.Log(gameObject.name + " is attacking player for " + attackDamage + " damage");
                playerHealth.TakeDamage(attackDamage);
            } else {
                Debug.LogWarning(gameObject.name + " cannot find PlayerHealth component on player!");
            }
        }
    }

    public void AlertEnemy(Vector3 alertPosition) {
        lastKnownPlayerPosition = alertPosition;
        lastSeenPlayerTime = Time.time;
        isAggro = true;
        currentState = EnemyState.SearchForPlayer;
        agent.SetDestination(alertPosition);
    }

    void UpdateAnimation() {
        if (!animator) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(speedParamName, speed);
    }

    void OnDrawGizmosSelected() {
        if (!showDebugGizmos) return;

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Field of view
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfView / 2f, transform.up) * transform.forward * detectionRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfView / 2f, transform.up) * transform.forward * detectionRange;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);

        // Patrol points
        if (patrolPoints.Length > 0) {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++) {
                if (patrolPoints[i]) {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1]) {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                }
            }
        }
    }
}