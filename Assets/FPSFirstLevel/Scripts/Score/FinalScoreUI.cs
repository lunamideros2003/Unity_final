using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pantalla final que aparece después del último nivel
/// Muestra la puntuación de todos los niveles
/// </summary>
public class FinalScoreUI : MonoBehaviour {
    [Header("UI References")]
    [Tooltip("Panel principal")]
    public GameObject finalPanel;
    [Tooltip("Texto del título")]
    public TextMeshProUGUI titleText;
    [Tooltip("Texto de puntuación total")]
    public TextMeshProUGUI totalScoreText;
    
    [Header("Level Scores")]
    [Tooltip("Texto para nivel 1")]
    public TextMeshProUGUI level1ScoreText;
    [Tooltip("Texto para nivel 2")]
    public TextMeshProUGUI level2ScoreText;
    [Tooltip("Texto para nivel 3")]
    public TextMeshProUGUI level3ScoreText;

    [Header("Statistics")]
    [Tooltip("Texto de enemigos totales eliminados")]
    public TextMeshProUGUI totalEnemiesText;
    [Tooltip("Texto de daño total infligido")]
    public TextMeshProUGUI totalDamageDealtText;
    [Tooltip("Texto de daño total recibido")]
    public TextMeshProUGUI totalDamageTakenText;

    [Header("Ranking")]
    [Tooltip("Texto de calificación")]
    public TextMeshProUGUI rankText;
    [Tooltip("Color para rango S")]
    public Color rankSColor = Color.yellow;
    [Tooltip("Color para rango A")]
    public Color rankAColor = Color.green;
    [Tooltip("Color para rango B")]
    public Color rankBColor = Color.blue;
    [Tooltip("Color para rango C")]
    public Color rankCColor = Color.gray;

    [Header("Buttons")]
    [Tooltip("Botón para jugar de nuevo")]
    public Button playAgainButton;
    [Tooltip("Botón para menú principal")]
    public Button mainMenuButton;

    [Header("Scene Names")]
    [Tooltip("Nombre de la escena del primer nivel")]
    public string firstLevelSceneName = "Level1";
    [Tooltip("Nombre de la escena del menú principal")]
    public string mainMenuSceneName = "MainMenu";

    void Start() {
        // Ocultar panel al inicio
        if (finalPanel) {
            finalPanel.SetActive(false);
        }

        // Configurar botones
        if (playAgainButton) {
            playAgainButton.onClick.AddListener(PlayAgain);
        }
        if (mainMenuButton) {
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
    }

    /// <summary>
    /// Muestra la pantalla de puntuación final
    /// </summary>
    public void ShowFinalScore() {
        if (!ScoreManager.Instance) {
            Debug.LogError("[FinalScoreUI] No se encontró ScoreManager");
            return;
        }

        // Guardar último nivel (si no se ha hecho ya en LevelCompleteUI)
        ScoreManager.Instance.CompleteLevel();

        // Pausar el juego
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Mostrar panel
        if (finalPanel) {
            finalPanel.SetActive(true);
        }

        // Título
        if (titleText) {
            titleText.text = "¡Juego Completado!";
        }

        // Puntuación total
        int totalScore = ScoreManager.Instance.GetTotalScore();
        if (totalScoreText) {
            totalScoreText.text = "Puntuación Total: " + totalScore.ToString("N0");
        }

        // Puntuaciones individuales de cada nivel
        int[] levelScores = ScoreManager.Instance.GetAllLevelScores();
        
        if (level1ScoreText && levelScores.Length > 0) {
            level1ScoreText.text = "Nivel 1: " + levelScores[0].ToString("N0") + " pts";
        }
        if (level2ScoreText && levelScores.Length > 1) {
            level2ScoreText.text = "Nivel 2: " + levelScores[1].ToString("N0") + " pts";
        }
        if (level3ScoreText && levelScores.Length > 2) {
            level3ScoreText.text = "Nivel 3: " + levelScores[2].ToString("N0") + " pts";
        }

        // Estadísticas totales (CORREGIDO)
        if (totalEnemiesText) {
            totalEnemiesText.text = "Enemigos Eliminados: " + ScoreManager.Instance.GetEnemiesKilled();
        }
        if (totalDamageDealtText) {
            totalDamageDealtText.text = "Daño Total Infligido: " + ScoreManager.Instance.GetTotalDamageDealt();
        }
        if (totalDamageTakenText) {
            totalDamageTakenText.text = "Daño Total Recibido: " + ScoreManager.Instance.GetTotalDamageTaken();
        }

        // Calcular y mostrar rango
        if (rankText) {
            string rank = CalculateRank(totalScore);
            rankText.text = "Rango: " + rank;
            
            // Colorear según el rango
            switch (rank) {
                case "S": rankText.color = rankSColor; break;
                case "A": rankText.color = rankAColor; break;
                case "B": rankText.color = rankBColor; break;
                case "C": rankText.color = rankCColor; break;
                default: rankText.color = Color.white; break;
            }
        }

        Debug.Log("[FinalScoreUI] Puntuación final mostrada: " + totalScore);
    }

    /// <summary>
    /// Calcula el rango según la puntuación total
    /// </summary>
    string CalculateRank(int score) {
        if (score >= 5000) return "S";
        if (score >= 3000) return "A";
        if (score >= 1500) return "B";
        if (score >= 500) return "C";
        return "D";
    }

    /// <summary>
    /// Reinicia el juego desde el nivel 1
    /// </summary>
    void PlayAgain() {
        Time.timeScale = 1f;
        
        if (ScoreManager.Instance) {
            ScoreManager.Instance.ResetGame();
        }

        SceneManager.LoadScene(firstLevelSceneName);
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