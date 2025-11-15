using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Script para puertas que se desbloquean al recopilar objetos
/// Ahora integrado con el sistema de puntuación
/// </summary>
public class DoorUnlock : MonoBehaviour {
    [Header("Door Settings")]
    [Tooltip("Nombre de la escena a cargar al interactuar")]
    public string nextSceneName = "LevelTwo";
    [Tooltip("Distancia a la que el jugador puede interactuar")]
    public float interactionRange = 3f;
    [Tooltip("Mensaje que se muestra cuando falta desbloquear")]
    public string lockedMessage = "Necesitas recopilar todos los objetos para abrir esta puerta";
    [Tooltip("Mensaje que se muestra cuando está desbloqueada")]
    public string unlockedMessage = "Presiona E para continuar";
    [Tooltip("Es la puerta final del último nivel")]
    public bool isFinalDoor = false;

    [Header("Visual Feedback")]
    [Tooltip("Material cuando está bloqueada")]
    public Material lockedMaterial;
    [Tooltip("Material cuando está desbloqueada")]
    public Material unlockedMaterial;
    [Tooltip("Efecto de luz (opcional)")]
    public Light doorLight;

    [Header("Score Screen Settings")]
    [Tooltip("Tiempo de espera antes de mostrar la pantalla final (solo nivel 3)")]
    public float finalScreenDelay = 2f;

    private bool isUnlocked = false;
    private bool isPlayerNear = false;
    private bool hasInteracted = false;
    private GameObject player;
    private Renderer doorRenderer;

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player");
        doorRenderer = GetComponent<Renderer>();

        // Aplicar material bloqueado inicialmente
        if (doorRenderer && lockedMaterial) {
            doorRenderer.material = lockedMaterial;
        }

        // Apagar la luz si existe
        if (doorLight) {
            doorLight.enabled = false;
        }

        Debug.Log("[DoorUnlock] Puerta creada - Estado: BLOQUEADA");
    }

    void Update() {
        if (!player || hasInteracted) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        isPlayerNear = distance <= interactionRange;

        // Mostrar mensaje si está cerca
        if (isPlayerNear && isUnlocked) {
            if (Input.GetKeyDown(KeyCode.E)) {
                InteractWithDoor();
            }
        }
    }

    /// <summary>
    /// Desbloquea la puerta
    /// </summary>
    public void Unlock() {
        if (isUnlocked) return;

        isUnlocked = true;
        Debug.Log("[DoorUnlock] Puerta desbloqueada");

        // Cambiar material
        if (doorRenderer && unlockedMaterial) {
            doorRenderer.material = unlockedMaterial;
        }

        // Encender la luz
        if (doorLight) {
            doorLight.enabled = true;
            doorLight.color = Color.green;
        }

        // Reproducir animación de desbloqueo (opcional)
        PlayUnlockAnimation();
    }

    /// <summary>
    /// Interactuar con la puerta
    /// </summary>
    void InteractWithDoor() {
        if (!isUnlocked || hasInteracted) return;

        hasInteracted = true;

        Debug.Log("[DoorUnlock] Puerta abierta - Procesando...");

        // Si es la puerta final del último nivel
        if (isFinalDoor) {
            StartCoroutine(ShowFinalScoreSequence());
        } 
        // Si es cualquier otro nivel
        else {
            ShowLevelCompleteScreen();
        }
    }

    /// <summary>
    /// Muestra la pantalla de nivel completado
    /// </summary>
    void ShowLevelCompleteScreen() {
        LevelCompleteUI levelComplete = FindObjectOfType<LevelCompleteUI>();
        
        if (levelComplete) {
            levelComplete.ShowLevelComplete();
            Debug.Log("[DoorUnlock] Mostrando pantalla de nivel completado");
        } else {
            Debug.LogWarning("[DoorUnlock] No se encontró LevelCompleteUI, cargando escena directamente");
            LoadNextScene();
        }
    }

    /// <summary>
    /// Secuencia para el nivel final: primero pantalla de nivel, luego pantalla final
    /// </summary>
    IEnumerator ShowFinalScoreSequence() {
        // 1. Mostrar pantalla del nivel 3 completado
        LevelCompleteUI levelComplete = FindObjectOfType<LevelCompleteUI>();
        if (levelComplete) {
            levelComplete.ShowLevelComplete();
            Debug.Log("[DoorUnlock] Mostrando pantalla de nivel 3 completado");
        }

        // 2. Esperar unos segundos
        yield return new WaitForSecondsRealtime(finalScreenDelay);

        // 3. Ocultar pantalla de nivel
        if (levelComplete && levelComplete.completePanelPanel) {
            levelComplete.completePanelPanel.SetActive(false);
        }

        // 4. Mostrar pantalla final con todos los niveles
        FinalScoreUI finalScore = FindObjectOfType<FinalScoreUI>();
        if (finalScore) {
            finalScore.ShowFinalScore();
            Debug.Log("[DoorUnlock] Mostrando pantalla final del juego");
        } else {
            Debug.LogWarning("[DoorUnlock] No se encontró FinalScoreUI");
        }
    }

    /// <summary>
    /// Carga la siguiente escena (llamado desde los botones del UI)
    /// </summary>
    public void LoadNextScene() {
        Time.timeScale = 1f; // Despauser
        
        if (!string.IsNullOrEmpty(nextSceneName)) {
            Debug.Log("[DoorUnlock] Cargando escena: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        } else {
            Debug.LogError("[DoorUnlock] No se configuró el nombre de la siguiente escena");
        }
    }

    /// <summary>
    /// Animar el desbloqueo (opcional)
    /// </summary>
    void PlayUnlockAnimation() {
        // Aquí puedes añadir animaciones de apertura, sonidos, etc.
        Debug.Log("[DoorUnlock] Puerta animada");
    }

    void OnDrawGizmosSelected() {
        // Mostrar el rango de interacción en el editor
        Gizmos.color = isUnlocked ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Indicador visual si es puerta final
        if (isFinalDoor) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);
        }
    }

    void OnGUI() {
        if (!isPlayerNear || hasInteracted) return;

        int w = Screen.width;
        int h = Screen.height;
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(w / 2 - 200, h - 100, 400, 80);

        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 20;
        style.normal.textColor = isUnlocked ? Color.green : Color.red;

        string message = isUnlocked ? unlockedMessage : lockedMessage;
        GUI.Label(rect, message, style);
    }
}