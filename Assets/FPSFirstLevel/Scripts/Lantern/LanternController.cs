using UnityEngine;

/// <summary>
/// Controla el farol del jugador - iluminación y efectos
/// Este script va en el GameObject del jugador
/// </summary>
public class LanternController : MonoBehaviour {
    [Header("Lantern References")]
    [Tooltip("GameObject del farol (modelo 3D)")]
    public GameObject lanternModel;
    [Tooltip("Luz principal del farol")]
    public Light lanternLight;
    [Tooltip("Punto donde se coloca el farol (hijo del jugador, ej: en la cintura)")]
    public Transform lanternPosition;

    [Header("Light Settings")]
    [Tooltip("Intensidad de la luz")]
    public float lightIntensity = 3f;
    [Tooltip("Rango de iluminación")]
    public float lightRange = 15f;
    [Tooltip("Color de la luz")]
    public Color lightColor = new Color(1f, 0.7f, 0.3f); // Naranja cálido
    [Tooltip("Tipo de luz (Point recomendado)")]
    public LightType lightType = LightType.Point;

    [Header("Fog Piercing")]
    [Tooltip("Reducir niebla alrededor del jugador")]
    public bool reduceFogNearPlayer = true;
    [Tooltip("Radio donde se reduce la niebla")]
    public float fogClearRadius = 10f;

    [Header("Audio")]
    [Tooltip("Sonido ambiente del farol (fuego crepitando)")]
    public AudioSource lanternAmbientSound;

    private bool isActive = false;

    void Start() {
        // Inicializar desactivado
        if (lanternModel != null) {
            lanternModel.SetActive(false);
        }
        if (lanternLight != null) {
            lanternLight.enabled = false;
        }
        if (lanternAmbientSound != null) {
            lanternAmbientSound.enabled = false;
        }
    }

    /// <summary>
    /// Activa el farol cuando el jugador lo recoge
    /// </summary>
    public void ActivateLantern() {
        if (isActive) {
            Debug.Log("[LanternController] El farol ya está activo");
            return;
        }

        isActive = true;

        // Configurar el modelo del farol
        if (lanternModel != null) {
            lanternModel.SetActive(true);
            
            // Posicionar el farol si hay un punto específico
            if (lanternPosition != null) {
                lanternModel.transform.SetParent(lanternPosition);
                lanternModel.transform.localPosition = Vector3.zero;
                lanternModel.transform.localRotation = Quaternion.identity;
            }
        }

        // Configurar la luz
        if (lanternLight != null) {
            lanternLight.enabled = true;
            lanternLight.type = lightType;
            lanternLight.range = lightRange;
            lanternLight.intensity = lightIntensity;
            lanternLight.color = lightColor;
            
            // Sombras suaves para mejor rendimiento
            lanternLight.shadows = LightShadows.Soft;
            lanternLight.shadowStrength = 0.8f;
        } else {
            Debug.LogWarning("[LanternController] No se asignó una luz al farol. Creando una por defecto.");
            CreateDefaultLight();
        }

        // Activar sonido ambiente
        if (lanternAmbientSound != null) {
            lanternAmbientSound.enabled = true;
            lanternAmbientSound.loop = true;
            lanternAmbientSound.Play();
        }

        Debug.Log("[LanternController] ¡Farol activado! El camino se ilumina...");
    }

    /// <summary>
    /// Desactiva el farol (por si necesitas apagarlo temporalmente)
    /// </summary>
    public void DeactivateLantern() {
        isActive = false;

        if (lanternModel != null) {
            lanternModel.SetActive(false);
        }
        if (lanternLight != null) {
            lanternLight.enabled = false;
        }
        if (lanternAmbientSound != null) {
            lanternAmbientSound.Stop();
            lanternAmbientSound.enabled = false;
        }

        Debug.Log("[LanternController] Farol desactivado");
    }

    /// <summary>
    /// Crea una luz por defecto si no se asignó una
    /// </summary>
    void CreateDefaultLight() {
        GameObject lightObj = new GameObject("LanternLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, 1f, 0); // A la altura del pecho

        lanternLight = lightObj.AddComponent<Light>();
        lanternLight.type = lightType;
        lanternLight.range = lightRange;
        lanternLight.intensity = lightIntensity;
        lanternLight.color = lightColor;
        lanternLight.shadows = LightShadows.Soft;
        lanternLight.shadowStrength = 0.8f;
    }

    /// <summary>
    /// Verifica si el farol está activo
    /// </summary>
    public bool IsLanternActive() {
        return isActive;
    }

    /// <summary>
    /// Cambia la intensidad de la luz (útil para puzzles o efectos)
    /// </summary>
    public void SetLightIntensity(float intensity) {
        lightIntensity = intensity;
        if (lanternLight != null) {
            lanternLight.intensity = intensity;
        }
    }

    /// <summary>
    /// Cambia el rango de la luz
    /// </summary>
    public void SetLightRange(float range) {
        lightRange = range;
        if (lanternLight != null) {
            lanternLight.range = range;
        }
    }

    void OnDrawGizmos() {
        // Visualizar el rango de luz en el editor
        if (isActive && lanternLight != null) {
            Gizmos.color = new Color(1f, 0.7f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, lightRange);
        }
    }

    /// <summary>
    /// Reinicia el estado del farol (para reiniciar nivel)
    /// </summary>
    public void ResetLantern() {
        DeactivateLantern();
        Debug.Log("[LanternController] Farol reiniciado");
    }

    void Update() {
    if (!isActive || lanternLight == null) return;
    
    // Forzar valores constantes cada frame
    lanternLight.range = lightRange;
    lanternLight.intensity = lightIntensity;
    
    Debug.Log("Range: " + lanternLight.range + " | Intensity: " + lanternLight.intensity);
}
}