using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum RangedEnemyState {
    Idle,
    Patrol,
    Chase,
    Attack,
    SearchForPlayer,
    TakingCover
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class RangedEnemyAI : MonoBehaviour {
    [Header("Target")]
    [Tooltip("The player transform (auto-finds if empty)")]
    public Transform player;
    private PlayerHealth playerHealth;

    [Header("Detection Settings")]
    [Tooltip("Distance to detect player")]
    public float detectionRange = 20f;
    [Tooltip("Field of view angle")]
    public float fieldOfView = 120f;
    [Tooltip("Distance to lose player")]
    public float loseTargetRange = 30f;
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

    [Header("Ranged Attack Settings")]
    [Tooltip("Distancia mínima para disparar")]
    public float minAttackRange = 5f;
    [Tooltip("Distancia máxima para disparar")]
    public float maxAttackRange = 20f;
    [Tooltip("Prefab de la bala")]
    public GameObject bulletPrefab;
    [Tooltip("Punto desde donde sale la bala")]
    public Transform bulletSpawnPoint;
    [Tooltip("Velocidad de la bala")]
    public float bulletSpeed = 50f;
    [Tooltip("Daño de la bala")]
    public float bulletDamage = 15f;
    [Tooltip("Tiempo entre disparos")]
    public float shootCooldown = 1.5f;
    [Tooltip("Número de disparos consecutivos")]
    public int burstSize = 1;
    [Tooltip("Tiempo entre disparos de ráfaga")]
    public float burstDelay = 0.3f;

    [Header("Movement Settings")]
    [Tooltip("Chase speed")]
    public float chaseSpeed = 4f;
    [Tooltip("Patrol speed")]
    public float patrolSpeed = 2f;
    [Tooltip("Stop distance from target")]
    public float stoppingDistance = 1.5f;
    [Tooltip("Mantiene distancia de este rango al jugador")]
    public float preferredAttackDistance = 10f;

    [Header("Animation")]
    [Tooltip("Animator component")]
    public Animator animator;
    [Tooltip("Speed parameter name")]
    public string speedParamName = "Speed";
    [Tooltip("Shoot trigger name")]
    public string shootTriggerName = "Shoot";

    [Header("Audio")]
    [Tooltip("Alert sound")]
    public AudioSource alertSound;
    [Tooltip("Shoot sound")]
    public AudioSource shootSound;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    // State
    public RangedEnemyState currentState = RangedEnemyState.Idle;
    [HideInInspector] public bool isAggro = false;
    
    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private float lastShootTime;
    private float lastSeenPlayerTime;
    private Vector3 lastKnownPlayerPosition;
    private int currentPatrolIndex = 0;
    private bool isWaitingAtPatrol = false;
    private Vector3 startPosition;
    private int burstCount = 0;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        if (!player) {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) {
                player = playerObj.transform;
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        startPosition = transform.position;
        agent.stoppingDistance = stoppingDistance;
        
        // Si no hay bulletSpawnPoint, crear uno
        if (!bulletSpawnPoint) {
            GameObject spawnPoint = new GameObject("BulletSpawnPoint");
            spawnPoint.transform.parent = transform;
            spawnPoint.transform.localPosition = Vector3.forward + Vector3.up * 0.5f;
            bulletSpawnPoint = spawnPoint.transform;
        }
        
        if (enablePatrol && patrolPoints.Length > 0) {
            currentState = RangedEnemyState.Patrol;
        } else if (enablePatrol && randomPatrol) {
            currentState = RangedEnemyState.Patrol;
        } else {
            currentState = RangedEnemyState.Idle;
        }
    }

    void Update() {
        // NUEVO: Detener completamente si está muerto
        if (enemyHealth.IsDead()) {
            if (agent) agent.isStopped = true;
            return;
        }

        if (!player || (playerHealth && playerHealth.IsDead())) {
            agent.isStopped = true;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState) {
            case RangedEnemyState.Idle:
                HandleIdle(distanceToPlayer);
                break;
            case RangedEnemyState.Patrol:
                HandlePatrol(distanceToPlayer);
                break;
            case RangedEnemyState.Chase:
                HandleChase(distanceToPlayer);
                break;
            case RangedEnemyState.Attack:
                HandleAttack(distanceToPlayer);
                break;
            case RangedEnemyState.SearchForPlayer:
                HandleSearch(distanceToPlayer);
                break;
            case RangedEnemyState.TakingCover:
                HandleTakingCover(distanceToPlayer);
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
            currentState = RangedEnemyState.Patrol;
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

        if (patrolPoints.Length > 0) {
            if (!agent.pathPending && agent.remainingDistance < 0.5f) {
                StartCoroutine(WaitAtPatrolPoint());
            }
        } else if (randomPatrol) {
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

        if (distanceToPlayer >= minAttackRange && distanceToPlayer <= maxAttackRange) {
            currentState = RangedEnemyState.Attack;
            return;
        }

        if (CanSeePlayer(distanceToPlayer)) {
            lastSeenPlayerTime = Time.time;
            lastKnownPlayerPosition = player.position;
            
            // Mantener distancia preferida
            if (distanceToPlayer < preferredAttackDistance) {
                Vector3 directionFromPlayer = (transform.position - player.position).normalized;
                agent.SetDestination(transform.position + directionFromPlayer * 3f);
            } else {
                agent.SetDestination(player.position);
            }
        } else if (distanceToPlayer > loseTargetRange || Time.time - lastSeenPlayerTime > 2f) {
            currentState = RangedEnemyState.SearchForPlayer;
            agent.SetDestination(lastKnownPlayerPosition);
        }
    }

    void HandleAttack(float distanceToPlayer) {
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        // Mover para mantener distancia
        if (distanceToPlayer < preferredAttackDistance * 0.8f) {
            Vector3 directionFromPlayer = (transform.position - player.position).normalized;
            agent.SetDestination(transform.position + directionFromPlayer * 2f);
        } else if (distanceToPlayer > maxAttackRange) {
            currentState = RangedEnemyState.Chase;
            return;
        }

        // Mirar al jugador
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero) {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        if (!CanSeePlayer(distanceToPlayer)) {
            currentState = RangedEnemyState.SearchForPlayer;
            return;
        }

        if (Time.time - lastShootTime > shootCooldown) {
            StartCoroutine(ShootBurst());
        }
    }

    void HandleTakingCover(float distanceToPlayer) {
        agent.isStopped = true;
        
        if (Time.time - lastShootTime > shootCooldown + 1f) {
            currentState = RangedEnemyState.Attack;
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
                currentState = enablePatrol ? RangedEnemyState.Patrol : RangedEnemyState.Idle;
            } else {
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
        currentState = RangedEnemyState.Chase;
        lastSeenPlayerTime = Time.time;
    }

    IEnumerator ShootBurst() {
        lastShootTime = Time.time; // Guardar tiempo ANTES de disparar
        
        for (int i = 0; i < burstSize; i++) {
            ShootAtPlayer();
            if (i < burstSize - 1) { // No esperar después del último disparo
                yield return new WaitForSeconds(burstDelay);
            }
        }
    }

    void ShootAtPlayer() {
        if (!bulletPrefab || !bulletSpawnPoint) {
            Debug.LogWarning(gameObject.name + " no tiene bulletPrefab o bulletSpawnPoint configurado");
            return;
        }

        if (animator) {
            animator.SetTrigger(shootTriggerName);
        }

        if (shootSound) {
            shootSound.Play();
        }

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb) {
            Vector3 directionToPlayer = (player.position + Vector3.up * 0.5f - bulletSpawnPoint.position).normalized;
            bulletRb.velocity = directionToPlayer * bulletSpeed;
            bullet.transform.forward = directionToPlayer;
        }

        Debug.Log(gameObject.name + " dispara al jugador");
    }

    void UpdateAnimation() {
        if (!animator) return;
    
        // NUEVO: No actualizar animaciones si está muerto
        if (enemyHealth && enemyHealth.IsDead()) {
            return;
        }

    float speed = agent.velocity.magnitude;
    animator.SetFloat(speedParamName, speed);
    }


    void OnDrawGizmosSelected() {
        if (!showDebugGizmos) return;
        if (transform == null) return;

        try {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, minAttackRange);
            Gizmos.DrawWireSphere(transform.position, maxAttackRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, preferredAttackDistance);

            Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfView / 2f, transform.up) * transform.forward * detectionRange;
            Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfView / 2f, transform.up) * transform.forward * detectionRange;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + fovLine1);
            Gizmos.DrawLine(transform.position, transform.position + fovLine2);

            if (patrolPoints != null && patrolPoints.Length > 0) {
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
        } catch {
            // Ignorar errores en el dibujado de gizmos
        }
    }
}