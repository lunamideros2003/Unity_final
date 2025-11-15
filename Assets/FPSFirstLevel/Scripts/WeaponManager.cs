using UnityEngine;
using System.Collections;

/// <summary>
/// Extensión del GunInventory para manejar el respawn de armas correctamente
/// Agrega este componente ADEMÁS del GunInventory existente
/// </summary>
public class WeaponManager : MonoBehaviour {
    
    [Header("Referencias")]
    [Tooltip("El componente GunInventory del jugador")]
    public GunInventory gunInventory;
    
    [Header("Configuración Inicial")]
    [Tooltip("Nombre del arma inicial (debe estar en gunsIHave)")]
    public string initialWeaponName = "";
    [Tooltip("Índice del arma inicial (0 = primera arma)")]
    public int initialWeaponIndex = 0;
    
    private bool hasSpawnedInitialWeapon = false;

    void Awake() {
        if (!gunInventory) {
            gunInventory = GetComponent<GunInventory>();
        }
        
        if (!gunInventory) {
            Debug.LogError("[WeaponManager] No se encontró GunInventory!");
            return;
        }
        
        Debug.Log("[WeaponManager] Inicializado. Armas disponibles: " + gunInventory.gunsIHave.Count);
    }

    void Start() {
        // Esperar un poco para asegurar que todo esté inicializado
        StartCoroutine(SpawnInitialWeaponDelayed());
    }

    IEnumerator SpawnInitialWeaponDelayed() {
        yield return new WaitForSeconds(0.5f);
        
        if (!hasSpawnedInitialWeapon) {
            SpawnInitialWeapon();
        }
    }

    /// <summary>
    /// Spawnea el arma inicial del jugador
    /// </summary>
    public void SpawnInitialWeapon() {
        if (!gunInventory || gunInventory.gunsIHave.Count == 0) {
            Debug.LogWarning("[WeaponManager] No hay armas disponibles para spawnear");
            return;
        }

        Debug.Log("[WeaponManager] Spawneando arma inicial...");

        // Destruir arma actual si existe
        if (gunInventory.currentGun != null) {
            Debug.Log("[WeaponManager] Destruyendo arma existente: " + gunInventory.currentGun.name);
            Destroy(gunInventory.currentGun);
            gunInventory.currentGun = null;
        }

        // Determinar qué arma spawnear
        string weaponToSpawn = "";
        int weaponIndex = 0;

        if (!string.IsNullOrEmpty(initialWeaponName)) {
            // Buscar el arma por nombre
            int index = gunInventory.gunsIHave.IndexOf(initialWeaponName);
            if (index >= 0) {
                weaponToSpawn = initialWeaponName;
                weaponIndex = index;
            } else {
                Debug.LogWarning("[WeaponManager] Arma '" + initialWeaponName + "' no encontrada, usando primera arma");
                weaponToSpawn = gunInventory.gunsIHave[0];
                weaponIndex = 0;
            }
        } else {
            // Usar el índice especificado
            weaponIndex = Mathf.Clamp(initialWeaponIndex, 0, gunInventory.gunsIHave.Count - 1);
            weaponToSpawn = gunInventory.gunsIHave[weaponIndex];
        }

        Debug.Log("[WeaponManager] Intentando cargar: " + weaponToSpawn);

        // Cargar el prefab desde Resources
        GameObject weaponPrefab = Resources.Load<GameObject>(weaponToSpawn);

        if (weaponPrefab) {
            // Instanciar el arma
            gunInventory.currentGun = Instantiate(weaponPrefab, transform.position, Quaternion.identity);
            
            Debug.Log("[WeaponManager] ✓ Arma spawneada exitosamente: " + weaponToSpawn);
            
            // Resetear el contador interno del GunInventory usando reflexión
            try {
                System.Reflection.FieldInfo counterField = gunInventory.GetType().GetField("currentGunCounter", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (counterField != null) {
                    counterField.SetValue(gunInventory, weaponIndex);
                    Debug.Log("[WeaponManager] Contador de arma actual establecido a: " + weaponIndex);
                }
            } catch (System.Exception e) {
                Debug.LogWarning("[WeaponManager] No se pudo actualizar currentGunCounter: " + e.Message);
            }
            
            hasSpawnedInitialWeapon = true;
        } else {
            Debug.LogError("[WeaponManager] ✗ NO se pudo cargar el arma desde Resources: " + weaponToSpawn);
            Debug.LogError("[WeaponManager] Verifica que el prefab esté en la carpeta 'Resources'");
            Debug.LogError("[WeaponManager] Ruta esperada: Assets/Resources/" + weaponToSpawn + ".prefab");
        }
    }

    /// <summary>
    /// Resetea el estado del arma (llama esto en respawn)
    /// </summary>
    public void ResetWeapons() {
        Debug.Log("[WeaponManager] Reseteando armas...");
        hasSpawnedInitialWeapon = false;
        
        // Destruir arma actual
        if (gunInventory && gunInventory.currentGun) {
            Destroy(gunInventory.currentGun);
            gunInventory.currentGun = null;
        }
        
        // Spawnear arma inicial nuevamente
        StartCoroutine(SpawnInitialWeaponDelayed());
    }

    /// <summary>
    /// Limpia todas las armas (útil para muerte del jugador)
    /// </summary>
    public void ClearWeapons() {
        Debug.Log("[WeaponManager] Limpiando todas las armas");
        
        if (gunInventory && gunInventory.currentGun) {
            Destroy(gunInventory.currentGun);
            gunInventory.currentGun = null;
        }
        
        hasSpawnedInitialWeapon = false;
    }
}