using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum BossState {
    Idle,
    Chase,
    RangedAttack,
    AreaAttack,
    Phase2,
    Enraged
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class BossEnemyAI : MonoBehaviour {
    [Header("Target")]
    [Tooltip("The player transform (auto-finds if empty)")]
    public Transform player;
    private PlayerHealth playerHealth;

    [Header("Detection Settings")]
    [Tooltip("Distance to detect player")]
    public float detectionRange = 30f;
    [Tooltip("Field of view angle")]
    public float fieldOfView = 180f;
    [Tooltip("Check for obstacles between boss and player")]
    public bool useLineOfSight = true;
    [Tooltip("Layer mask for obstacles")]
    public LayerMask obstacleMask;

    [Header("Health & Phase Settings")]
    [Tooltip("Salud a la que entra en Phase 2")]
    public float phase2HealthThreshold = 0.5f;
    [Tooltip("Salud a la que entra en modo Enraged")]
    public float enragedHealthThreshold = 0.25f;

    [Header("Ranged Attack Settings")]
    [Tooltip("Distancia para disparos normales")]
    public float rangedAttackRange = 15f;
    [Tooltip("Tiempo entre disparos normales")]
    public float rangedCooldown = 1f;
    [Tooltip("Número de disparos en ráfaga")]
    public int rangedBurstSize = 3;
    [Tooltip("Prefab de bala")]
    public GameObject bulletPrefab;
    [Tooltip("Punto de spawn de balas")]
    public Transform bulletSpawnPoint;
    [Tooltip("Velocidad de bala")]
    public float bulletSpeed = 50f;
    [Tooltip("Daño de bala")]
    public float bulletDamage = 20f;

    [Header("Area Attack Settings")]
    [Tooltip("Distancia para ataque de área")]
    public float areaAttackRange = 20f;
    [Tooltip("Tiempo entre ataques de área")]
    public float areaAttackCooldown = 4f;
    [Tooltip("Radio del ataque de área")]
    public float areaAttackRadius = 8f;
    [Tooltip("Daño de ataque de área")]
    public float areaAttackDamage = 30f;
    [Tooltip("Prefab del efecto de área")]
    public GameObject areaAttackEffect;
    [Tooltip("Duración del efecto de área")]
    public float areaAttackDuration = 1f;

    [Header("Movement Settings")]
    [Tooltip("Velocidad de persecución")]
    public float chaseSpeed = 6f;
    [Tooltip("Distancia de parada")]
    public float stoppingDistance = 1.5f;
    [Tooltip("Distancia preferida para atacar")]
    public float preferredAttackDistance = 12f;

    [Header("Animation")]
    public Animator animator;
    public string speedParamName = "Speed";
    public string shootTriggerName = "Shoot";
    public string areaAttackTriggerName = "AreaAttack";

    [Header("Audio")]
    public AudioSource alertSound;
    public AudioSource shootSound;
    public AudioSource areaAttackSound;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    // State
    public BossState currentState = BossState.Idle;
    [HideInInspector] public bool isAggro = false;
    
    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private float lastShootTime;
    private float lastAreaAttackTime;
    private Vector3 startPosition;
    private bool isInPhase2 = false;
    private bool isEnraged = false;

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
        
        if (!bulletSpawnPoint) {
            GameObject spawnPoint = new GameObject("BulletSpawnPoint");
            spawnPoint.transform.parent = transform;
            spawnPoint.transform.localPosition = Vector3.forward + Vector3.up * 1f;
            bulletSpawnPoint = spawnPoint.transform;
        }

        currentState = BossState.Idle;
    }

    void Update() {
        if (enemyHealth.IsDead() || !player || (playerHealth && playerHealth.IsDead())) {
            agent.isStopped = true;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float healthPercentage = enemyHealth.currentHealth / enemyHealth.maxHealth;

        // Check phase transitions
        if (healthPercentage <= enragedHealthThreshold && !isEnraged) {
            EnterEnragedMode();
        } else if (healthPercentage <= phase2HealthThreshold && !isInPhase2) {
            EnterPhase2();
        }

        switch (currentState) {
            case BossState.Idle:
                HandleIdle(distanceToPlayer);
                break;
            case BossState.Chase:
                HandleChase(distanceToPlayer);
                break;
            case BossState.RangedAttack:
                HandleRangedAttack(distanceToPlayer);
                break;
            case BossState.AreaAttack:
                HandleAreaAttack(distanceToPlayer);
                break;
            case BossState.Phase2:
                HandlePhase2(distanceToPlayer);
                break;
            case BossState.Enraged:
                HandleEnraged(distanceToPlayer);
                break;
        }

        UpdateAnimation();
    }

    void HandleIdle(float distanceToPlayer) {
        agent.isStopped = true;

        if (CanSeePlayer(distanceToPlayer)) {
            StartChasing();
        }
    }

    void HandleChase(float distanceToPlayer) {
        agent.isStopped = false;
        agent.speed = chaseSpeed;

        if (distanceToPlayer <= rangedAttackRange) {
            currentState = BossState.RangedAttack;
            return;
        }

        if (CanSeePlayer(distanceToPlayer)) {
            agent.SetDestination(player.position);
        } else {
            currentState = BossState.Idle;
        }
    }

    void HandleRangedAttack(float distanceToPlayer) {
        agent.isStopped = false;
        agent.speed = chaseSpeed * 0.5f;

        // Decidir si atacar de área
        if (distanceToPlayer <= areaAttackRange && Time.time - lastAreaAttackTime > areaAttackCooldown) {
            currentState = BossState.AreaAttack;
            return;
        }

        // Mantener distancia
        if (distanceToPlayer < preferredAttackDistance * 0.8f) {
            Vector3 directionFromPlayer = (transform.position - player.position).normalized;
            agent.SetDestination(transform.position + directionFromPlayer * 2f);
        } else if (distanceToPlayer > rangedAttackRange) {
            currentState = BossState.Chase;
            return;
        }

        // Mirar al jugador
        LookAtPlayer();

        if (!CanSeePlayer(distanceToPlayer)) {
            currentState = BossState.Chase;
            return;
        }

        // Disparar
        if (Time.time - lastShootTime > rangedCooldown) {
            StartCoroutine(ShootBurst(rangedBurstSize));
        }
    }

    void HandleAreaAttack(float distanceToPlayer) {
        agent.isStopped = true;
        LookAtPlayer();

        if (Time.time - lastAreaAttackTime > areaAttackCooldown) {
            PerformAreaAttack();
            currentState = BossState.RangedAttack;
        }
    }

    void HandlePhase2(float distanceToPlayer) {
        // En Phase 2 dispara más rápido y ataca de área más frecuentemente
        HandleRangedAttack(distanceToPlayer);
    }

    void HandleEnraged(float distanceToPlayer) {
        agent.isStopped = false;
        agent.speed = chaseSpeed * 1.5f;

        // En modo enraged ataca constantemente
        if (distanceToPlayer <= areaAttackRange) {
            PerformAreaAttack();
        } else {
            agent.SetDestination(player.position);
            
            if (distanceToPlayer <= rangedAttackRange && Time.time - lastShootTime > rangedCooldown * 0.5f) {
                StartCoroutine(ShootBurst(rangedBurstSize + 2));
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
            Debug.Log("BOSS DETECTED PLAYER!");
        }
        currentState = BossState.Chase;
    }

    void LookAtPlayer() {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero) {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 8f);
        }
    }

    IEnumerator ShootBurst(int burstCount) {
        for (int i = 0; i < burstCount; i++) {
            ShootAtPlayer();
            yield return new WaitForSeconds(0.2f);
        }
        lastShootTime = Time.time;
    }

    void ShootAtPlayer() {
        if (!bulletPrefab || !bulletSpawnPoint) return;

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
    }

    void PerformAreaAttack() {
        lastAreaAttackTime = Time.time;

        if (animator) {
            animator.SetTrigger(areaAttackTriggerName);
        }

        if (areaAttackSound) {
            areaAttackSound.Play();
        }

        // Crear efecto visual
        if (areaAttackEffect) {
            GameObject effect = Instantiate(areaAttackEffect, player.position, Quaternion.identity);
            Destroy(effect, areaAttackDuration);
        }

        // Aplicar daño al jugador si está en rango
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= areaAttackRadius && playerHealth) {
            playerHealth.TakeDamage(areaAttackDamage);
            Debug.Log("BOSS AREA ATTACK - Player hit for " + areaAttackDamage + " damage!");
        }

        Debug.Log("BOSS performs AREA ATTACK!");
    }

    void EnterPhase2() {
        isInPhase2 = true;
        currentState = BossState.Phase2;
        
        rangedCooldown *= 0.7f; // Disparar más rápido
        areaAttackCooldown *= 0.8f; // Ataques de área más frecuentes
        rangedBurstSize += 1;
        
        Debug.Log("BOSS enters PHASE 2! Increased difficulty!");
    }

    void EnterEnragedMode() {
        isEnraged = true;
        currentState = BossState.Enraged;
        
        chaseSpeed *= 1.3f;
        areaAttackCooldown = 2f; // Ataca de área muy frecuentemente
        
        Debug.Log("BOSS enters ENRAGED MODE! Health critical!");
    }

    void UpdateAnimation() {
        if (!animator) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(speedParamName, speed);
    }

    void OnDrawGizmosSelected() {
        if (!showDebugGizmos) return;
        if (transform == null) return;

        try {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Ranged attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, rangedAttackRange);

            // Area attack range
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, areaAttackRange);
            Gizmos.DrawWireSphere(transform.position, areaAttackRadius);

            // Preferred distance
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, preferredAttackDistance);
        } catch {
            // Ignorar errores en el dibujado de gizmos
        }
    }
}