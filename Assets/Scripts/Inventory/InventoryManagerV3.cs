using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManagerV3 : InventoryManager
{
    public static new InventoryManagerV3 Instance { get; private set; }
    
    [Header("V3 UI References")]
    public InventoryPanelUIV3 inventoryPanelV3;
    
    [Header("V3 State Management")]
    private InventorySlotUIV3 selectedSlot;
    private TabType currentTab = TabType.AllCards;
    
    private new void Awake()
    {
        Debug.Log("[InventoryManagerV3] Awake called - implementing full singleton pattern");
        
        // IMPORTANT: 'new' keyword HIDES the base Awake(), it doesn't call it!
        // We must manually implement the entire singleton pattern here
        
        if (InventoryManager.Instance == null)
        {
            // Use reflection to set the private setter, or just work with what we have
            // We'll work around this by only checking InventoryManagerV3.Instance
            
            // Set V3 Instance (we can set this)
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[InventoryManagerV3] Singleton instance created (V3)");
            
            // Verify and initialize inventoryData
            if (inventoryData == null)
            {
                Debug.LogError("[InventoryManagerV3] inventoryData is NULL! Please assign 'NewInventoryData' in Inspector!");
                return;
            }
            
            Debug.Log($"[InventoryManagerV3] inventoryData found: {inventoryData.name}");
            
            // Initialize inventory data (this creates the slots)
            inventoryData.InitializeSlots();
            
            // Load saved inventory
            if (enablePersistence)
            {
                LoadInventory();
            }
            
            Debug.Log($"[InventoryManagerV3] Initialized with {inventoryData.storageSlotCount} storage slots and {inventoryData.equipmentSlotCount} equipment slots");
        }
        else
        {
            Debug.LogWarning($"[InventoryManagerV3] Duplicate instance detected, destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Double-check inventoryData is initialized
        if (inventoryData == null)
        {
            Debug.LogError("[InventoryManagerV3] Start - inventoryData is still NULL! Cannot initialize UI!");
            return;
        }
        
        Debug.Log($"[InventoryManagerV3] Start - inventoryData ready: {inventoryData.storageSlotCount} storage slots");
        
        // Find and initialize V3 UI panel if not assigned
        if (inventoryPanelV3 == null)
        {
            inventoryPanelV3 = FindObjectOfType<InventoryPanelUIV3>();
            Debug.Log($"[InventoryManagerV3] Searched for InventoryPanelUIV3: {(inventoryPanelV3 != null ? "Found" : "Not Found")}");
        }
        
        if (inventoryPanelV3 != null)
        {
            Debug.Log("[InventoryManagerV3] Calling inventoryPanelV3.Initialize()");
            inventoryPanelV3.Initialize();
        }
        else
        {
            Debug.LogError("[InventoryManagerV3] InventoryPanelUIV3 not found! Cannot initialize inventory UI!");
        }
    }
    
    // V3 Specific Methods
    
    public void SetSelectedSlot(InventorySlotUIV3 slot)
    {
        // Clear previous selection
        if (selectedSlot != null && selectedSlot != slot)
        {
            selectedSlot.SetSelected(false);
        }
        
        selectedSlot = slot;
        
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(true);
            Debug.Log($"[InventoryManagerV3] Card selected: {selectedSlot.slotData?.storedCard?.cardName ?? "Empty"} from {(selectedSlot.slotData?.isEquipmentSlot == true ? "Equipment" : "Storage")} slot");
        }
        
        // Update UI
        if (inventoryPanelV3 != null)
        {
            if (selectedSlot != null && selectedSlot.slotData != null && selectedSlot.slotData.isOccupied)
            {
                inventoryPanelV3.UpdateSelectedCard(selectedSlot.slotData);
            }
            else
            {
                inventoryPanelV3.ClearCardDescription();
            }
            
            // Update action buttons based on selection
            inventoryPanelV3.UpdateActionButtons();
        }
    }
    
    public InventorySlotUIV3 GetSelectedSlot()
    {
        return selectedSlot;
    }
    
    public void ClearSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }
        
        if (inventoryPanelV3 != null)
        {
            inventoryPanelV3.ClearCardDescription();
        }
    }
    
    public TabType GetCurrentTab()
    {
        return currentTab;
    }
    
    public void SetCurrentTab(TabType tab)
    {
        currentTab = tab;
    }
    
    // Override base methods to also update V3 UI
    
    public override bool AddPurchasedCard(TarotCardData card)
    {
        Debug.Log($"[InventoryManagerV3] AddPurchasedCard override called for '{card?.cardName}'");
        
        bool success = base.AddPurchasedCard(card);
        
        if (success)
        {
            Debug.Log($"[InventoryManagerV3] Card '{card.cardName}' added to inventory - refreshing V3 UI");
            
            // Find the panel if not assigned
            if (inventoryPanelV3 == null)
            {
                inventoryPanelV3 = FindObjectOfType<InventoryPanelUIV3>();
                Debug.Log($"[InventoryManagerV3] Searched for InventoryPanelUIV3: {(inventoryPanelV3 != null ? "Found" : "Not Found")}");
            }
            
            if (inventoryPanelV3 != null)
            {
                // Immediate refresh
                inventoryPanelV3.RefreshAllSlots();
                inventoryPanelV3.RefreshEquipmentSlots();
                inventoryPanelV3.UpdateOverviewStats();
                
                // Also do a delayed refresh to ensure UI updates properly
                StartCoroutine(DelayedUIRefresh());
                
                Debug.Log($"[InventoryManagerV3] V3 UI refreshed successfully");
            }
            else
            {
                Debug.LogWarning("[InventoryManagerV3] InventoryPanelUIV3 not found - cannot refresh UI!");
            }
            
            // Show a brief notification that the card was added
            ShowCardAddedNotification(card);
        }
        else
        {
            Debug.LogWarning($"[InventoryManagerV3] Failed to add card '{card.cardName}' - inventory may be full");
        }
        
        return success;
    }
    
    /// <summary>
    /// Delayed UI refresh to ensure updates take effect
    /// </summary>
    private IEnumerator DelayedUIRefresh()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);
        
        if (inventoryPanelV3 != null)
        {
            inventoryPanelV3.RefreshAllSlots();
            inventoryPanelV3.RefreshEquipmentSlots();
            inventoryPanelV3.UpdateOverviewStats();
            
            // Force canvas update to ensure visual refresh
            Canvas.ForceUpdateCanvases();
            
            Debug.Log("[InventoryManagerV3] Delayed UI refresh completed with forced canvas update");
        }
    }
    
    private void ShowCardAddedNotification(TarotCardData card)
    {
        // Simple debug notification - can be enhanced with UI toast later
        Debug.Log($"✓ '{card.cardName}' added to inventory!");
    }
    
    /// <summary>
    /// Force a complete inventory UI refresh (public method for external calls)
    /// </summary>
    public void ForceCompleteUIRefresh()
    {
        if (inventoryPanelV3 != null)
        {
            Debug.Log("[InventoryManagerV3] ForceCompleteUIRefresh called");
            inventoryPanelV3.RefreshAllSlots();
            inventoryPanelV3.RefreshEquipmentSlots();
            inventoryPanelV3.UpdateOverviewStats();
            Canvas.ForceUpdateCanvases();
        }
    }
    
    public override bool EquipCardFromStorage(int storageSlotIndex, int equipmentSlotIndex = -1)
    {
        bool success = base.EquipCardFromStorage(storageSlotIndex, equipmentSlotIndex);
        
        if (success && inventoryPanelV3 != null)
        {
            inventoryPanelV3.RefreshAllSlots();
            inventoryPanelV3.RefreshEquipmentSlots();
            inventoryPanelV3.UpdateOverviewStats();
            ClearSelection();
        }
        
        return success;
    }
    
    public override bool UnequipCard(int equipmentSlotIndex)
    {
        bool success = base.UnequipCard(equipmentSlotIndex);
        
        if (success && inventoryPanelV3 != null)
        {
            inventoryPanelV3.RefreshAllSlots();
            inventoryPanelV3.RefreshEquipmentSlots();
            inventoryPanelV3.UpdateOverviewStats();
            ClearSelection();
        }
        
        return success;
    }
    
    public override void RemoveUsedUpCard(TarotCardData card)
    {
        base.RemoveUsedUpCard(card);
        
        if (inventoryPanelV3 != null)
        {
            inventoryPanelV3.RefreshAllSlots();
            inventoryPanelV3.RefreshEquipmentSlots();
            inventoryPanelV3.UpdateOverviewStats();
            ClearSelection();
        }
    }
    
    // V3 Action Methods
    
    public bool EquipSelectedCard()
    {
        Debug.Log($"[InventoryManagerV3] EquipSelectedCard called - selectedSlot: {(selectedSlot != null)}");
        
        if (selectedSlot == null || selectedSlot.slotData == null || !selectedSlot.slotData.isOccupied)
        {
            Debug.LogWarning($"[InventoryManagerV3] Cannot equip - Invalid selection: slot={selectedSlot != null}, data={selectedSlot?.slotData != null}, occupied={selectedSlot?.slotData?.isOccupied}");
            return false;
        }
        
        // Can only equip from storage slots
        if (selectedSlot.slotData.isEquipmentSlot)
        {
            Debug.LogWarning("[InventoryManagerV3] Cannot equip - Card is already in equipment slot");
            return false;
        }
        
        Debug.Log($"[InventoryManagerV3] Attempting to equip '{selectedSlot.slotData.storedCard.cardName}' from storage slot {selectedSlot.slotIndex}");
        return EquipCardFromStorage(selectedSlot.slotIndex);
    }
    
    public bool UnequipSelectedCard()
    {
        Debug.Log($"[InventoryManagerV3] UnequipSelectedCard called - selectedSlot: {(selectedSlot != null)}");
        
        if (selectedSlot == null || selectedSlot.slotData == null || !selectedSlot.slotData.isOccupied)
        {
            Debug.LogWarning($"[InventoryManagerV3] Cannot unequip - Invalid selection: slot={selectedSlot != null}, data={selectedSlot?.slotData != null}, occupied={selectedSlot?.slotData?.isOccupied}");
            return false;
        }
        
        // Can only unequip from equipment slots
        if (!selectedSlot.slotData.isEquipmentSlot)
        {
            Debug.LogWarning("[InventoryManagerV3] Cannot unequip - Card is not in equipment slot");
            return false;
        }
        
        Debug.Log($"[InventoryManagerV3] Attempting to unequip '{selectedSlot.slotData.storedCard.cardName}' from equipment slot {selectedSlot.slotIndex}");
        return UnequipCard(selectedSlot.slotIndex);
    }
    
    public bool DiscardSelectedCard()
    {
        if (selectedSlot == null || selectedSlot.slotData == null || !selectedSlot.slotData.isOccupied)
        {
            return false;
        }
        
        TarotCardData card = selectedSlot.slotData.storedCard;
        RemoveUsedUpCard(card);
        return true;
    }
    
    // Persistence methods - wrapper around base class private methods
    private void SaveInventory()
    {
        if (!enablePersistence || inventoryData == null) return;
        
        try
        {
            // Use reflection to call the private SaveInventoryToPlayerPrefs method
            var method = GetType().BaseType.GetMethod("SaveInventoryToPlayerPrefs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(this, null);
                Debug.Log("[InventoryManagerV3] Inventory saved to PlayerPrefs");
            }
            else
            {
                Debug.LogWarning("[InventoryManagerV3] Could not find SaveInventoryToPlayerPrefs method via reflection");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManagerV3] Failed to save inventory: {e.Message}");
        }
    }
    
    private void LoadInventory()
    {
        if (!enablePersistence || inventoryData == null) return;
        
        try
        {
            // Use reflection to call the private LoadInventoryFromPlayerPrefs method
            var method = GetType().BaseType.GetMethod("LoadInventoryFromPlayerPrefs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(this, null);
                Debug.Log("[InventoryManagerV3] Inventory loaded from PlayerPrefs");
            }
            else
            {
                Debug.LogWarning("[InventoryManagerV3] Could not find LoadInventoryFromPlayerPrefs method via reflection");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManagerV3] Failed to load inventory: {e.Message}");
        }
    }
}

public enum TabType
{
    AllCards,
    Materials
}

