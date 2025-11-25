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
        // Base class Awake will run automatically first
        // Just set up V3-specific instance reference
        
        // Only set V3 instance if base class instance was set (meaning this is the singleton)
        if (InventoryManager.Instance == this)
        {
            Instance = this;
            Debug.Log("InventoryManagerV3 initialized with " + (inventoryData != null ? inventoryData.storageSlotCount.ToString() : "no data") + " storage slots");
        }
    }
    
    private void Start()
    {
        // Find and initialize V3 UI panel if not assigned
        if (inventoryPanelV3 == null)
        {
            inventoryPanelV3 = FindObjectOfType<InventoryPanelUIV3>();
        }
        
        if (inventoryPanelV3 != null)
        {
            inventoryPanelV3.Initialize();
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
        bool success = base.AddPurchasedCard(card);
        
        if (success && inventoryPanelV3 != null)
        {
            inventoryPanelV3.RefreshAllSlots();
            inventoryPanelV3.UpdateOverviewStats();
        }
        
        return success;
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
        if (selectedSlot == null || selectedSlot.slotData == null || !selectedSlot.slotData.isOccupied)
        {
            return false;
        }
        
        // Can only equip from storage slots
        if (selectedSlot.slotData.isEquipmentSlot)
        {
            return false;
        }
        
        return EquipCardFromStorage(selectedSlot.slotIndex);
    }
    
    public bool UnequipSelectedCard()
    {
        if (selectedSlot == null || selectedSlot.slotData == null || !selectedSlot.slotData.isOccupied)
        {
            return false;
        }
        
        // Can only unequip from equipment slots
        if (!selectedSlot.slotData.isEquipmentSlot)
        {
            return false;
        }
        
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
}

public enum TabType
{
    AllCards,
    Materials
}

