using UnityEngine;

/// <summary>
/// Script para que el jugador recoja loot automáticamente
/// Añade este script al prefab de loot
/// </summary>
public class LootPickup : MonoBehaviour {
    [Header("Pickup Settings")]
    [Tooltip("Distancia a la que se recoge el loot")]
    public float pickupRange = 3f;
    [Tooltip("Velocidad de movimiento hacia el jugador")]
    public float pickupSpeed = 10f;
    [Tooltip("Sonido al recoger (opcional)")]
    public AudioSource pickupSound;

    [Header("Visual Feedback")]
    [Tooltip("Hacer rotar el objeto")]
    public bool rotate = true;
    [Tooltip("Velocidad de rotación")]
    public float rotationSpeed = 200f;

    private GameObject player;
    private Rigidbody rb;
    private bool isBeingPickedUp = false;

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = GetComponent<Rigidbody>();

        if (!player) {
            Debug.LogWarning("[LootPickup] Jugador no encontrado");
        }
    }

    void Update() {
        if (!player) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Si el jugador está cerca, mover el loot hacia él
        if (distanceToPlayer <= pickupRange && !isBeingPickedUp) {
            PickupLoot();
        }

        // Rotación visual
        if (rotate) {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }

    void PickupLoot() {
        isBeingPickedUp = true;

        // Reproducir sonido
        if (pickupSound) {
            pickupSound.Play();
        }

        // Mover hacia el jugador
        StartCoroutine(MoveToPlayer());
    }

    System.Collections.IEnumerator MoveToPlayer() {
        while (Vector3.Distance(transform.position, player.transform.position) > 0.5f) {
            transform.position = Vector3.Lerp(
                transform.position,
                player.transform.position,
                pickupSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Destruir el loot cuando llega al jugador
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected() {
        // Mostrar rango de pickup
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}