using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Enemigo que solo se mueve cuando el jugador NO lo está mirando
/// Inmortal pero limitado a un área específica
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class StalkerEnemy : MonoBehaviour {
    [Header("Target")]
    [Tooltip("The player transform (auto-finds if empty)")]
    public Transform player;
    [Tooltip("La cámara del jugador")]
    public Camera playerCamera;

    [Header("Detection Settings")]
    [Tooltip("¿El jugador me está mirando?")]
    public bool isBeingWatched = false;
    [Tooltip("Distancia máxima para detectar la mirada")]
    public float maxDetectionDistance = 50f;
    [Tooltip("Ángulo del campo de visión del jugador (90-110 típico)")]
    public float playerFieldOfView = 90f;
    [Tooltip("Margen de error (más alto = más fácil escapar)")]
    public float detectionMargin = 5f;
    [Tooltip("Usar raycast para verificar obstáculos")]
    public bool useLineOfSight = true;
    [Tooltip("Capas que bloquean la visión")]
    public LayerMask obstacleMask;

    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento hacia el jugador")]
    public float moveSpeed = 3.5f;
    [Tooltip("Distancia de ataque (cuán cerca puede llegar)")]
    public float attackDistance = 2f;
    [Tooltip("Tiempo que espera después de ser visto antes de moverse")]
    public float moveDelay = 0.3f;

    [Header("Boundary Settings")]
    [Tooltip("Centro del área permitida")]
    public Transform boundaryCenter;
    [Tooltip("Radio del área permitida")]
    public float boundaryRadius = 20f;
    [Tooltip("Forma del límite")]
    public BoundaryShape boundaryShape = BoundaryShape.Circle;
    [Tooltip("Tamaño del cuadrado (si es cuadrado)")]
    public Vector3 boundaryBoxSize = new Vector3(20f, 10f, 20f);

    [Header("Attack Settings")]
    [Tooltip("Daño al jugador al alcanzarlo")]
    public float attackDamage = 50f;
    [Tooltip("Tiempo entre ataques")]
    public float attackCooldown = 1f;
    [Tooltip("Empujar al jugador después de atacar")]
    public bool pushPlayerBack = true;
    [Tooltip("Fuerza del empuje")]
    public float pushForce = 10f;

    [Header("Animation")]
    [Tooltip("Animator component")]
    public Animator animator;
    [Tooltip("Nombre del parámetro de velocidad")]
    public string speedParamName = "Speed";

    [Header("Audio")]
    [Tooltip("Sonido cuando se mueve")]
    public AudioSource moveSound;
    [Tooltip("Sonido cuando ataca")]
    public AudioSource attackSound;
    [Tooltip("Sonido cuando es visto")]
    public AudioSource detectedSound;

    [Header("Visual Effects")]
    [Tooltip("Material cuando está siendo observado")]
    public Material frozenMaterial;
    [Tooltip("Material cuando se puede mover")]
    public Material normalMaterial;
    [Tooltip("Renderer del enemigo")]
    public Renderer enemyRenderer;

    [Header("Debug")]
    public bool showDebugGizmos = true;
    public bool showDebugLogs = false;

    private NavMeshAgent agent;
    private float lastMoveTime;
    private float lastAttackTime;
    private bool wasBeingWatched = false;
    private Vector3 startPosition;
    private PlayerHealth playerHealth;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        
        // Buscar jugador si no está asignado
        if (player == null) {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) {
                player = playerObj.transform;
                playerHealth = player.GetComponent<PlayerHealth>();
                
                if (playerHealth == null) {
                    Debug.LogError("[StalkerEnemy] PlayerHealth no encontrado en el jugador!");
                } else {
                    Debug.Log("[StalkerEnemy] PlayerHealth encontrado correctamente");
                }
            } else {
                Debug.LogError("[StalkerEnemy] No se encontró GameObject con tag 'Player'");
            }
        } else {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null) {
                Debug.LogError("[StalkerEnemy] PlayerHealth no encontrado en el jugador asignado!");
            }
        }

        // Buscar cámara del jugador
        if (playerCamera == null) {
            playerCamera = Camera.main;
            if (playerCamera == null) {
                Debug.LogError("[StalkerEnemy] No se encontró Camera.main!");
            }
        }

        // Posición inicial
        startPosition = transform.position;

        // Centro del límite
        if (boundaryCenter == null) {
            boundaryCenter = transform;
        }

        // Configurar agente
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackDistance;
        agent.autoBraking = true;
        
        Debug.Log("[StalkerEnemy] Inicializado - Attack Distance: " + attackDistance);
    }

    void Update() {
        if (player == null || playerCamera == null) return;

        // Verificar si el jugador me está mirando
        CheckIfBeingWatched();

        // Comportamiento según si estoy siendo observado
        if (isBeingWatched) {
            HandleBeingWatched();
        } else {
            HandleNotWatched();
        }

        // Verificar límites del área
        EnforceBoundary();

        // Actualizar animaciones
        UpdateAnimation();

        // Atacar si está cerca
        TryAttack();
    }

    /// <summary>
    /// Verifica si el jugador está mirando al enemigo
    /// </summary>
    void CheckIfBeingWatched() {
        Vector3 directionToEnemy = (transform.position - playerCamera.transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, playerCamera.transform.position);

        // Demasiado lejos
        if (distanceToPlayer > maxDetectionDistance) {
            isBeingWatched = false;
            return;
        }

        // Calcular ángulo entre la cámara y el enemigo
        float angleToEnemy = Vector3.Angle(playerCamera.transform.forward, directionToEnemy);

        // Fuera del campo de visión
        if (angleToEnemy > (playerFieldOfView / 2f) + detectionMargin) {
            isBeingWatched = false;
            return;
        }

        // Verificar obstáculos
        if (useLineOfSight) {
            RaycastHit hit;
            Vector3 rayStart = playerCamera.transform.position;
            
            if (Physics.Raycast(rayStart, directionToEnemy, out hit, distanceToPlayer, ~obstacleMask)) {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) {
                    isBeingWatched = true;
                    return;
                }
            }
            
            isBeingWatched = false;
            return;
        }

        // Sin obstáculos, está siendo observado
        isBeingWatched = true;
    }

    /// <summary>
    /// Comportamiento cuando está siendo observado
    /// </summary>
    void HandleBeingWatched() {
        // Detener movimiento
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Cambiar material si está configurado
        if (enemyRenderer && frozenMaterial) {
            enemyRenderer.material = frozenMaterial;
        }

        // Sonido de detección (solo una vez)
        if (!wasBeingWatched) {
            if (detectedSound && !detectedSound.isPlaying) {
                detectedSound.Play();
            }
            
            if (showDebugLogs) {
                Debug.Log("[StalkerEnemy] ¡El jugador me está mirando!");
            }
        }

        wasBeingWatched = true;

        // Detener sonido de movimiento
        if (moveSound && moveSound.isPlaying) {
            moveSound.Stop();
        }
    }

    /// <summary>
    /// Comportamiento cuando NO está siendo observado
    /// </summary>
    void HandleNotWatched() {
        // Esperar un poco después de ser visto
        if (Time.time - lastMoveTime < moveDelay && wasBeingWatched) {
            return;
        }

        wasBeingWatched = false;
        lastMoveTime = Time.time;

        // Cambiar material
        if (enemyRenderer && normalMaterial) {
            enemyRenderer.material = normalMaterial;
        }

        // SIEMPRE persigue al jugador, sin importar la distancia
        agent.isStopped = false;
        agent.SetDestination(player.position);
        
        // Sonido de movimiento
        if (moveSound && !moveSound.isPlaying) {
            moveSound.Play();
        }

        if (showDebugLogs && Time.frameCount % 60 == 0) {
            Debug.Log("[StalkerEnemy] Moviéndose hacia el jugador...");
        }
    }

    /// <summary>
    /// Mantiene al enemigo dentro del área permitida
    /// </summary>
    void EnforceBoundary() {
        Vector3 centerPos = boundaryCenter.position;
        Vector3 currentPos = transform.position;

        bool isOutOfBounds = false;
        Vector3 correctedPos = currentPos;

        if (boundaryShape == BoundaryShape.Circle) {
            // Límite circular
            float distanceFromCenter = Vector3.Distance(new Vector3(currentPos.x, centerPos.y, currentPos.z), 
                                                        new Vector3(centerPos.x, centerPos.y, centerPos.z));
            
            if (distanceFromCenter > boundaryRadius) {
                isOutOfBounds = true;
                Vector3 direction = (currentPos - centerPos).normalized;
                correctedPos = centerPos + direction * boundaryRadius;
                correctedPos.y = currentPos.y; // Mantener altura
            }
        } else {
            // Límite cuadrado/rectangular
            Vector3 localPos = currentPos - centerPos;
            Vector3 halfSize = boundaryBoxSize / 2f;

            if (Mathf.Abs(localPos.x) > halfSize.x || 
                Mathf.Abs(localPos.z) > halfSize.z) {
                isOutOfBounds = true;
                
                correctedPos.x = centerPos.x + Mathf.Clamp(localPos.x, -halfSize.x, halfSize.x);
                correctedPos.z = centerPos.z + Mathf.Clamp(localPos.z, -halfSize.z, halfSize.z);
            }
        }

        if (isOutOfBounds) {
            transform.position = correctedPos;
            agent.ResetPath();
            
            if (showDebugLogs) {
                Debug.Log("[StalkerEnemy] Fuera de límites, reposicionando...");
            }
        }
    }

    /// <summary>
    /// Intenta atacar al jugador si está cerca
    /// </summary>
    void TryAttack() {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackDistance && Time.time - lastAttackTime > attackCooldown) {
            if (showDebugLogs) {
                Debug.Log("[StalkerEnemy] Distancia al jugador: " + distanceToPlayer + " <= " + attackDistance + " - ¡ATACANDO!");
            }
            Attack();
        } else if (showDebugLogs && Time.frameCount % 60 == 0) {
            Debug.Log("[StalkerEnemy] Distancia al jugador: " + distanceToPlayer + " (necesita <= " + attackDistance + ")");
        }
    }

    /// <summary>
    /// Ataca al jugador
    /// </summary>
    void Attack() {
        lastAttackTime = Time.time;

        // Sonido de ataque
        if (attackSound) {
            attackSound.Play();
        }

        // Daño al jugador
        if (playerHealth) {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log("[StalkerEnemy] ¡Atacó al jugador! Daño: " + attackDamage);
        } else {
            Debug.LogWarning("[StalkerEnemy] No se pudo hacer daño - PlayerHealth no encontrado");
        }

        // Empujar al jugador
        if (pushPlayerBack && player) {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb) {
                Vector3 pushDirection = (player.position - transform.position).normalized;
                playerRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// Actualiza las animaciones
    /// </summary>
    void UpdateAnimation() {
        if (animator == null) return;

        // Velocidad: 0 si está siendo observado, normal si no
        float speed = isBeingWatched ? 0f : agent.velocity.magnitude;
        animator.SetFloat(speedParamName, speed);
    }

    /// <summary>
    /// Este enemigo no puede morir, pero puedes llamar esto para resetearlo
    /// </summary>
    public void ResetToStart() {
        transform.position = startPosition;
        agent.ResetPath();
        lastAttackTime = 0;
        
        Debug.Log("[StalkerEnemy] Reseteado a posición inicial");
    }

    /// <summary>
    /// Teletransporta al enemigo a una posición específica
    /// </summary>
    public void TeleportTo(Vector3 position) {
        if (agent.enabled) {
            agent.Warp(position);
        } else {
            transform.position = position;
        }
    }

    void OnDrawGizmos() {
        if (!showDebugGizmos) return;

        // Dibujar área de límite
        Vector3 center = boundaryCenter ? boundaryCenter.position : transform.position;
        
        if (boundaryShape == BoundaryShape.Circle) {
            Gizmos.color = Color.red;
            DrawCircle(center, boundaryRadius, 32);
        } else {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, boundaryBoxSize);
        }

        // Distancia de ataque
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Línea hacia el jugador si está en runtime
        if (Application.isPlaying && player) {
            Gizmos.color = isBeingWatched ? Color.green : Color.cyan;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    void DrawCircle(Vector3 center, float radius, int segments) {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++) {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}

public enum BoundaryShape {
    Circle,
    Square
}