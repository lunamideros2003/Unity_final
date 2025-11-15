using UnityEngine;

/// <summary>
/// Script que hace que un enemigo suelte un objeto al morir
/// Añade este script al mismo GameObject que tiene EnemyHealth
/// </summary>
public class EnemyLootDrop : MonoBehaviour {
    [Header("Loot Settings")]
    [Tooltip("Prefab del objeto a soltar")]
    public GameObject lootPrefab;
    [Tooltip("Probabilidad de soltar el objeto (0-1)")]
    [Range(0f, 1f)]
    public float dropChance = 1f;
    [Tooltip("Altura a la que cae el objeto")]
    public float dropHeight = 1f;
    [Tooltip("Fuerza dispersión aleatoria")]
    public float spreadForce = 5f;
    [Tooltip("Sonido al soltar el objeto (opcional)")]
    public AudioSource dropSound;

    private EnemyHealth enemyHealth;
    private bool hasDropped = false;

    void Start() {
        enemyHealth = GetComponent<EnemyHealth>();

        if (!enemyHealth) {
            Debug.LogWarning("[EnemyLootDrop] No se encontró EnemyHealth en " + gameObject.name);
        }

        if (!lootPrefab) {
            Debug.LogWarning("[EnemyLootDrop] Loot Prefab no asignado en " + gameObject.name);
        }
    }

    void Update() {
        // Verificar si el enemigo murió
        if (enemyHealth && enemyHealth.IsDead() && !hasDropped) {
            DropLoot();
        }
    }

    void DropLoot() {
        hasDropped = true;

        // Verificar probabilidad
        if (Random.value > dropChance) {
            Debug.Log("[EnemyLootDrop] " + gameObject.name + " no soltó loot (probabilidad fallida)");
            return;
        }

        if (!lootPrefab) {
            Debug.LogWarning("[EnemyLootDrop] No hay loot prefab asignado");
            return;
        }

        // Calcular posición de caída
        Vector3 dropPosition = transform.position + Vector3.up * dropHeight;

        // Instanciar el loot
        GameObject loot = Instantiate(lootPrefab, dropPosition, Quaternion.identity);

        // Añadir dispersión aleatoria
        Rigidbody lootRb = loot.GetComponent<Rigidbody>();
        if (lootRb) {
            Vector3 randomDirection = Random.insideUnitSphere * spreadForce;
            randomDirection.y = 0; // Solo dispersión horizontal
            lootRb.velocity = randomDirection;
        }

        // Reproducir sonido
        if (dropSound) {
            AudioSource audioSource = Instantiate(dropSound, dropPosition, Quaternion.identity);
            Destroy(audioSource.gameObject, dropSound.clip.length);
        }

        Debug.Log("[EnemyLootDrop] " + gameObject.name + " soltó: " + lootPrefab.name);
    }

    /// <summary>
    /// Método público para soltar loot manualmente (opcional)
    /// </summary>
    public void ForceDropLoot() {
        if (!hasDropped) {
            DropLoot();
        }
    }
}