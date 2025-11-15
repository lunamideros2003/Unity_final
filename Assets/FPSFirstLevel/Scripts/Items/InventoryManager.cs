using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona el inventario del jugador
/// </summary>
public class InventoryManager : MonoBehaviour {
    public static InventoryManager Instance;

    [Header("Inventory Settings")]
    [Tooltip("Cantidad de objetos necesarios para desbloquear puertas")]
    public int itemsRequiredToUnlock = 5;

    [Header("Lantern Settings")]
    [Tooltip("Referencia al jugador (para activar el farol)")]
    public GameObject player;
    [Tooltip("Nombre del item farol (debe coincidir con LanternItem)")]
    public string lanternItemName = "Farol";

    private List<string> collectedItems = new List<string>();
    private int totalItemsCollected = 0;
    private bool hasLantern = false;

    // Delegado para notificar cambios en el inventario
    public delegate void InventoryChangedDelegate();
    public event InventoryChangedDelegate OnInventoryChanged;

    // Delegado específico para el farol
    public delegate void LanternCollectedDelegate();
    public event LanternCollectedDelegate OnLanternCollected;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        // Buscar al jugador si no está asignado
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    /// <summary>
    /// Añade un objeto al inventario
    /// </summary>
    public void AddItem(string itemName) {
        collectedItems.Add(itemName);
        totalItemsCollected++;

        Debug.Log("[InventoryManager] Objeto añadido: " + itemName + " (Total: " + totalItemsCollected + "/" + itemsRequiredToUnlock + ")");

        // Verificar si es el farol
        if (itemName == lanternItemName && !hasLantern) {
            hasLantern = true;
            ActivatePlayerLantern();
            OnLanternCollected?.Invoke();
        }

        // Notificar que el inventario cambió
        OnInventoryChanged?.Invoke();

        // Si tenemos suficientes objetos, desbloquear puertas
        if (totalItemsCollected >= itemsRequiredToUnlock) {
            UnlockAllDoors();
        }
    }

    /// <summary>
    /// Activa el farol del jugador
    /// </summary>
    void ActivatePlayerLantern() {
        if (player == null) {
            Debug.LogWarning("[InventoryManager] No se encontró referencia al jugador");
            return;
        }

        LanternController lanternController = player.GetComponent<LanternController>();
        if (lanternController != null) {
            lanternController.ActivateLantern();
            Debug.Log("[InventoryManager] Farol activado desde InventoryManager");
        } else {
            Debug.LogWarning("[InventoryManager] El jugador no tiene LanternController");
        }
    }

    /// <summary>
    /// Obtiene la cantidad total de objetos recolectados
    /// </summary>
    public int GetItemCount() {
        return totalItemsCollected;
    }

    /// <summary>
    /// Verifica si se han recolectado suficientes objetos
    /// </summary>
    public bool IsUnlocked() {
        return totalItemsCollected >= itemsRequiredToUnlock;
    }

    /// <summary>
    /// Verifica si el jugador tiene el farol
    /// </summary>
    public bool HasLantern() {
        return hasLantern;
    }

    /// <summary>
    /// Obtiene los objetos recolectados
    /// </summary>
    public List<string> GetCollectedItems() {
        return new List<string>(collectedItems);
    }

    /// <summary>
    /// Verifica si un objeto específico está en el inventario
    /// </summary>
    public bool HasItem(string itemName) {
        return collectedItems.Contains(itemName);
    }

    /// <summary>
    /// Desbloquea todas las puertas en la escena
    /// </summary>
    void UnlockAllDoors() {
        DoorUnlock[] doors = FindObjectsOfType<DoorUnlock>();
        Debug.Log("[InventoryManager] Desbloqueando " + doors.Length + " puertas");

        foreach (DoorUnlock door in doors) {
            door.Unlock();
        }
    }

    /// <summary>
    /// Reinicia el inventario
    /// </summary>
    public void ResetInventory() {
        collectedItems.Clear();
        totalItemsCollected = 0;
        hasLantern = false;

        // Desactivar el farol del jugador
        if (player != null) {
            LanternController lanternController = player.GetComponent<LanternController>();
            if (lanternController != null) {
                lanternController.DeactivateLantern();
            }
        }

        Debug.Log("[InventoryManager] Inventario reiniciado");
    }
}