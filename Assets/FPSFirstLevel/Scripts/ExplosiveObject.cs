using UnityEngine;

/// <summary>
/// Gestiona un objeto que puede recibir daño y explotar,
/// causando daño en área a los enemigos.
/// </summary>
public class ExplosiveObject : MonoBehaviour {

    [Header("Health")]
    [Tooltip("Vida del objeto antes de explotar")]
    public float health = 50f;

    [Header("Explosion Settings")]
    [Tooltip("El daño que causa la explosión")]
    public float explosionDamage = 100f;
    [Tooltip("El radio de la explosión")]
    public float explosionRadius = 5f;
    [Tooltip("Fuerza de la explosión (para empujar ragdolls)")]
    public float explosionForce = 700f;

    [Header("Effects")]
    [Tooltip("Prefab del sistema de partículas de la explosión")]
    public GameObject explosionVFX;
    [Tooltip("Sonido de la explosión")]
    public AudioClip explosionSFX;
    
    [Header("Targeting")]
    [Tooltip("Qué capas deben ser afectadas por la explosión (IMPORTANTE: ¡Configura esto!")]
    public LayerMask enemyLayerMask;

    private bool isExploded = false;
    private AudioSource audioSource;

    void Awake() {
        // Asegurarnos de que tenga un AudioSource para el sonido
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Método público para que otros scripts (como el del jugador) le hagan daño.
    /// </summary>
    public void TakeDamage(float amount) {
        if (isExploded) return; // Ya explotó, no hacer nada

        health -= amount;
        
        if (health <= 0) {
            Explode();
        }
    }

    /// <summary>
    /// Realiza la explosión
    /// </summary>
    void Explode() {
        if (isExploded) return;
        isExploded = true;

        Debug.Log(gameObject.name + " ha explotado!");

        // 1. Encontrar todos los enemigos en el radio
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayerMask);

        foreach (Collider hit in colliders) {
            // 2. Intentar obtener el script EnemyHealth de lo que golpeamos
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            
            if (enemy != null && !enemy.IsDead()) {
                // ¡Encontramos un enemigo!
                Debug.Log("Explosión golpeó a " + enemy.name);
                
                // Calcular dirección para el ragdoll (desde el centro de la explosión)
                Vector3 hitDirection = (enemy.transform.position - transform.position).normalized;
                
                // 3. Aplicar daño al enemigo
                enemy.TakeDamage(explosionDamage, enemy.transform.position, hitDirection * explosionForce);
            }

            // Opcional: Aplicar fuerza física a cualquier Rigidbody (útil para ragdolls)
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        // 4. Efectos visuales y de sonido
        if (explosionVFX != null) {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        // --- CAMBIO AQUÍ ---
        if (explosionSFX != null && audioSource != null) {
            // Asigna y reproduce el clip en el AudioSource del objeto.
            audioSource.clip = explosionSFX;
            audioSource.Play();
            
            // Destruye el gameObject *después* de la duración del clip para que el sonido termine de reproducirse.
            // Si la explosión ocurre, el barril explota y el sonido suena.
            Destroy(gameObject, explosionSFX.length);
        } else {
            // Si no hay sonido, o el AudioSource no se pudo obtener, simplemente destruye el objeto inmediatamente.
            Destroy(gameObject);
        }
    }

    // Dibuja un gizmo en el editor para ver el radio de la explosión
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}