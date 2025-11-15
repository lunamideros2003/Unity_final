using UnityEngine;

/// <summary>
/// Script para el farol coleccionable que ilumina los alrededores
/// </summary>
public class LanternItem : MonoBehaviour {
    [Header("Lantern Collectible Settings")]
    [Tooltip("Nombre del objeto")]
    public string itemName = "Farol";
    [Tooltip("Distancia a la que el jugador puede recoger el farol")]
    public float pickupRange = 3f;
    [Tooltip("Efecto visual al recoger (opcional)")]
    public GameObject pickupEffect;
    [Tooltip("Sonido al recoger (opcional)")]
    public AudioSource pickupSound;

    [Header("Visual Effects")]
    [Tooltip("Luz del farol en el suelo (se apaga al recogerlo)")]
    public Light lanternGroundLight;
    [Tooltip("Rotación suave del farol")]
    public bool rotateInPlace = true;
    [Tooltip("Velocidad de rotación")]
    public float rotationSpeed = 30f;

    private bool isCollected = false;
    private GameObject player;

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Asegurar que la luz del suelo esté encendida
        if (lanternGroundLight != null) {
            lanternGroundLight.enabled = true;
        }
    }

    void Update() {
        if (isCollected) return;

        // Rotación decorativa
        if (rotateInPlace) {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        // Verificar si el jugador está cerca
        if (player) {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            
            if (distance <= pickupRange) {
                PickupLantern();
            }
        }
    }

    void PickupLantern() {
        if (isCollected) return;

        isCollected = true;

        // Activar el sistema de farol del jugador
        LanternController lanternController = player.GetComponent<LanternController>();
        if (lanternController != null) {
            lanternController.ActivateLantern();
            Debug.Log("[LanternItem] Farol activado en el jugador");
        } else {
            Debug.LogWarning("[LanternItem] No se encontró LanternController en el jugador");
        }

        // Añadir al inventario (opcional, para tracking)
        if (InventoryManager.Instance) {
            InventoryManager.Instance.AddItem(itemName);
        }

        // Apagar la luz del suelo
        if (lanternGroundLight != null) {
            lanternGroundLight.enabled = false;
        }

        // Crear efecto visual
        if (pickupEffect) {
            Destroy(Instantiate(pickupEffect, transform.position, Quaternion.identity), 2f);
        }

        // Reproducir sonido
        if (pickupSound) {
            pickupSound.Play();
            Destroy(gameObject, pickupSound.clip.length);
        } else {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected() {
        // Mostrar el rango de pickup en el editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}