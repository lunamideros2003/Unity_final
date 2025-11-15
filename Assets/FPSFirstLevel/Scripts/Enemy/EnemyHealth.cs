using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Collections;

public class EnemyHealth : MonoBehaviour {
    [Header("Health Settings")]
    [Tooltip("Maximum health of enemy")]
    public float maxHealth = 50f;
    [Tooltip("Current health")]
    public float currentHealth;

    [Header("Health Bar UI (World Space)")]
    [Tooltip("Canvas with health bar (optional)")]
    public Canvas healthBarCanvas;
    [Tooltip("Health bar image")]
    public Image healthBarImage;
    [Tooltip("Hide health bar when full")]
    public bool hideWhenFull = true;
    [Tooltip("Time to hide health bar after full")]
    public float hideDelay = 2f;

    [Header("Death Settings")]
    [Tooltip("Time before destroying the body")]
    public float destroyDelay = 5f;
    [Tooltip("Use ragdoll on death")]
    public bool useRagdoll = false;
    [Tooltip("Ragdoll root (optional)")]
    public GameObject ragdollRoot;
    [Tooltip("Delay before enabling ragdoll (let animation play first)")]
    public float ragdollDelay = 1f;
    [Tooltip("Drops on death")]
    public GameObject[] dropItems;
    [Tooltip("Drop chance (0-1)")]
    public float dropChance = 0.5f;

    [Header("Audio")]
    [Tooltip("Sound when hit")]
    public AudioSource hitSound;
    [Tooltip("Sound when dying")]
    public AudioSource deathSound;

    private bool isDead = false;
    private EnemyAI enemyAI;
    private Animator animator;
    private NavMeshAgent agent;
    private float lastHitTime;

    void Awake() {
        currentHealth = maxHealth;
        enemyAI = GetComponent<EnemyAI>();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (healthBarCanvas && hideWhenFull) {
            healthBarCanvas.enabled = false;
        }
        
        Debug.Log("[EnemyHealth] " + gameObject.name + " inicializado con " + maxHealth + " de vida");
    }
    
    void Start() {
        // Verificar que la vida esté correctamente inicializada
        if (currentHealth <= 0) {
            currentHealth = maxHealth;
            Debug.LogWarning("[EnemyHealth] " + gameObject.name + " tenía vida en 0, reiniciada a " + maxHealth);
        }
    }

    void Update() {
        // Hide health bar after delay when full
        if (healthBarCanvas && hideWhenFull && currentHealth >= maxHealth) {
            if (Time.time - lastHitTime > hideDelay) {
                healthBarCanvas.enabled = false;
            }
        }

        // Make health bar face camera
        if (healthBarCanvas && healthBarCanvas.enabled) {
            Camera mainCam = Camera.main;
            if (mainCam) {
                healthBarCanvas.transform.LookAt(mainCam.transform);
                healthBarCanvas.transform.Rotate(0, 180, 0);
            }
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection) {
        if (isDead) {
            Debug.Log(gameObject.name + " is already dead, ignoring damage");
            return;
        }

        Debug.Log(gameObject.name + " taking " + damage + " damage. Current health: " + currentHealth);

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        lastHitTime = Time.time;

        Debug.Log(gameObject.name + " health after damage: " + currentHealth);

        // Show health bar
        if (healthBarCanvas) {
            healthBarCanvas.enabled = true;
        }

        // Update health bar
        if (healthBarImage) {
            healthBarImage.fillAmount = currentHealth / maxHealth;
        }

        // Play hit sound
        if (hitSound) {
            hitSound.Play();
        }

        // Hit reaction animation
        if (animator && currentHealth > 0) {
            animator.SetTrigger("Hit");
        }

        // Alert AI
        if (enemyAI && !enemyAI.isAggro) {
            enemyAI.AlertEnemy(hitPoint);
        }

        if (currentHealth <= 0) {
            Die(hitDirection);
        }
    }

    void Die(Vector3 deathDirection) {
        if (isDead) return;

        isDead = true;

        Debug.Log(gameObject.name + " has died!");

        // 🎯 PUNTUACIÓN: Bonus por matar enemigo
        if (ScoreManager.Instance) {
            ScoreManager.Instance.AddEnemyKillBonus(100);
        }

        // Play death sound
        if (deathSound) {
            deathSound.Play();
        }

        // Disable AI
        if (enemyAI) {
            enemyAI.enabled = false;
        }

        // Desactivar RangedEnemyAI si existe
        RangedEnemyAI rangedAI = GetComponent<RangedEnemyAI>();
        if (rangedAI) {
            rangedAI.enabled = false;
        }

        // Stop NavMeshAgent
        if (agent) {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Play death animation
        if (animator) {
            animator.SetTrigger("Death");
            
            // Evitar que otras animaciones interrumpan la muerte
            animator.SetFloat("Speed", 0);
        }

        // Disable main collider (keep ragdoll colliders if using ragdoll)
        if (!useRagdoll) {
            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider col in colliders) {
                col.enabled = false;
            }
        }

        // Enable ragdoll after animation plays a bit
        if (useRagdoll && ragdollRoot) {
            StartCoroutine(EnableRagdollAfterDelay(ragdollDelay, deathDirection));
        }

        // Drop items
        DropLoot();

        // Hide health bar
        if (healthBarCanvas) {
            healthBarCanvas.enabled = false;
        }

        // Destroy after delay
        Destroy(gameObject, destroyDelay);
    }

    IEnumerator EnableRagdollAfterDelay(float delay, Vector3 deathDirection) {
        yield return new WaitForSeconds(delay);
        
        EnableRagdoll();
        ApplyDeathForce(deathDirection);
    }

    void EnableRagdoll() {
        if (!ragdollRoot) return;

        // Disable animator
        if (animator) {
            animator.enabled = false;
        }

        // Enable ragdoll rigidbodies
        Rigidbody[] rigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies) {
            rb.isKinematic = false;
        }

        // Enable ragdoll colliders
        Collider[] colliders = ragdollRoot.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) {
            col.enabled = true;
        }
        
        Debug.Log(gameObject.name + " ragdoll enabled");
    }

    void ApplyDeathForce(Vector3 direction) {
        if (!ragdollRoot) return;

        Rigidbody[] rigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies) {
            rb.AddForce(direction * 300f);
        }
    }

    void DropLoot() {
        if (dropItems.Length == 0) return;

        if (Random.value > dropChance) return;

        GameObject itemToDrop = dropItems[Random.Range(0, dropItems.Length)];
        if (itemToDrop) {
            Vector3 dropPos = transform.position + Vector3.up;
            Instantiate(itemToDrop, dropPos, Quaternion.identity);
        }
    }

    public bool IsDead() {
        return isDead;
    }

    public float GetHealthPercentage() {
        return currentHealth / maxHealth;
    }
}