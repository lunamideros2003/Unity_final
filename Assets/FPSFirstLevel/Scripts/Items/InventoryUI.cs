using UnityEngine;
using UnityEngine.UI; // Necesario para 'Image'
using TMPro; // 1. Namespace añadido

/// <summary>
/// Muestra el progreso del inventario en pantalla
/// </summary>
public class InventoryUI : MonoBehaviour {
    [Header("UI References")]
    [Tooltip("Texto para mostrar el contador de objetos")]
    public TextMeshProUGUI itemCountText; // 2. Tipo de variable cambiado
    [Tooltip("Imagen de progreso (fill type)")]
    public Image progressBar;

    private InventoryManager inventory;

    void Start() {
        inventory = InventoryManager.Instance;

        if (!inventory) {
            Debug.LogWarning("[InventoryUI] InventoryManager no encontrado");
            return;
        }

        // Suscribirse a cambios de inventario
        inventory.OnInventoryChanged += UpdateUI;

        // Actualizar UI inicial
        UpdateUI();
    }

    void UpdateUI() {
        if (!inventory) return;

        int currentItems = inventory.GetItemCount();
        int requiredItems = inventory.itemsRequiredToUnlock;

        // Actualizar texto
        if (itemCountText) {
            // La asignación .text funciona igual para TextMeshPro
            itemCountText.text = "Llaves: " + currentItems + " / " + requiredItems;
        }

        // Actualizar barra de progreso
        if (progressBar) {
            progressBar.fillAmount = (float)currentItems / requiredItems;
        }

        Debug.Log("[InventoryUI] Actualizado: " + currentItems + " / " + requiredItems);
    }

    void OnDestroy() {
        if (inventory) {
            inventory.OnInventoryChanged -= UpdateUI;
        }
    }
}