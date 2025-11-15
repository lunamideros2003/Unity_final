using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona la puntuación del jugador durante todo el juego
/// Persiste entre escenas usando DontDestroyOnLoad
/// </summary>
public class ScoreManager : MonoBehaviour {
    public static ScoreManager Instance;

    [Header("Score Settings")]
    [Tooltip("Multiplicador de puntos por daño a enemigos")]
    public float damageScoreMultiplier = 1f;
    [Tooltip("Multiplicador de puntos perdidos por recibir daño")]
    public float damagePenaltyMultiplier = 0.5f;
    [Tooltip("Puntuación mínima (no puede bajar de aquí)")]
    public int minimumScore = 0;

    [Header("Level Tracking")]
    [Tooltip("Número total de niveles en el juego")]
    public int totalLevels = 3;

    // Puntuación actual del nivel
    private int currentLevelScore = 0;
    
    // Puntuación de cada nivel (índice 0 = nivel 1)
    private int[] levelScores;
    
    // Nivel actual (1, 2, 3)
    private int currentLevel = 1;
    
    // Estadísticas ACUMULADAS (TOTALES del juego)
    private int totalDamageDealt = 0; // Se acumulará correctamente
    private int totalDamageTaken = 0;
    private int enemiesKilled = 0;

    // Estadísticas del NIVEL ACTUAL (para LevelCompleteUI)
    private int currentLevelDamageDealt = 0;
    private int currentLevelDamageTaken = 0;
    private int currentLevelEnemiesKilled = 0;

