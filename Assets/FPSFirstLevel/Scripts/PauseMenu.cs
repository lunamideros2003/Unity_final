using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Menú de pausa que detiene el juego y permite reanudar o salir
/// </summary>
public class PauseMenu : MonoBehaviour {
    [Header("UI References")]
    [Tooltip("Panel principal del menú de pausa")]
    public GameObject pauseMenuPanel;
    [Tooltip("Título del menú")]
    public TextMeshProUGUI titleText;

    [Header("Buttons")]
    [Tooltip("Botón para reanudar")]
    public Button resumeButton;
    [Tooltip("Botón para reiniciar nivel")]
    public Button restartButton;
    [Tooltip("Botón para ir al menú principal")]
    public Button mainMenuButton;
    [Tooltip("Botón para salir del juego")]
    public Button quitButton;

    [Header("Settings")]
    [Tooltip("Tecla para pausar/reanudar")]
    public KeyCode pauseKey = KeyCode.Escape;
    [Tooltip("Nombre de la escena del menú principal")]
    public string mainMenuSceneName = "MainMenu";
    [Tooltip("Desactivar ScoreUI cuando está pausado")]
    public bool hideScoreUI = true;

    [Header("Audio (Opcional)")]
    [Tooltip("Sonido al abrir el menú")]
    public AudioSource openSound;
    [Tooltip("Sonido al cerrar el menú")]
    public AudioSource closeSound;

    private bool isPaused = false;
    private ScoreUI scoreUI;

    void Start() {
        // Ocultar menú al inicio
        if (pauseMenuPanel) {
            pauseMenuPanel.SetActive(false);
        }

        // Configurar botones
        if (resumeButton) {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (restartButton) {
            restartButton.onClick.AddListener(RestartLevel);
        }
        if (mainMenuButton) {
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
        if (quitButton) {
            quitButton.onClick.AddListener(QuitGame);
        }

        // Buscar ScoreUI
        scoreUI = FindObjectOfType<ScoreUI>();

        // Configurar título si existe
        if (titleText) {
            titleText.text = "PAUSA";
        }

        Debug.Log("[PauseMenu] Menú de pausa inicializado - Presiona " + pauseKey + " para pausar");
    }

    void Update() {
        // Detectar tecla de pausa
        if (Input.GetKeyDown(pauseKey)) {
            if (isPaused) {
                ResumeGame();
            } else {
                PauseGame();
            }
        }
    }

    /// <summary>
    /// Pausa el juego
    /// </summary>
    public void PauseGame() {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        // Mostrar menú
        if (pauseMenuPanel) {
            pauseMenuPanel.SetActive(true);
        }

        // Habilitar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ocultar ScoreUI si está configurado
        if (hideScoreUI && scoreUI && scoreUI.gameObject) {
            scoreUI.gameObject.SetActive(false);
        }

        // Sonido
        if (openSound) {
            openSound.PlayOneShot(openSound.clip);
        }

        Debug.Log("[PauseMenu] Juego pausado");
    }

    /// <summary>
    /// Reanuda el juego
    /// </summary>
    public void ResumeGame() {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        // Ocultar menú
        if (pauseMenuPanel) {
            pauseMenuPanel.SetActive(false);
        }

        // Bloquear cursor (depende de tu juego)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Mostrar ScoreUI de nuevo
        if (hideScoreUI && scoreUI && scoreUI.gameObject) {
            scoreUI.gameObject.SetActive(true);
        }

        // Sonido
        if (closeSound) {
            closeSound.PlayOneShot(closeSound.clip);
        }

        Debug.Log("[PauseMenu] Juego reanudado");
    }

    /// <summary>
    /// Reinicia el nivel actual
    /// </summary>
    public void RestartLevel() {
        Time.timeScale = 1f;
        isPaused = false;

        // Reiniciar puntuación del nivel
        if (ScoreManager.Instance) {
            ScoreManager.Instance.RestartLevel();
        }

        // Recargar escena
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);

        Debug.Log("[PauseMenu] Reiniciando nivel: " + currentScene.name);
    }

    /// <summary>
    /// Carga el menú principal
    /// </summary>
    public void LoadMainMenu() {
        Time.timeScale = 1f;
        isPaused = false;

        // Opcional: Resetear el juego completo
        if (ScoreManager.Instance) {
            // Descomentar si quieres resetear TODO al volver al menú
            // ScoreManager.Instance.ResetGame();
        }

        if (!string.IsNullOrEmpty(mainMenuSceneName)) {
            SceneManager.LoadScene(mainMenuSceneName);
            Debug.Log("[PauseMenu] Cargando menú principal: " + mainMenuSceneName);
        } else {
            Debug.LogError("[PauseMenu] No se configuró el nombre del menú principal");
        }
    }

    /// <summary>
    /// Sale del juego
    /// </summary>
    public void QuitGame() {
        Debug.Log("[PauseMenu] Saliendo del juego...");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Verifica si el juego está pausado
    /// </summary>
    public bool IsPaused() {
        return isPaused;
    }

    void OnDestroy() {
        // Asegurar que el tiempo vuelva a la normalidad
        Time.timeScale = 1f;
    }
}