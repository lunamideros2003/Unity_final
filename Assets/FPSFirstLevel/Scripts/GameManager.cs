using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Gestiona el estado del juego, respawn de jugador y reinicio de enemigos
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [Header("Player Settings")]
    [Tooltip("Reference to the player")]
    public GameObject player;
    [Tooltip("Respawn point")]
    public Transform respawnPoint;

    [Header("Enemy Management")]
    [Tooltip("Track all enemies in scene")]
    public bool trackEnemies = true;
    
    // Lista de estados iniciales de enemigos
    private List<EnemyInitialState> enemyStates = new List<EnemyInitialState>();
    
    // Referencias a componentes del jugador
    private PlayerHealth playerHealth;
    private GunInventory gunInventory;

    // Clase para guardar estado inicial de enemigos
    [System.Serializable]
    private class EnemyInitialState {
        public GameObject enemy;
        public Vector3 position;
        public Quaternion rotation;
        public float health;
        public bool wasActive;
    }

    void Awake() {
        // Singleton
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }

        // Encontrar jugador si no está asignado
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player) {
            playerHealth = player.GetComponent<PlayerHealth>();
            gunInventory = player.GetComponent<GunInventory>();
        }

        // Guardar estados iniciales
        SaveInitialStates();
    }

    void Start() {
        // Asegurarse de que el jugador tenga un arma al inicio
        if (gunInventory && gunInventory.gunsIHave.Count > 0) {
            Debug.Log("[GameManager] Jugador tiene " + gunInventory.gunsIHave.Count + " armas disponibles");
            SpawnInitialWeapon();
        }
    }

    /// <summary>
    /// Guarda los estados iniciales de todos los enemigos
    /// </summary>
    void SaveInitialStates() {
        if (!trackEnemies) return;

        enemyStates.Clear();

        // Encontrar todos los objetos con EnemyHealth
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        
        Debug.Log("[GameManager] Guardando estado de " + enemies.Length + " enemigos");

        foreach (EnemyHealth enemy in enemies) {
            EnemyInitialState state = new EnemyInitialState();
            state.enemy = enemy.gameObject;
            state.position = enemy.transform.position;
            state.rotation = enemy.transform.rotation;
            state.health = enemy.maxHealth;
            state.wasActive = enemy.gameObject.activeSelf;
            
            enemyStates.Add(state);
        }
    }

    /// <summary>
    /// Reinicia todos los enemigos a su estado inicial
    /// </summary>
    void ResetAllEnemies() {
        if (!trackEnemies) return;

        Debug.Log("[GameManager] Reiniciando " + enemyStates.Count + " enemigos");

        foreach (EnemyInitialState state in enemyStates) {
            if (state.enemy == null) {
                Debug.LogWarning("[GameManager] Enemigo fue destruido");
                continue;
            }

            // Reactivar si estaba destruido
            if (!state.enemy.activeSelf && state.wasActive) {
                state.enemy.SetActive(true);
            }

            // Restaurar posición y rotación
            state.enemy.transform.position = state.position;
            state.enemy.transform.rotation = state.rotation;

            // Restaurar salud
            EnemyHealth enemyHealth = state.enemy.GetComponent<EnemyHealth>();
            if (enemyHealth) {
                enemyHealth.currentHealth = state.health;
                System.Reflection.FieldInfo isDead = enemyHealth.GetType().GetField("isDead", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (isDead != null) {
                    isDead.SetValue(enemyHealth, false);
                }
            }

            // Reiniciar IA
            EnemyAI enemyAI = state.enemy.GetComponent<EnemyAI>();
            if (enemyAI) {
                enemyAI.enabled = false;
                enemyAI.enabled = true;
            }

            // Reiniciar NavMeshAgent
            UnityEngine.AI.NavMeshAgent agent = state.enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent) {
                agent.enabled = false;
                agent.enabled = true;
                agent.ResetPath();
            }

            // Reactivar colliders
            Collider[] colliders = state.enemy.GetComponents<Collider>();
            foreach (Collider col in colliders) {
                col.enabled = true;
            }

            // Reiniciar animator
            Animator animator = state.enemy.GetComponent<Animator>();
            if (animator) {
                animator.enabled = true;
                animator.Rebind();
            }
        }
    }

    /// <summary>
    /// Spawneea el arma inicial
    /// </summary>
    void SpawnInitialWeapon() {
        if (!gunInventory || gunInventory.gunsIHave.Count == 0) {
            Debug.LogWarning("[GameManager] No hay armas disponibles");
            return;
        }

        // Destruir arma actual si existe
        if (gunInventory.currentGun != null) {
            Destroy(gunInventory.currentGun);
            gunInventory.currentGun = null;
        }

        // Obtener nombre de la primera arma
        string weaponName = gunInventory.gunsIHave[0];
        Debug.Log("[GameManager] Spawneando arma inicial: " + weaponName);

        // Cargar prefab desde Resources
        GameObject weaponPrefab = Resources.Load<GameObject>(weaponName);
        
        if (weaponPrefab) {
            // Instanciar el arma como hijo del jugador
            gunInventory.currentGun = Instantiate(weaponPrefab, gunInventory.transform);
            gunInventory.currentGun.transform.localPosition = Vector3.zero;
            gunInventory.currentGun.transform.localRotation = Quaternion.identity;
            
            Debug.Log("[GameManager] Arma spawneada exitosamente: " + weaponName);
        } else {
            Debug.LogError("[GameManager] No se pudo cargar el arma: " + weaponName + " desde Resources");
        }
    }

    /// <summary>
    /// Llamar esto cuando el jugador muera
    /// </summary>
    public void OnPlayerDeath() {
        Debug.Log("[GameManager] Jugador murió");
        // El menú de muerte se mostrará desde PlayerHealth
        // Aquí solo reiniciamos enemigos
        ResetAllEnemies();
    }

    /// <summary>
    /// Reinicia el nivel completamente
    /// </summary>
    public void RestartLevel() {
        Debug.Log("[GameManager] Reiniciando nivel");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}