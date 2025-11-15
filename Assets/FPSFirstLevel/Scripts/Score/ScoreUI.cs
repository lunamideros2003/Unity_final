using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // <-- 1. AÑADIR ESTA LÍNEA

/// <summary>
/// UI que muestra la puntuación durante el juego
/// </summary>
public class ScoreUI : MonoBehaviour {
    [Header("UI References")]
    [Tooltip("Texto que muestra la puntuación actual")]
    public TextMeshProUGUI scoreText; // <-- 2. CAMBIADO
    [Tooltip("Texto que muestra el nivel actual")]
    public TextMeshProUGUI levelText; // <-- 2. CAMBIADO

    [Header("Score Popup")]
    [Tooltip("Texto para mostrar puntos ganados/perdidos")]
    public TextMeshProUGUI scorePopupText; // <-- 2. CAMBIADO
    [Tooltip("Color cuando ganas puntos")]
    public Color gainColor = Color.green;
    [Tooltip("Color cuando pierdes puntos")]
    public Color loseColor = Color.red;
    [Tooltip("Duración del popup")]
    public float popupDuration = 1f;

    [Header("Statistics")]
    [Tooltip("Texto para enemigos eliminados (opcional)")]
    public TextMeshProUGUI enemiesKilledText; // <-- 2. CAMBIADO
    [Tooltip("Texto para daño infligido (opcional)")]
    public TextMeshProUGUI damageDealtText; // <-- 2. CAMBIADO

    private Coroutine popupCoroutine;

    void Start() {
        UpdateScore(0);
        UpdateLevel();
        
        if (scorePopupText) {
            scorePopupText.gameObject.SetActive(false);
        }
    }

    void Update() {
        // Actualizar estadísticas en tiempo real
        if (ScoreManager.Instance) {
            if (enemiesKilledText) {
                enemiesKilledText.text = "Enemigos: " + ScoreManager.Instance.GetEnemiesKilled();
            }
            if (damageDealtText) {
                damageDealtText.text = "Daño: " + ScoreManager.Instance.GetTotalDamageDealt();
            }
        }
    }

    /// <summary>
    /// Actualiza el texto de puntuación
    /// </summary>
    public void UpdateScore(int score) {
        if (scoreText) {
            scoreText.text = "Puntuación: " + score.ToString("N0");
        }
    }

    /// <summary>
    /// Actualiza el texto del nivel
    /// </summary>
    public void UpdateLevel() {
        if (levelText && ScoreManager.Instance) {
            int current = ScoreManager.Instance.GetCurrentLevel();
            int total = ScoreManager.Instance.GetTotalLevels();
            levelText.text = "Nivel " + current + "/" + total;
        }
    }

    /// <summary>
    /// Muestra un popup con los puntos ganados/perdidos
    /// </summary>
    public void ShowScorePopup(int points, bool isGain, string customText = null) {
        if (!scorePopupText) return;

        if (popupCoroutine != null) {
            StopCoroutine(popupCoroutine);
        }

        popupCoroutine = StartCoroutine(ShowPopupCoroutine(points, isGain, customText));
    }

    IEnumerator ShowPopupCoroutine(int points, bool isGain, string customText) {
        scorePopupText.gameObject.SetActive(true);
        
        // Configurar texto
        string sign = isGain ? "+" : "-";
        string text = customText != null ? customText : sign + points;
        scorePopupText.text = text;
        
        // Configurar color
        scorePopupText.color = isGain ? gainColor : loseColor;
        
        // Animación simple de fade out
        float elapsed = 0f;
        Color startColor = scorePopupText.color;
        Vector3 startPos = scorePopupText.transform.localPosition;
        
        while (elapsed < popupDuration) {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / popupDuration);
            
            Color newColor = startColor;
            newColor.a = alpha;
            scorePopupText.color = newColor;
            
            // Mover hacia arriba
            Vector3 newPos = startPos;
            newPos.y += (elapsed / popupDuration) * 50f;
            scorePopupText.transform.localPosition = newPos;
            
            yield return null;
        }
        
        scorePopupText.gameObject.SetActive(false);
        scorePopupText.transform.localPosition = startPos;
    }
}