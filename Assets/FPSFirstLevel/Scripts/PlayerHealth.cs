using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour {
    [Header("Health Settings")]
    [Tooltip("Maximum health of the player")]
    public float maxHealth = 100f;
    [Tooltip("Current health of the player")]
    public float currentHealth;
    [Tooltip("Time before health regeneration starts")]
    public float regenDelay = 5f;
    [Tooltip("Health regenerated per second")]
    public float regenRate = 5f;
    [Tooltip("Enable health regeneration")]
    public bool enableRegen = true;

    [Header("UI References")]
    [Tooltip("Health bar image (fill type)")]
    public Image healthBar;
    [Tooltip("Damage overlay image")]
    public Image damageOverlay;
    [Tooltip("Text to display health numbers")]
    public Text healthText;

    [Header("Death Settings")]
    [Tooltip("Camera to use when dead (optional)")]
    public Camera deathCamera;
    [Tooltip("Respawn point transform")]
    public Transform respawnPoint;
    [Tooltip("Time to wait before showing death menu")]
    public float timeBeforeDeathMenu = 2f;
    [Tooltip("Auto respawn on death")]
    public bool autoRespawn = false; // Ahora mostramos menú en lugar de respawnear automáticamente

    [Header("Audio")]
    [Tooltip("Sound when taking damage")]
    public AudioSource damageSound;
    [Tooltip("Sound when dying")]
    public AudioSource deathSound;
    [Tooltip("Heartbeat sound when low health")]
    public AudioSource heartbeatSound;

    private bool isDead = false;
    private float lastDamageTime;
    private float overlayAlpha = 0f;
    private PlayerMovementScript movementScript;
    private MouseLookScript mouseLookScript;
    private GunInventory gunInventory;
    private GunScript currentGunScript;

    void Awake() {
        currentHealth = maxHealth;
        movementScript = GetComponent<PlayerMovementScript>();
        mouseLookScript = GetComponent<MouseLookScript>();
        gunInventory = GetComponent<GunInventory>();
        
        if (damageOverlay) {
            Color c = damageOverlay.color;
            c.a = 0f;
            damageOverlay.color = c;
        }
    }

    void Update() {
        if (isDead) return;

        // Health regeneration
        if (enableRegen && currentHealth < maxHealth && Time.time - lastDamageTime > regenDelay) {
            currentHealth += regenRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthUI();
        }

        // Damage overlay fade out
        if (overlayAlpha > 0) {
            overlayAlpha -= Time.deltaTime * 2f;
            if (damageOverlay) {
                Color c = damageOverlay.color;
                c.a = overlayAlpha;
                damageOverlay.color = c;
            }
        }

        // Heartbeat sound when low health
        if (heartbeatSound && currentHealth < maxHealth * 0.3f) {
            if (!heartbeatSound.isPlaying) {
                heartbeatSound.Play();
            }
        } else if (heartbeatSound && heartbeatSound.isPlaying) {
            heartbeatSound.Stop();
        }

        UpdateHealthUI();
    }

    public void TakeDamage(float damage) {
        if (isDead) {
            Debug.Log("Player is already dead, ignoring damage");
            return;
        }

        Debug.Log("Player taking " + damage + " damage. Current health: " + currentHealth);
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        lastDamageTime = Time.time;

        Debug.Log("Player health after damage: " + currentHealth);

        if (ScoreManager.Instance) {
            ScoreManager.Instance.SubtractDamageScore(damage);
        }

        // Visual feedback
        overlayAlpha = 0.5f;
        if (damageOverlay) {
            Color c = damageOverlay.color;
            c.a = overlayAlpha;
            damageOverlay.color = c;
        }

        // Audio feedback
        if (damageSound) {
            damageSound.Play();
        }

        UpdateHealthUI();

        if (currentHealth <= 0) {
            Die();
        }
    }

    public void Heal(float amount) {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    void UpdateHealthUI() {
        if (healthBar) {
            healthBar.fillAmount = currentHealth / maxHealth;
        }

        if (healthText) {
            healthText.text = Mathf.CeilToInt(currentHealth) + " / " + maxHealth;
        }
    }

    void Die() {
        if (isDead) return;
        
        isDead = true;

        Debug.Log("Player has died!");

        // Desbloquear el cursor para poder seleccionar botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Play death sound
        if (deathSound) {
            deathSound.Play();
        }

        // Stop heartbeat
        if (heartbeatSound && heartbeatSound.isPlaying) {
            heartbeatSound.Stop();
        }

        // Disable player controls
        if (movementScript) {
            movementScript.enabled = false;
        }
        if (mouseLookScript) {
            mouseLookScript.enabled = false;
        }

        // Destroy current weapon
        if (gunInventory && gunInventory.currentGun) {
            Destroy(gunInventory.currentGun);
            gunInventory.currentGun = null;
        }

        // Disable rigidbody physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Switch to death camera if exists
        if (deathCamera) {
            Camera mainCam = Camera.main;
            if (mainCam) {
                mainCam.enabled = false;
            }
            deathCamera.enabled = true;
        }

        // Mostrar menú de muerte después del tiempo especificado
        StartCoroutine(ShowDeathMenuCoroutine());
    }

    IEnumerator ShowDeathMenuCoroutine() {
        yield return new WaitForSeconds(timeBeforeDeathMenu);
        
        // Notificar al GameManager
        if (GameManager.Instance) {
            GameManager.Instance.OnPlayerDeath();
        }

        // Mostrar menú de muerte
        if (DeathMenuUI.Instance) {
            DeathMenuUI.Instance.ShowDeathMenu();
        }
    }

    public void Respawn() {
        isDead = false;
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Reset overlay
        overlayAlpha = 0f;
        if (damageOverlay) {
            Color c = damageOverlay.color;
            c.a = 0f;
            damageOverlay.color = c;
        }

        // Move to respawn point
        if (respawnPoint) {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        } else {
            transform.position = Vector3.zero;
        }

        // Re-enable controls
        if (movementScript) {
            movementScript.enabled = true;
        }
        if (mouseLookScript) {
            mouseLookScript.enabled = true;
        }

        // Re-enable physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
        }

        // Switch back to main camera
        if (deathCamera) {
            deathCamera.enabled = false;
            Camera mainCam = Camera.main;
            if (mainCam) {
                mainCam.enabled = true;
            }
        }

        Debug.Log("Player respawned!");
    }

    public bool IsDead() {
        return isDead;
    }

    public float GetHealthPercentage() {
        return currentHealth / maxHealth;
    }
}