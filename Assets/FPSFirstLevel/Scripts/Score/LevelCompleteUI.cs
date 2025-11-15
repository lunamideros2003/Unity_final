using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pantalla que aparece al completar un nivel
/// Muestra la puntuación del nivel y permite continuar
/// </summary>
public class LevelCompleteUI : MonoBehaviour {
    [Header("UI References")]
    [Tooltip("Panel principal")]
    public GameObject completePanelPanel;
    [Tooltip("Texto del título")]
    public TextMeshProUGUI titleText;
    [Tooltip("Texto de puntuación del nivel")]
    public TextMeshProUGUI levelScoreText;
    [Tooltip("Texto de puntuación total")]
    public TextMeshProUGUI totalScoreText;
    
    [Header("Statistics")]
    [Tooltip("Texto de enemigos eliminados")]
    public TextMeshProUGUI enemiesKilledText;
    [Tooltip("Texto de daño infligido")]
    public TextMeshProUGUI damageDealtText;
    [Tooltip("Texto de daño recibido")]
    public TextMeshProUGUI damageTakenText;

    [Header("Buttons")]
    [Tooltip("Botón para siguiente nivel")]
    public Button nextLevelButton;
    [Tooltip("Botón para reintentar")]
    public Button retryButton;
    [Tooltip("Botón para menú principal")]
    public Button mainMenuButton;

    [Header("Scene Names")]
    [Tooltip("Nombre de la escena del siguiente nivel")]
    public string nextLevelSceneName;
    [Tooltip("Nombre de la escena del menú principal")]
    public string mainMenuSceneName = "MainMenu";

    [Header("References")]
    [Tooltip("Referencia al DoorUnlock (se busca automáticamente si está vacío)")]
    public DoorUnlock doorUnlock;

    void Start() {
        // Ocultar panel al inicio
        if (completePanelPanel) {
            completePanelPanel.SetActive(false);
        }

        // Buscar DoorUnlock si no está asignado
        if (doorUnlock == null) {
            doorUnlock = FindObjectOfType<DoorUnlock>();
        }

        // Configurar botones
        if (nextLevelButton) {
            nextLevelButton.onClick.AddListener(LoadNextLevel);
        }
        if (retryButton) {
            retryButton.onClick.AddListener(RetryLevel);
        }
        if (mainMenuButton) {
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
    }

    /// <summary>
    /// Muestra la pantalla de nivel completado
    /// </summary>
    public void ShowLevelComplete() {
        if (!ScoreManager.Instance) {
            Debug.LogError("[LevelCompleteUI] No se encontró ScoreManager");
            return;
        }

        // Guardar puntuación del nivel
        ScoreManager.Instance.CompleteLevel();

        // Pausar el juego
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Mostrar panel
        if (completePanelPanel) {
            completePanelPanel.SetActive(true);
        }

        // Configurar título
        int currentLevel = ScoreManager.Instance.GetCurrentLevel();
        if (titleText) {
            titleText.text = "¡Nivel " + currentLevel + " Completado!";
        }

        // Mostrar puntuación del nivel
        int levelScore = ScoreManager.Instance.GetCurrentLevelScore();
        if (levelScoreText) {
            levelScoreText.text = "Puntuación del Nivel: " + levelScore.ToString("N0");
        }

        // Mostrar puntuación total
        int totalScore = ScoreManager.Instance.GetTotalScore();
        if (totalScoreText) {
            totalScoreText.text = "Puntuación Total: " + totalScore.ToString("N0");
        }

        // Mostrar estadísticas del NIVEL ACTUAL (CORREGIDO)
        if (enemiesKilledText) {
            enemiesKilledText.text = "Enemigos Eliminados: " + ScoreManager.Instance.GetCurrentLevelEnemiesKilled();
        }
        if (damageDealtText) {
            damageDealtText.text = "Daño Infligido: " + ScoreManager.Instance.GetCurrentLevelDamageDealt();
        }
        if (damageTakenText) {
            damageTakenText.text = "Daño Recibido: " + ScoreManager.Instance.GetCurrentLevelDamageTaken();
        }

        // Ocultar botón de siguiente nivel si es el último
        if (nextLevelButton && ScoreManager.Instance.IsLastLevel()) {
            nextLevelButton.gameObject.SetActive(false);
        }

        Debug.Log("[LevelCompleteUI] Nivel completado mostrado");
    }

    /// <summary>
    /// Carga el siguiente nivel
    /// </summary>
    void LoadNextLevel() {
        Time.timeScale = 1f;
        
        if (ScoreManager.Instance) {
            ScoreManager.Instance.NextLevel();
        }

        // Usar DoorUnlock para cargar la escena si existe
        if (doorUnlock != null) {
            doorUnlock.LoadNextScene();
        } else if (!string.IsNullOrEmpty(nextLevelSceneName)) {
            SceneManager.LoadScene(nextLevelSceneName);
        } else {
            Debug.LogError("[LevelCompleteUI] No se puede cargar el siguiente nivel");
        }
    }

    /// <summary>
    /// Reinicia el nivel actual
    /// </summary>
    void RetryLevel() {
        Time.timeScale = 1f;
        
        if (ScoreManager.Instance) {
            ScoreManager.Instance.RestartLevel();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Vuelve al menú principal
    /// </summary>
    void LoadMainMenu() {
        Time.timeScale = 1f;
        
        if (ScoreManager.Instance) {
            ScoreManager.Instance.ResetGame();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}