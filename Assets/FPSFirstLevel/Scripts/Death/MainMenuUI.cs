using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona el menú principal del juego
/// </summary>
public class MainMenuUI : MonoBehaviour {
    [Header("UI References")]
    public Button newGameButton;
    public Button continueButton;
    public Button exitButton;

    void Awake() {
        // Asegurarse de que el juego no esté pausado
        Time.timeScale = 1f;

        // Desbloquear y mostrar el cursor en el menú
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Asignar listeners a los botones
        if (newGameButton) {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }
        if (continueButton) {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        if (exitButton) {
            exitButton.onClick.AddListener(OnExitClicked);
        }

        // Desactivar el botón "Continuar" si no hay guardado
        if (continueButton) {
            continueButton.interactable = PlayerPrefs.HasKey("GameSaved");
        }
    }

    void OnNewGameClicked() {
        Debug.Log("[MainMenuUI] Nueva partida");
        // Limpiar cualquier guardado previo
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("LevelOne");
    }

    void OnContinueClicked() {
        Debug.Log("[MainMenuUI] Continuar partida");
        // Aquí iría la lógica para cargar el juego guardado
        // Por ahora simplemente cargamos la escena del juego
        SceneManager.LoadScene("LevelOne");
    }

    void OnExitClicked() {
        Debug.Log("[MainMenuUI] Saliendo del juego");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}