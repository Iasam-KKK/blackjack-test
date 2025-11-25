using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to test and debug Inventory V3 setup
/// Attach this to any GameObject to test the inventory system
/// </summary>
public class InventoryV3TestHelper : MonoBehaviour
{
    [Header("Test Controls")]
    [Tooltip("Press this key to toggle inventory")]
    public KeyCode toggleKey = KeyCode.I;
    
    [Header("Debug Info")]
    public bool showDebugLogs = true;
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }
    
    [ContextMenu("Toggle Inventory")]
    public void ToggleInventory()
    {
        if (InventoryManagerV3.Instance == null)
        {
            LogError("InventoryManagerV3.Instance is NULL! Make sure you have an InventoryManagerV3 GameObject in your scene.");
            return;
        }
        
        if (InventoryManagerV3.Instance.inventoryPanelV3 == null)
        {
            LogError("InventoryManagerV3.Instance.inventoryPanelV3 is NULL! Assign the InventoryPanelUIV3 component in the Inspector.");
            return;
        }
        
        Log("Toggling inventory...");
        InventoryManagerV3.Instance.inventoryPanelV3.ToggleInventory();
    }
    
    [ContextMenu("Show Inventory")]
    public void ShowInventory()
    {
        if (InventoryManagerV3.Instance?.inventoryPanelV3 != null)
        {
            Log("Showing inventory...");
            InventoryManagerV3.Instance.inventoryPanelV3.ShowInventory();
        }
        else
        {
            LogError("Cannot show inventory - missing references");
        }
    }
    
    [ContextMenu("Hide Inventory")]
    public void HideInventory()
    {
        if (InventoryManagerV3.Instance?.inventoryPanelV3 != null)
        {
            Log("Hiding inventory...");
            InventoryManagerV3.Instance.inventoryPanelV3.HideInventory();
        }
        else
        {
            LogError("Cannot hide inventory - missing references");
        }
    }
    
    [ContextMenu("Debug Inventory State")]
    public void DebugInventoryState()
    {
        Debug.Log("=== INVENTORY V3 DEBUG STATE ===");
        
        if (InventoryManagerV3.Instance == null)
        {
            Debug.LogError("InventoryManagerV3.Instance is NULL");
            return;
        }
        
        Debug.Log("InventoryManagerV3.Instance: FOUND");
        
        if (InventoryManagerV3.Instance.inventoryPanelV3 == null)
        {
            Debug.LogError("inventoryPanelV3 is NULL");
        }
        else
        {
            Debug.Log("inventoryPanelV3: FOUND");
            
            var panel = InventoryManagerV3.Instance.inventoryPanelV3;
            
            Debug.Log($"  - inventoryPanelV3 GameObject: {(panel.inventoryPanelV3 != null ? panel.inventoryPanelV3.name : "NULL")}");
            Debug.Log($"  - inventoryPanelV3 Active: {(panel.inventoryPanelV3 != null ? panel.inventoryPanelV3.activeSelf.ToString() : "N/A")}");
            Debug.Log($"  - backCloseButton: {(panel.backCloseButton != null ? "ASSIGNED" : "NULL")}");
            
            if (panel.backCloseButton != null)
            {
                Debug.Log($"    - backCloseButton GameObject: {panel.backCloseButton.gameObject.name}");
                Debug.Log($"    - backCloseButton Active: {panel.backCloseButton.gameObject.activeInHierarchy}");
                Debug.Log($"    - backCloseButton Interactable: {panel.backCloseButton.interactable}");
                Debug.Log($"    - backCloseButton onClick listeners: {panel.backCloseButton.onClick.GetPersistentEventCount()}");
            }
            
            Debug.Log($"  - equipButton: {(panel.equipButton != null ? "ASSIGNED" : "NULL")}");
            Debug.Log($"  - unequipButton: {(panel.unequipButton != null ? "ASSIGNED" : "NULL")}");
            Debug.Log($"  - discardButton: {(panel.discardButton != null ? "ASSIGNED" : "NULL")}");
        }
        
        if (InventoryManagerV3.Instance.inventoryData == null)
        {
            Debug.LogError("inventoryData is NULL");
        }
        else
        {
            Debug.Log($"inventoryData: FOUND - {InventoryManagerV3.Instance.inventoryData.storageSlotCount} storage slots");
        }
        
        Debug.Log("=== END DEBUG STATE ===");
    }
    
    [ContextMenu("Test Close Button Directly")]
    public void TestCloseButtonDirectly()
    {
        if (InventoryManagerV3.Instance?.inventoryPanelV3 != null)
        {
            var panel = InventoryManagerV3.Instance.inventoryPanelV3;
            
            if (panel.backCloseButton != null)
            {
                Log("Invoking close button onClick event...");
                panel.backCloseButton.onClick.Invoke();
            }
            else
            {
                LogError("backCloseButton is NULL!");
            }
        }
    }
    
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[InventoryV3TestHelper] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[InventoryV3TestHelper] {message}");
    }
}

