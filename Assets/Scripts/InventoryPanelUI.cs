using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class InventoryPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform storageSlotContainer;
    public Transform equipmentSlotContainer;
    public GameObject slotPrefab;
    public Button closeButton;
    
    [Header("Info Display")]
    public TextMeshProUGUI inventoryStatsText;
    public TextMeshProUGUI selectedCardInfoText;
    
    [Header("Action Buttons")]
    public Button equipButton;
    public Button unequipButton;
    public Button discardButton;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    
    private List<InventorySlotUI> storageSlots = new List<InventorySlotUI>();
    private List<InventorySlotUI> equipmentSlots = new List<InventorySlotUI>();
    private InventorySlotUI selectedSlot;
    private bool isVisible = false;
    
    private void Start()
    {
        // Setup button listeners
        if (closeButton != null)
            closeButton.onClick.AddListener(HideInventory);
        
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        
        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);
        
        if (discardButton != null)
            discardButton.onClick.AddListener(OnDiscardButtonClicked);
        
        // Initialize inventory
        InitializeInventorySlots();
        
        // Hide panel initially
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        
        // Subscribe to inventory events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnCardAdded += OnCardAddedToInventory;
            InventoryManager.Instance.OnCardRemoved += OnCardRemovedFromInventory;
            InventoryManager.Instance.OnCardEquippedChanged += OnCardEquippedChanged;
        }
    }
    
    private void InitializeInventorySlots()
    {
        if (InventoryManager.Instance == null || InventoryManager.Instance.inventoryData == null)
        {
            Debug.LogWarning("InventoryManager or InventoryData not found!");
            return;
        }
        
        var inventoryData = InventoryManager.Instance.inventoryData;
        
        // Clear existing slots
        ClearSlots();
        
        // Create storage slots
        for (int i = 0; i < inventoryData.storageSlotCount; i++)
        {
            CreateStorageSlot(i);
        }
        
        // Create equipment slots
        for (int i = 0; i < inventoryData.equipmentSlotCount; i++)
        {
            CreateEquipmentSlot(i);
        }
        
        RefreshAllSlots();
    }
    
    private void CreateStorageSlot(int index)
    {
        if (slotPrefab == null || storageSlotContainer == null) return;
        
        GameObject slotObj = Instantiate(slotPrefab, storageSlotContainer);
        InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
        
        if (slotUI != null)
        {
            var slotData = InventoryManager.Instance.GetStorageSlot(index);
            slotUI.Initialize(index, false, slotData, this);
            storageSlots.Add(slotUI);
        }
    }
    
    private void CreateEquipmentSlot(int index)
    {
        if (slotPrefab == null || equipmentSlotContainer == null) return;
        
        GameObject slotObj = Instantiate(slotPrefab, equipmentSlotContainer);
        InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
        
        if (slotUI != null)
        {
            var slotData = InventoryManager.Instance.GetEquipmentSlot(index);
            slotUI.Initialize(index, true, slotData, this);
            equipmentSlots.Add(slotUI);
        }
    }
    
    private void ClearSlots()
    {
        // Clear storage slots
        foreach (var slot in storageSlots)
        {
            if (slot != null && slot.gameObject != null)
                DestroyImmediate(slot.gameObject);
        }
        storageSlots.Clear();
        
        // Clear equipment slots
        foreach (var slot in equipmentSlots)
        {
            if (slot != null && slot.gameObject != null)
                DestroyImmediate(slot.gameObject);
        }
        equipmentSlots.Clear();
    }
    
    public void ShowInventory()
    {
        if (isVisible) return;
        
        isVisible = true;
        
        // Show panel
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            
            // Animate panel appearance
            RectTransform panelRect = inventoryPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.zero;
                panelRect.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
            }
        }
        
        // Force refresh the inventory display with debug logging
        Debug.Log("🔄 ShowInventory: Refreshing inventory display...");
        ForceRefreshInventoryDisplay();
    }
    
    public void HideInventory()
    {
        if (!isVisible) return;
        
        isVisible = false;
        
        // Animate panel disappearance
        if (inventoryPanel != null)
        {
            RectTransform panelRect = inventoryPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
                    .OnComplete(() => inventoryPanel.SetActive(false));
            }
            else
            {
                inventoryPanel.SetActive(false);
            }
        }
        
        // Clear selection
        SetSelectedSlot(null);
    }
    
    public void ToggleInventory()
    {
        if (isVisible)
            HideInventory();
        else
            ShowInventory();
    }
    
    private void RefreshInventoryDisplay()
    {
        RefreshAllSlots();
        UpdateInventoryStats();
        UpdateActionButtons();
    }
    
    public void ForceRefreshInventoryDisplay()
    {
        Debug.Log("🔄 Force refreshing inventory display...");
        
        // First check if InventoryManager is available
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("❌ InventoryManager.Instance is null during force refresh!");
            return;
        }
        
        if (InventoryManager.Instance.inventoryData == null)
        {
            Debug.LogError("❌ InventoryManager.inventoryData is null during force refresh!");
            return;
        }
        
        // Log current data state
        var stats = InventoryManager.Instance.GetInventoryStats();
        Debug.Log($"📊 Before refresh - Storage: {stats.storageUsed}/{stats.storageTotal}, Equipment: {stats.equipmentUsed}/{stats.equipmentTotal}");
        
        // Reinitialize slots if needed
        if (storageSlots.Count == 0 || equipmentSlots.Count == 0)
        {
            Debug.Log("🔄 No slots found, reinitializing...");
            InitializeInventorySlots();
        }
        
        // Force refresh all slots
        RefreshAllSlots();
        UpdateInventoryStats();
        UpdateActionButtons();
        
        Debug.Log($"✅ Force refresh complete - {storageSlots.Count} storage slots, {equipmentSlots.Count} equipment slots");
    }
    
    private void RefreshAllSlots()
    {
        Debug.Log($"🔄 RefreshAllSlots: {storageSlots.Count} storage slots, {equipmentSlots.Count} equipment slots");
        
        // Refresh storage slots
        for (int i = 0; i < storageSlots.Count; i++)
        {
            var slotData = InventoryManager.Instance.GetStorageSlot(i);
            storageSlots[i].slotData = slotData;
            storageSlots[i].UpdateSlotDisplay();
            
            if (slotData?.isOccupied == true && slotData.storedCard != null)
            {
                Debug.Log($"   📦 Storage[{i}]: {slotData.storedCard.cardName} - UI should show this card");
            }
        }
        
        // Refresh equipment slots
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            var slotData = InventoryManager.Instance.GetEquipmentSlot(i);
            equipmentSlots[i].slotData = slotData;
            equipmentSlots[i].UpdateSlotDisplay();
            
            if (slotData?.isOccupied == true && slotData.storedCard != null)
            {
                Debug.Log($"   ⚔️ Equipment[{i}]: {slotData.storedCard.cardName} - UI should show this card");
            }
        }
    }
    
    private void UpdateInventoryStats()
    {
        if (inventoryStatsText == null || InventoryManager.Instance == null) return;
        
        var stats = InventoryManager.Instance.GetInventoryStats();
        
        string statsText = $"Storage: {stats.storageUsed}/{stats.storageTotal}\n";
        statsText += $"Equipment: {stats.equipmentUsed}/{stats.equipmentTotal}\n";
        statsText += $"Usable Cards: {stats.usableCards}\n";
        statsText += $"Depleted Cards: {stats.unusableCards}";
        
        inventoryStatsText.text = statsText;
    }
    
    private void UpdateActionButtons()
    {
        bool hasSelection = selectedSlot != null && selectedSlot.slotData != null && selectedSlot.slotData.isOccupied;
        bool isEquipmentSelected = hasSelection && selectedSlot.isEquipmentSlot;
        bool isStorageSelected = hasSelection && !selectedSlot.isEquipmentSlot;
        bool canEquip = isStorageSelected && selectedSlot.slotData.storedCard.CanBeUsed() && 
                       InventoryManager.Instance.HasEquipmentSpace();
        
        if (equipButton != null)
            equipButton.interactable = canEquip;
        
        if (unequipButton != null)
            unequipButton.interactable = isEquipmentSelected && InventoryManager.Instance.HasStorageSpace();
        
        if (discardButton != null)
            discardButton.interactable = hasSelection;
        
        // Update selected card info
        if (selectedCardInfoText != null)
        {
            if (hasSelection)
            {
                var card = selectedSlot.slotData.storedCard;
                string cardInfo = $"<b>{card.cardName}</b>\n";
                cardInfo += $"<i>{card.description}</i>\n\n";
                cardInfo += $"Material: {card.GetMaterialDisplayName()}\n";
                
                int remainingUses = card.GetRemainingUses();
                if (remainingUses == -1)
                {
                    cardInfo += "Uses: Unlimited\n";
                }
                else
                {
                    cardInfo += $"Uses: {remainingUses}/{card.maxUses}\n";
                }
                
                cardInfo += $"Status: {(selectedSlot.isEquipmentSlot ? "Equipped" : "In Storage")}";
                
                selectedCardInfoText.text = cardInfo;
            }
            else
            {
                selectedCardInfoText.text = "Select a card to view details";
            }
        }
    }
    
    public void OnSlotClicked(InventorySlotUI slot)
    {
        SetSelectedSlot(slot);
    }
    
    public void OnSlotRightClicked(InventorySlotUI slot)
    {
        // Quick action on right click
        if (slot == null || slot.slotData == null || !slot.slotData.isOccupied) return;
        
        if (slot.isEquipmentSlot)
        {
            // Unequip card
            InventoryManager.Instance.UnequipCard(slot.slotIndex);
        }
        else if (slot.slotData.storedCard.CanBeUsed() && InventoryManager.Instance.HasEquipmentSpace())
        {
            // Equip card
            InventoryManager.Instance.EquipCardFromStorage(slot.slotIndex);
        }
        
        RefreshInventoryDisplay();
    }
    
    private void SetSelectedSlot(InventorySlotUI slot)
    {
        // Clear previous selection
        if (selectedSlot != null)
        {
            selectedSlot.ResetVisualState();
        }
        
        selectedSlot = slot;
        
        // Highlight new selection
        if (selectedSlot != null && selectedSlot.slotData != null && selectedSlot.slotData.isOccupied)
        {
            // Add selection visual feedback here if needed
            selectedSlot.transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad);
        }
        
        UpdateActionButtons();
    }
    
    private void OnEquipButtonClicked()
    {
        if (selectedSlot == null || selectedSlot.isEquipmentSlot) return;
        
        bool success = InventoryManager.Instance.EquipCardFromStorage(selectedSlot.slotIndex);
        if (success)
        {
            RefreshInventoryDisplay();
            SetSelectedSlot(null); // Clear selection after action
        }
    }
    
    private void OnUnequipButtonClicked()
    {
        if (selectedSlot == null || !selectedSlot.isEquipmentSlot) return;
        
        bool success = InventoryManager.Instance.UnequipCard(selectedSlot.slotIndex);
        if (success)
        {
            // Sync with tarot panel to remove visual duplicate
            InventoryManager.Instance.SyncWithTarotPanel();
            RefreshInventoryDisplay();
            SetSelectedSlot(null); // Clear selection after action
        }
    }
    
    private void OnDiscardButtonClicked()
    {
        if (selectedSlot == null || selectedSlot.slotData == null || !selectedSlot.slotData.isOccupied) return;
        
        // Confirm discard (you might want to add a confirmation dialog here)
        var card = selectedSlot.slotData.storedCard;
        InventoryManager.Instance.RemoveUsedUpCard(card);
        
        RefreshInventoryDisplay();
        SetSelectedSlot(null);
    }
    
    // Event handlers for inventory changes
    private void OnCardAddedToInventory(TarotCardData card)
    {
        if (isVisible)
        {
            RefreshInventoryDisplay();
        }
    }
    
    private void OnCardRemovedFromInventory(TarotCardData card)
    {
        if (isVisible)
        {
            RefreshInventoryDisplay();
        }
    }
    
    private void OnCardEquippedChanged(TarotCardData card, bool isEquipped)
    {
        if (isVisible)
        {
            RefreshInventoryDisplay();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnCardAdded -= OnCardAddedToInventory;
            InventoryManager.Instance.OnCardRemoved -= OnCardRemovedFromInventory;
            InventoryManager.Instance.OnCardEquippedChanged -= OnCardEquippedChanged;
        }
    }
}