    void Awake() {
        // Singleton pattern con persistencia
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            levelScores = new int[totalLevels];
            Debug.Log("[ScoreManager] Sistema de puntuación inicializado");
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Añade puntos por hacer daño a enemigos
    /// </summary>
    public void AddDamageScore(float damage) {
        // Aseguramos que el daño sea positivo
        if (damage <= 0) return;
        
        int points = Mathf.RoundToInt(damage * damageScoreMultiplier);
        currentLevelScore += points;
        
        // 🎯 Acumulación de estadísticas
        int damageInt = Mathf.RoundToInt(damage);
        
        // ✅ SOLUCIÓN DAÑO INFLIGIDO: Acumulación de daño total
        totalDamageDealt += damageInt; 
        
        currentLevelDamageDealt += damageInt; // Del nivel actual
        
        Debug.LogFormat("[ScoreManager] +{0} puntos por daño. Score actual: {1}", points, currentLevelScore);
        
        // Notificar al UI
        ScoreUI scoreUI = FindObjectOfType<ScoreUI>();
        if (scoreUI) {
            scoreUI.UpdateScore(currentLevelScore);
            scoreUI.ShowScorePopup(points, true);
        }
    }

    /// <summary>
    /// Resta puntos por recibir daño
    /// </summary>
    public void SubtractDamageScore(float damage) {
        if (damage <= 0) return;

        int points = Mathf.RoundToInt(damage * damagePenaltyMultiplier);
        currentLevelScore -= points;
        currentLevelScore = Mathf.Max(currentLevelScore, minimumScore);
        
        // 🎯 Acumulación de estadísticas
        int damageInt = Mathf.RoundToInt(damage);
        totalDamageTaken += damageInt; // Total del juego
        currentLevelDamageTaken += damageInt; // Del nivel actual
        
        Debug.LogFormat("[ScoreManager] -{0} puntos por recibir daño. Score actual: {1}", points, currentLevelScore);
        
        // Notificar al UI
        ScoreUI scoreUI = FindObjectOfType<ScoreUI>();
        if (scoreUI) {
            scoreUI.UpdateScore(currentLevelScore);
            scoreUI.ShowScorePopup(points, false);
        }
    }

    /// <summary>
    /// Añade puntos bonus por matar un enemigo
    /// </summary>
    public void AddEnemyKillBonus(int bonus = 50) {
        currentLevelScore += bonus;
        
        // 🎯 Acumulación de estadísticas
        enemiesKilled++; // Total del juego
        currentLevelEnemiesKilled++; // Del nivel actual
        
        Debug.LogFormat("[ScoreManager] +{0} puntos BONUS por muerte. Score: {1}", bonus, currentLevelScore);
        
        ScoreUI scoreUI = FindObjectOfType<ScoreUI>();
        if (scoreUI) {
            scoreUI.UpdateScore(currentLevelScore);
            scoreUI.ShowScorePopup(bonus, true, "KILL!");
        }
    }

    /// <summary>
    /// Guarda la puntuación del nivel actual
    /// </summary>
    public void CompleteLevel() {
        if (currentLevel > 0 && currentLevel <= totalLevels) {
            levelScores[currentLevel - 1] = currentLevelScore;
            Debug.LogFormat("[ScoreManager] Nivel {0} completado con {1} puntos", currentLevel, currentLevelScore);
        }
    }

    /// <summary>
    /// Avanza al siguiente nivel
    /// </summary>
    public void NextLevel() {
        CompleteLevel(); // Guarda el score del nivel que acaba de terminar
        currentLevel++;
        currentLevelScore = 0;
        
        // 🔄 Reset de estadísticas del nivel actual para el nuevo nivel
        currentLevelDamageDealt = 0;
        currentLevelDamageTaken = 0;
        currentLevelEnemiesKilled = 0;

        Debug.LogFormat("[ScoreManager] Avanzando al nivel {0}", currentLevel);
    }

    /// <summary>
    /// Reinicia el nivel actual
    /// </summary>
    public void RestartLevel() {
        currentLevelScore = 0;
        
        // 🔄 Reset de estadísticas del nivel actual (las totales permanecen sin cambios)
        currentLevelDamageDealt = 0;
        currentLevelDamageTaken = 0;
        currentLevelEnemiesKilled = 0;

        Debug.LogFormat("[ScoreManager] Nivel {0} reiniciado", currentLevel);
    }

    /// <summary>
    /// Reinicia todo el juego
    /// </summary>
    public void ResetGame() {
        currentLevel = 1;
        currentLevelScore = 0;
        levelScores = new int[totalLevels];
        
        // 🔄 Reset de TODAS las estadísticas
        totalDamageDealt = 0;
        totalDamageTaken = 0;
        enemiesKilled = 0;
        currentLevelDamageDealt = 0;
        currentLevelDamageTaken = 0;
        currentLevelEnemiesKilled = 0;
        
        Debug.Log("[ScoreManager] Juego reiniciado completamente");
    }

    // Getters
    public int GetCurrentLevelScore() => currentLevelScore;
    public int GetLevelScore(int level) => (level > 0 && level <= totalLevels) ? levelScores[level - 1] : 0;
    
    /// <summary>
    /// 🥇 Calcula la puntuación total (suma de todos los niveles completados)
    /// </summary>
    public int GetTotalScore() {
        // ✅ SOLUCIÓN PUNTUACIÓN TOTAL: Iniciar en 0 para que sea solo la suma de los niveles guardados
        int total = 0; 
        for (int i = 0; i < levelScores.Length; i++) {
            total += levelScores[i]; 
        }
        return total;
    }
    
    public int GetCurrentLevel() => currentLevel;
    public int GetTotalLevels() => totalLevels;

    // Getters para estadísticas del NIVEL ACTUAL (Usado en LevelCompleteUI)
    public int GetCurrentLevelEnemiesKilled() => currentLevelEnemiesKilled;
    public int GetCurrentLevelDamageDealt() => currentLevelDamageDealt;
    public int GetCurrentLevelDamageTaken() => currentLevelDamageTaken;

    // Getters para estadísticas TOTALES (Usado en FinalScoreUI)
    public int GetEnemiesKilled() => enemiesKilled;
    public int GetTotalDamageDealt() => totalDamageDealt;
    public int GetTotalDamageTaken() => totalDamageTaken;
    
    public int[] GetAllLevelScores() => levelScores;

    /// <summary>
    /// Verifica si es el último nivel
    /// </summary>
    public bool IsLastLevel() {
        return currentLevel >= totalLevels;
    }
}