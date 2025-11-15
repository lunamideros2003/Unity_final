using UnityEngine;

/// <summary>
/// Script para objetos coleccionables en el mapa con efectos visuales
/// </summary>
public class CollectibleItem : MonoBehaviour {
    [Header("Collectible Settings")]
    [Tooltip("Nombre del objeto (para debugging)")]
    public string itemName = "Collectible";
    [Tooltip("Distancia a la que el jugador puede recoger el objeto")]
    public float pickupRange = 5f;
    [Tooltip("Efecto visual al recoger (opcional)")]
    public GameObject pickupEffect;
    [Tooltip("Sonido al recoger (opcional)")]
    public AudioSource pickupSound;

    [Header("Visual Effects")]
    [Tooltip("Activar rotación automática")]
    public bool enableRotation = true;
    [Tooltip("Velocidad de rotación en el eje Y")]
    public float rotationSpeed = 50f;
    [Tooltip("Activar movimiento de flotación arriba/abajo")]
    public bool enableFloating = true;
    [Tooltip("Velocidad de flotación")]
    public float floatSpeed = 1f;
    [Tooltip("Altura máxima de flotación")]
    public float floatHeight = 0.3f;

    [Header("Glow Effect")]
    [Tooltip("Activar efecto de brillo pulsante")]
    public bool enableGlow = true;
    [Tooltip("Material del objeto (para cambiar el brillo)")]
    public Renderer objectRenderer;
    [Tooltip("Color de emisión base")]
    public Color glowColor = new Color(1f, 0.8f, 0.3f);
    [Tooltip("Intensidad mínima del brillo")]
    public float minGlowIntensity = 0.5f;
    [Tooltip("Intensidad máxima del brillo")]
    public float maxGlowIntensity = 2f;
    [Tooltip("Velocidad del pulso de brillo")]
    public float glowPulseSpeed = 2f;

    private bool isCollected = false;
    private GameObject player;
    private Vector3 startPosition;
    private float floatTimer = 0f;
    private float glowTimer = 0f;
    private Material glowMaterial;

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player");
        startPosition = transform.position;

        // Configurar material para el brillo
        if (enableGlow && objectRenderer != null) {
            // Crear una instancia del material para no afectar otros objetos
            glowMaterial = objectRenderer.material;
            
            // Habilitar emisión si el material lo soporta
            glowMaterial.EnableKeyword("_EMISSION");
        } else if (enableGlow && objectRenderer == null) {
            // Intentar obtener el Renderer automáticamente
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null) {
                glowMaterial = objectRenderer.material;
                glowMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    void Update() {
        if (isCollected) return;

        // Rotación continua
        if (enableRotation) {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        // Movimiento de flotación
        if (enableFloating) {
            floatTimer += Time.deltaTime * floatSpeed;
            float newY = startPosition.y + Mathf.Sin(floatTimer) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // Efecto de brillo pulsante
        if (enableGlow && glowMaterial != null) {
            glowTimer += Time.deltaTime * glowPulseSpeed;
            float glowIntensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, 
                                            (Mathf.Sin(glowTimer) + 1f) / 2f);
            
            Color emissionColor = glowColor * glowIntensity;
            glowMaterial.SetColor("_EmissionColor", emissionColor);
        }

        // Verificar si el jugador está cerca
        if (player) {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            
            if (distance <= pickupRange) {
                PickupItem();
            }
        }
    }

    void PickupItem() {
        if (isCollected) return;

        isCollected = true;

        // Añadir al inventario
        if (InventoryManager.Instance) {
            InventoryManager.Instance.AddItem(itemName);
            Debug.Log("[CollectibleItem] " + itemName + " recolectado. Total: " + InventoryManager.Instance.GetItemCount());
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
        
        // Mostrar la altura de flotación
        if (enableFloating) {
            Gizmos.color = Color.yellow;
            Vector3 pos = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawLine(pos + Vector3.up * floatHeight, pos - Vector3.up * floatHeight);
        }
    }

    void OnDestroy() {
        // Limpiar el material instanciado
        if (glowMaterial != null && Application.isPlaying) {
            Destroy(glowMaterial);
        }
    }
}