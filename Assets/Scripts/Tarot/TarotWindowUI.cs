using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main manager for the tarot window UI.
/// Displays equipped tarot cards in a horizontal, overlapping layout.
/// </summary>
public class TarotWindowUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform container; // Container with HorizontalLayoutGroup
    public GameObject slotPrefab; // TarotCardSlot prefab
    public HorizontalLayoutGroup layoutGroup;
    
    [Header("Layout Settings")]
    public float cardWidth = 180f;
    public float cardHeight = 250f;
    public float overlapSpacing = -40f; // Negative for overlap
    
    [Header("Slots Display")]
    public TextMeshProUGUI slotsText; // Optional: "Slots: X/4"
    
    private List<TarotCardSlot> cardSlots = new List<TarotCardSlot>();
    private TarotCardSlot selectedSlot;
    private TarotCardSlot hoveredSlot;
    private Dictionary<int, TarotCardSlot> slotIndexMap = new Dictionary<int, TarotCardSlot>(); // Maps equipment slot index to UI slot
    private const int MAX_SLOTS = 4;
    
    private void Awake()
    {
        // Find container if not assigned
        if (container == null)
        {
            container = transform;
        }
        
        // Add RectMask2D to clip cards that go outside container
        RectMask2D mask = container.GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = container.gameObject.AddComponent<RectMask2D>();
        }
        
        // Find or add HorizontalLayoutGroup
        if (layoutGroup == null)
        {
            layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
        }
        
        // Configure layout group
        ConfigureLayoutGroup();
    }
    
    private void Start()
    {
        // Sync with inventory (will create slots for equipped cards)
        SyncWithInventory();
        
        // Subscribe to inventory changes if possible
        if (InventoryManagerV3.Instance != null)
        {
            // We'll poll for changes in Update or use a coroutine
            StartCoroutine(PollInventoryChanges());
        }
    }
    
    /// <summary>
    /// Configure the horizontal layout group for overlapping cards
    /// </summary>
    private void ConfigureLayoutGroup()
    {
        if (layoutGroup == null) return;
        
        // Let layout group do its job - minimal configuration
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = overlapSpacing; // Negative for overlap
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.enabled = true;
    }
    
    /// <summary>
    /// Create a slot for a specific equipment slot index (only if card is equipped)
    /// </summary>
    private void CreateSlotForIndex(int equipmentSlotIndex, TarotCardData cardData)
    {
        // Check if slot already exists for this index
        if (slotIndexMap.ContainsKey(equipmentSlotIndex) && slotIndexMap[equipmentSlotIndex] != null)
        {
            // Slot already exists, just update it
            slotIndexMap[equipmentSlotIndex].Initialize(cardData, equipmentSlotIndex, this);
            return;
        }
        
        // Create new slot
        CreateSlot(equipmentSlotIndex, cardData);
        
        // Sort slots by index to maintain proper order in hierarchy
        SortSlotsByIndex();
        
        // Force layout rebuild to position new slot
        StartCoroutine(ForceLayoutRebuildAfterFrame());
    }
    
    /// <summary>
    /// Sort slots in hierarchy by their slot index (so layout group positions them correctly)
    /// </summary>
    private void SortSlotsByIndex()
    {
        // Remove null entries
        cardSlots.RemoveAll(s => s == null);
        
        // Sort the list by slot index
        cardSlots.Sort((a, b) => {
            if (a == null || b == null) return 0;
            return a.slotIndex.CompareTo(b.slotIndex);
        });
        
        // Reorder in hierarchy - this ensures HorizontalLayoutGroup positions them in the right order
        // Only reorder non-selected cards (selected card stays at front)
        int siblingIndex = 0;
        foreach (var slot in cardSlots)
        {
            if (slot != null && !slot.IsSelected())
            {
                slot.transform.SetSiblingIndex(siblingIndex);
                slot.SetOriginalSiblingIndex(siblingIndex);
                siblingIndex++;
            }
        }
        
        // Selected card stays at the end (front)
        if (selectedSlot != null)
        {
            selectedSlot.transform.SetAsLastSibling();
        }
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Remove slot for a specific equipment slot index
    /// </summary>
    private void RemoveSlotForIndex(int equipmentSlotIndex)
    {
        if (slotIndexMap.ContainsKey(equipmentSlotIndex))
        {
            TarotCardSlot slot = slotIndexMap[equipmentSlotIndex];
            
            // Clear selection/hover if this was selected/hovered
            if (selectedSlot == slot)
            {
                selectedSlot = null;
            }
            if (hoveredSlot == slot)
            {
                hoveredSlot = null;
            }
            
            // Remove from lists
            cardSlots.Remove(slot);
            slotIndexMap.Remove(equipmentSlotIndex);
            
            // Destroy the slot
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Create a single slot for a specific equipment slot index
    /// </summary>
    private void CreateSlot(int equipmentSlotIndex, TarotCardData cardData)
    {
        if (slotPrefab == null)
        {
            Debug.LogError("[TarotWindowUI] Slot prefab not assigned!");
            return;
        }
        
        GameObject slotObj = Instantiate(slotPrefab, container);
        slotObj.name = $"TarotCardSlot_{equipmentSlotIndex}";
        
        TarotCardSlot slot = slotObj.GetComponent<TarotCardSlot>();
        if (slot == null)
        {
            Debug.LogError($"[TarotWindowUI] Slot prefab missing TarotCardSlot component!");
            Destroy(slotObj);
            return;
        }
        
        // Set slot size
        RectTransform slotRect = slotObj.GetComponent<RectTransform>();
        if (slotRect != null)
        {
            slotRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        }
        
        // Initialize slot with card data
        slot.Initialize(cardData, equipmentSlotIndex, this);
        
        // Add to lists
        cardSlots.Add(slot);
        slotIndexMap[equipmentSlotIndex] = slot;
        
        // KEEP layout group enabled - it handles positioning
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
        }
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Clear all slots
    /// </summary>
    private void ClearSlots()
    {
        foreach (var slot in cardSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        cardSlots.Clear();
        slotIndexMap.Clear();
        selectedSlot = null;
        hoveredSlot = null;
    }
    
    /// <summary>
    /// Sync slots with inventory equipment slots - create empty placeholder slots for all 4 positions
    /// </summary>
    public void SyncWithInventory()
    {
        if (InventoryManagerV3.Instance == null || InventoryManagerV3.Instance.inventoryData == null)
        {
            Debug.LogWarning("[TarotWindowUI] InventoryManagerV3 or inventoryData not found!");
            return;
        }
        
        var inventoryData = InventoryManagerV3.Instance.inventoryData;
        
        // Create/update slots for ALL 4 equipment slots (even if empty)
        // This ensures cards stay in their fixed grid positions
        for (int i = 0; i < MAX_SLOTS && i < inventoryData.equipmentSlots.Count; i++)
        {
            var equipmentSlot = inventoryData.equipmentSlots[i];
            
            // Check if slot is actually occupied (both flag AND card exists)
            bool actuallyOccupied = equipmentSlot.isOccupied && equipmentSlot.storedCard != null;
            
            if (actuallyOccupied)
            {
                // Create or update slot with card
                CreateSlotForIndex(i, equipmentSlot.storedCard);
            }
            else
            {
                // Create empty placeholder slot to maintain grid position
                if (!slotIndexMap.ContainsKey(i) || slotIndexMap[i] == null)
                {
                    CreateEmptySlotForIndex(i);
                }
                else
                {
                    // Slot exists but is now empty - clear it
                    if (slotIndexMap[i].cardData != null)
                    {
                        slotIndexMap[i].Initialize(null, i, this);
                    }
                }
            }
        }
        
        // Remove any slots beyond MAX_SLOTS
        List<int> slotsToRemove = new List<int>();
        foreach (var kvp in slotIndexMap)
        {
            if (kvp.Key >= MAX_SLOTS || kvp.Value == null)
            {
                slotsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int slotIndex in slotsToRemove)
        {
            RemoveSlotForIndex(slotIndex);
        }
        
        // Force layout rebuild after changes
        StartCoroutine(ForceLayoutRebuildAfterFrame());
        
        UpdateSlotsText();
    }
    
    /// <summary>
    /// Create an empty placeholder slot for a specific index (to maintain grid position)
    /// </summary>
    private void CreateEmptySlotForIndex(int equipmentSlotIndex)
    {
        // Check if slot already exists
        if (slotIndexMap.ContainsKey(equipmentSlotIndex) && slotIndexMap[equipmentSlotIndex] != null)
        {
            return; // Already exists
        }
        
        // Create new empty slot
        CreateSlot(equipmentSlotIndex, null);
        
        // Sort slots by index to maintain proper order in hierarchy
        SortSlotsByIndex();
        
        // Force layout rebuild to position new slot
        StartCoroutine(ForceLayoutRebuildAfterFrame());
    }
    
    /// <summary>
    /// Force layout rebuild after a frame delay to ensure positions are correct
    /// </summary>
    private IEnumerator ForceLayoutRebuildAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        
        // Ensure layout group is enabled
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
        }
        
        // Sort slots by index to ensure correct order
        SortSlotsByIndex();
        
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Update the slots text display
    /// </summary>
    private void UpdateSlotsText()
    {
        if (slotsText == null) return;
        
        int occupiedSlots = 0;
        
        // Count from inventory equipment slots (source of truth)
        if (InventoryManagerV3.Instance != null && InventoryManagerV3.Instance.inventoryData != null)
        {
            foreach (var equipmentSlot in InventoryManagerV3.Instance.inventoryData.equipmentSlots)
            {
                if (equipmentSlot.isOccupied && equipmentSlot.storedCard != null)
                {
                    occupiedSlots++;
                }
            }
        }
        
        slotsText.text = $"Slots: {occupiedSlots}/{MAX_SLOTS}";
    }
    
    /// <summary>
    /// Handle card click - bring to front or deselect
    /// </summary>
    public void OnCardClicked(TarotCardSlot slot)
    {
        if (slot == null || slot.cardData == null) return;
        
        // If clicking the same card again, deselect it
        if (selectedSlot == slot && slot.IsSelected())
        {
            DeselectCard(slot);
            return;
        }
        
        // Deselect previous card if any
        if (selectedSlot != null && selectedSlot != slot)
        {
            DeselectCard(selectedSlot);
        }
        
        // Select new card
        selectedSlot = slot;
        selectedSlot.SetSelected(true);
    }
    
    /// <summary>
    /// Handle card hover
    /// </summary>
    public void OnCardHovered(TarotCardSlot slot)
    {
        hoveredSlot = slot;
    }
    
    /// <summary>
    /// Handle card unhover
    /// </summary>
    public void OnCardUnhovered(TarotCardSlot slot)
    {
        if (hoveredSlot == slot)
        {
            hoveredSlot = null;
        }
    }
    
    /// <summary>
    /// Deselect a card
    /// </summary>
    private void DeselectCard(TarotCardSlot slot)
    {
        if (slot == null) return;
        
        slot.SetSelected(false);
        if (selectedSlot == slot)
        {
            selectedSlot = null;
        }
        RestoreSlotOrder(slot);
    }
    
    /// <summary>
    /// Get the currently selected slot
    /// </summary>
    public TarotCardSlot GetSelectedSlot()
    {
        return selectedSlot;
    }
    
    /// <summary>
    /// Get the currently hovered slot
    /// </summary>
    public TarotCardSlot GetHoveredSlot()
    {
        return hoveredSlot;
    }
    
    /// <summary>
    /// Restore a slot to its correct position in the layout order based on slotIndex
    /// </summary>
    public void RestoreSlotOrder(TarotCardSlot slot)
    {
        if (slot == null) return;
        
        // Sort slots by index to ensure correct order
        SortSlotsByIndex();
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Called when a card is used
    /// </summary>
    public void OnCardUsed(TarotCardSlot slot)
    {
        if (slot == null) return;
        
        // Update the slot display
        slot.UpdateDisplay();
        
        // Sync with inventory to get updated card data
        SyncWithInventory();
        
        // Update slots text
        UpdateSlotsText();
    }
    
    /// <summary>
    /// Poll inventory for changes (fallback if events aren't available)
    /// </summary>
    private IEnumerator PollInventoryChanges()
    {
        var lastEquipmentState = new List<TarotCardData>();
        
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Poll every 0.5 seconds
            
            if (InventoryManagerV3.Instance == null || InventoryManagerV3.Instance.inventoryData == null)
            {
                continue;
            }
            
            // Check if equipment slots have changed
            var currentEquipmentState = new List<TarotCardData>();
            foreach (var slot in InventoryManagerV3.Instance.inventoryData.equipmentSlots)
            {
                currentEquipmentState.Add(slot.isOccupied ? slot.storedCard : null);
            }
            
            // Compare states
            bool hasChanged = false;
            if (lastEquipmentState.Count != currentEquipmentState.Count)
            {
                hasChanged = true;
            }
            else
            {
                for (int i = 0; i < currentEquipmentState.Count; i++)
                {
                    if (lastEquipmentState[i] != currentEquipmentState[i])
                    {
                        hasChanged = true;
                        break;
                    }
                }
            }
            
            if (hasChanged)
            {
                SyncWithInventory();
                lastEquipmentState = new List<TarotCardData>(currentEquipmentState);
            }
        }
    }
    
    /// <summary>
    /// Public method to force refresh (can be called from InventoryManagerV3)
    /// </summary>
    public void Refresh()
    {
        SyncWithInventory();
    }
    
    private void OnDestroy()
    {
        // Clean up
        StopAllCoroutines();
    }
}

