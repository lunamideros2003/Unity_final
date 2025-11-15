using UnityEngine;

/// <summary>
/// Gestiona la atmósfera oscura del nivel y la interacción con el farol
/// Este script va en un GameObject vacío en la escena
/// </summary>
public class DarknessManager : MonoBehaviour {
    [Header("Darkness Settings")]
    [Tooltip("Color de la niebla sin el farol")]
    public Color darkFogColor = new Color(0.05f, 0.05f, 0.1f);
    [Tooltip("Densidad de la niebla")]
    [Range(0f, 1f)]
    public float fogDensity = 0.08f;
    [Tooltip("Color de ambiente sin farol")]
    public Color darkAmbientLight = new Color(0.1f, 0.1f, 0.15f);
    
    [Header("With Lantern Settings")]
    [Tooltip("Color de la niebla con el farol (más cálido)")]
    public Color litFogColor = new Color(0.1f, 0.1f, 0.15f);
    [Tooltip("Densidad reducida con farol")]
    [Range(0f, 1f)]
    public float litFogDensity = 0.05f;
    
    [Header("Transition")]
    [Tooltip("Velocidad de transición entre oscuridad y luz")]
    public float transitionSpeed = 0.5f;
    
    [Header("Vignette Effect (Post-Processing)")]
    [Tooltip("Activar efecto de viñeta sin el farol")]
    public bool enableVignetteWithoutLantern = true;
    [Tooltip("Intensidad de la viñeta")]
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.5f;

    [Header("References")]
    [Tooltip("Referencia al jugador")]
    public GameObject player;

    private LanternController lanternController;
    private bool hasLantern = false;
    private float currentTransition = 0f; // 0 = oscuro, 1 = con farol

    void Start() {
        // Buscar al jugador si no está asignado
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        // Obtener el controlador del farol
        if (player != null) {
            lanternController = player.GetComponent<LanternController>();
        }

        // Configurar ambiente inicial (oscuro)
        ApplyDarknessSettings();
    }

    void Update() {
        // Verificar si el jugador tiene el farol
        if (lanternController != null) {
            bool lanternActive = lanternController.IsLanternActive();
            
            if (lanternActive != hasLantern) {
                hasLantern = lanternActive;
                Debug.Log("[DarknessManager] Estado del farol cambió: " + hasLantern);
            }
        }

        // Transición suave entre estados
        float targetTransition = hasLantern ? 1f : 0f;
        currentTransition = Mathf.Lerp(currentTransition, targetTransition, Time.deltaTime * transitionSpeed);

        // Aplicar configuración interpolada
        ApplyInterpolatedSettings();
    }

    /// <summary>
    /// Aplica la configuración inicial de oscuridad
    /// </summary>
    void ApplyDarknessSettings() {
        RenderSettings.fog = true;
        RenderSettings.fogColor = darkFogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.ambientLight = darkAmbientLight;
        RenderSettings.ambientIntensity = 0.3f;

        Debug.Log("[DarknessManager] Configuración de oscuridad aplicada");
    }

    /// <summary>
    /// Aplica configuración interpolada según si tiene o no el farol
    /// </summary>
    void ApplyInterpolatedSettings() {
        // Interpolar color de niebla
        RenderSettings.fogColor = Color.Lerp(darkFogColor, litFogColor, currentTransition);
        
        // Interpolar densidad de niebla
        RenderSettings.fogDensity = Mathf.Lerp(fogDensity, litFogDensity, currentTransition);
    }

    /// <summary>
    /// Fuerza actualización del ambiente (útil para eventos)
    /// </summary>
    public void ForceUpdateEnvironment() {
        if (lanternController != null) {
            hasLantern = lanternController.IsLanternActive();
        }
    }

    /// <summary>
    /// Activa o desactiva temporalmente la oscuridad (para cinemáticas)
    /// </summary>
    public void SetDarknessEnabled(bool enabled) {
        RenderSettings.fog = enabled;
    }

    /// <summary>
    /// Cambia la intensidad de la oscuridad dinámicamente
    /// </summary>
    public void SetDarknessIntensity(float intensity) {
        intensity = Mathf.Clamp01(intensity);
        fogDensity = 0.02f + (0.1f * intensity);
        RenderSettings.fogDensity = fogDensity;
    }

    void OnDrawGizmos() {
        // Indicador visual en el editor
        Gizmos.color = hasLantern ? Color.yellow : Color.black;
        if (player != null) {
            Gizmos.DrawWireSphere(player.transform.position, 5f);
        }
    }
}