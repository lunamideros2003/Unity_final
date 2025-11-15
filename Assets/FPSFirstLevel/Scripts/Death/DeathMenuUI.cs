using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona el menú que aparece cuando el jugador muere
/// </summary>
public class DeathMenuUI : MonoBehaviour {
    [Header("UI References")]
    public Canvas deathMenuCanvas;
    public Button retryButton;
    public Button mainMenuButton;

    private static DeathMenuUI instance;

    void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }

        // Asegurarse de que el menú esté desactivado al inicio
        if (deathMenuCanvas) {
            deathMenuCanvas.gameObject.SetActive(false);
        }

        // Asignar listeners a los botones
        if (retryButton) {
            retryButton.onClick.AddListener(OnRetryClicked);
        }
        if (mainMenuButton) {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    /// <summary>
    /// Muestra el menú de muerte
    /// </summary>
    public void ShowDeathMenu() {
        if (deathMenuCanvas) {
            deathMenuCanvas.gameObject.SetActive(true);
            Time.timeScale = 0f; // Pausar el juego
            Debug.Log("[DeathMenuUI] Menú de muerte mostrado");
        }
    }

    /// <summary>
    /// Oculta el menú de muerte
    /// </summary>
    public void HideDeathMenu() {
        if (deathMenuCanvas) {
            deathMenuCanvas.gameObject.SetActive(false);
            Time.timeScale = 1f; // Reanudar el juego
        }
    }

    void OnRetryClicked() {
        Debug.Log("[DeathMenuUI] Intentar de nuevo");
        Time.timeScale = 1f; // Asegurarse de despauser antes de reintentar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnMainMenuClicked() {
        Debug.Log("[DeathMenuUI] Volviendo al menú principal");
        Time.timeScale = 1f; // Asegurarse de despauser
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu"); // Asegúrate de que la escena se llama "MainMenu"
    }

    public static DeathMenuUI Instance {
        get { return instance; }
    }
}