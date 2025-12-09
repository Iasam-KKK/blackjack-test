using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    // Static registry to cache TarotCardData by type - survives scene loads
    private static Dictionary<TarotCardType, TarotCardData> cardDataRegistry = new Dictionary<TarotCardType, TarotCardData>();
    
    // Track cards that need sprite refresh after shop loads
    private static List<TarotCardData> cardsNeedingSpriteRefresh = new List<TarotCardData>();
    
    [Header("Inventory Configuration")]
    public InventoryData inventoryData; // Reference to the ScriptableObject
    
    [Header("UI Update Settings")]
    public bool enableImmediateUIUpdates = true; // Toggle for immediate equipment slot updates
    
    [Header("Persistence")]
    public bool enablePersistence = true;
    private const string INVENTORY_SAVE_KEY = "FinalInventoryData_v1";
    
    [Header("Old System Integration")]
    public ShopManager shopManager;
    
    [Header("Events")]
    public System.Action<TarotCardData> OnCardAdded;
    public System.Action<TarotCardData> OnCardRemoved;
    public System.Action<TarotCardData, bool> OnCardEquippedChanged; // card, isEquipped
    
    [Header("Tarot Panel Management")]
    private Dictionary<int, GameObject> tarotSlotObjects = new Dictionary<int, GameObject>();
    private Dictionary<int, TarotCard> tarotSlotCards = new Dictionary<int, TarotCard>();
    private bool tarotPanelInitialized = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeInventory()
    {
        if (inventoryData == null)
        {
            Debug.LogWarning("InventoryData not assigned to InventoryManager during Awake - will retry when assigned");
            return;
        }
        
        inventoryData.InitializeSlots();
        
        // Load saved inventory data
        if (enablePersistence)
        {
            LoadInventoryFromPlayerPrefs();
        }
        
        // Initialize tarot panel with pre-created slots on startup
        Invoke("InitializeTarotPanelSlots", 0.1f);
        
        // Find shop manager for old system integration
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
        }
        
        Debug.Log("Inventory initialized with " + inventoryData.storageSlotCount + " storage slots and " + 
                 inventoryData.equipmentSlotCount + " equipment slots");
    }
    
    // Public method to force initialization when data is assigned later
    public void ForceInitializeWithData(InventoryData data)
    {
        Debug.Log("🔄 ForceInitializeWithData called");
        inventoryData = data;
        enablePersistence = true;
        
        if (inventoryData != null)
        {
            inventoryData.InitializeSlots();
            
            // Load saved inventory data
            if (enablePersistence)
            {
                LoadInventoryFromPlayerPrefs();
            }
            
            // Find shop manager for old system integration
            if (shopManager == null)
            {
                shopManager = FindObjectOfType<ShopManager>();
                if (shopManager != null)
                {
                    Debug.Log("✅ Found and assigned ShopManager reference");
                }
            }
            
            // Initialize tarot panel with pre-created slots
            Invoke("InitializeTarotPanelSlots", 0.1f);
            
            Debug.Log("✅ Inventory force-initialized with " + inventoryData.storageSlotCount + " storage slots and " + 
                     inventoryData.equipmentSlotCount + " equipment slots");
        }
    }
    
    // Add a purchased card to inventory
    public virtual bool AddPurchasedCard(TarotCardData card)
    {
        if (card == null) return false;
        
        // Register the card data for future lookups (in case we need to reload from save)
        if (card.cardImage != null)
        {
            RegisterCardData(card);
        }
        
        bool success = inventoryData.AddCardToStorage(card);
        if (success)
        {
            Debug.Log($"Added {card.cardName} to inventory storage");
            OnCardAdded?.Invoke(card);
            
            // Also update PlayerStats for compatibility
            if (PlayerStats.instance != null && !PlayerStats.instance.ownedCards.Contains(card))
            {
                PlayerStats.instance.ownedCards.Add(card);
            }
            
            // Save to PlayerPrefs
            if (enablePersistence)
            {
                SaveInventoryToPlayerPrefs();
            }
        }
        else
        {
            Debug.LogWarning($"Failed to add {card.cardName} to inventory - no empty storage slots");
        }
        
        return success;
    }
    
    // Equip a card from storage to equipment slot
    public virtual bool EquipCardFromStorage(int storageSlotIndex, int equipmentSlotIndex = -1)
    {
        if (inventoryData == null) return false;
        
        // If no specific equipment slot provided, find the first available one
        if (equipmentSlotIndex == -1)
        {
            for (int i = 0; i < inventoryData.equipmentSlots.Count; i++)
            {
                var slot = inventoryData.equipmentSlots[i];
                // Check both isOccupied flag AND if storedCard is actually null
                bool actuallyOccupied = slot.isOccupied && slot.storedCard != null;
                if (!actuallyOccupied)
                {
                    equipmentSlotIndex = i;
                    Debug.Log($"[InventoryManager] EquipCardFromStorage: Found available equipment slot {i} (isOccupied={slot.isOccupied}, card={slot.storedCard != null})");
                    break;
                }
            }
        }
        
        if (equipmentSlotIndex == -1)
        {
            Debug.LogWarning($"[InventoryManager] EquipCardFromStorage: No available equipment slots found. Checking all slots:");
            for (int i = 0; i < inventoryData.equipmentSlots.Count; i++)
            {
                var slot = inventoryData.equipmentSlots[i];
                Debug.LogWarning($"[InventoryManager] Equipment slot {i}: isOccupied={slot.isOccupied}, card={(slot.storedCard != null ? slot.storedCard.cardName : "null")}");
            }
            return false; // No available equipment slots
        }
        
        bool success = inventoryData.MoveCardToEquipment(storageSlotIndex, equipmentSlotIndex);
        if (success)
        {
            var card = inventoryData.equipmentSlots[equipmentSlotIndex].storedCard;
            Debug.Log($"Equipped {card.cardName} to slot {equipmentSlotIndex}");
            
            // IMMEDIATELY force update the equipment slot UI (if enabled)
            if (enableImmediateUIUpdates)
            {
                ForceUpdateEquipmentSlotUI(equipmentSlotIndex, card);
            }
            
            OnCardEquippedChanged?.Invoke(card, true);
            
            Debug.Log($"[InventoryManager] About to update tarot slot {equipmentSlotIndex} with {card.cardName}");
            Debug.Log($"[InventoryManager] Tarot panel initialized: {tarotPanelInitialized}");
            
            // Update specific tarot slot with new slot-based system
            UpdateTarotSlot(equipmentSlotIndex, card);
            
            // Save to PlayerPrefs
            if (enablePersistence)
            {
                SaveInventoryToPlayerPrefs();
            }
        }
        
        return success;
    }
    
    // Unequip a card from equipment slot back to storage
    public virtual bool UnequipCard(int equipmentSlotIndex)
    {
        if (inventoryData == null) return false;
        
        var equipmentSlot = inventoryData.equipmentSlots[equipmentSlotIndex];
        if (!equipmentSlot.isOccupied) return false;
        
        var card = equipmentSlot.storedCard;
        
        bool success = inventoryData.UnequipCard(equipmentSlotIndex);
        if (success)
        {
            Debug.Log($"Unequipped {card.cardName} from slot {equipmentSlotIndex}");
            
            // IMMEDIATELY force update the equipment slot UI to show it's empty (if enabled)
            if (enableImmediateUIUpdates)
            {
                ForceUpdateEquipmentSlotUI(equipmentSlotIndex, null);
            }
            
            OnCardEquippedChanged?.Invoke(card, false);
            
            // Hide the card in the specific tarot slot
            ShowTarotSlot(equipmentSlotIndex, false);
            
            // Save to PlayerPrefs
            if (enablePersistence)
            {
                SaveInventoryToPlayerPrefs();
            }
        }
        
        return success;
    }
    
    // Remove a card that has been used up
    public virtual void RemoveUsedUpCard(TarotCardData card)
    {
        if (inventoryData == null || card == null) return;
        
        // Find which equipment slot this card was in
        int slotIndex = -1;
        for (int i = 0; i < inventoryData.equipmentSlots.Count; i++)
        {
            var slot = inventoryData.equipmentSlots[i];
            if (slot.isOccupied && slot.storedCard == card)
            {
                slotIndex = i;
                break;
            }
        }
        
        bool removed = inventoryData.RemoveCard(card);
        if (removed)
        {
            Debug.Log($"Removed used up card {card.cardName} from inventory");
            OnCardRemoved?.Invoke(card);
            
            // Hide from tarot panel if it was equipped
            if (slotIndex >= 0)
            {
                ShowTarotSlot(slotIndex, false);
            }
            
            // Also remove from PlayerStats for compatibility
            if (PlayerStats.instance != null && PlayerStats.instance.ownedCards.Contains(card))
            {
                PlayerStats.instance.ownedCards.Remove(card);
            }
            
            // Save to PlayerPrefs
            if (enablePersistence)
            {
                SaveInventoryToPlayerPrefs();
            }
        }
    }
    
    // Get all equipped cards that can be used in the current game
    public List<TarotCardData> GetEquippedUsableCards()
    {
        if (inventoryData == null) return new List<TarotCardData>();
        return inventoryData.GetEquippedUsableCards();
    }
    
    // Get all cards in inventory
    public List<TarotCardData> GetAllInventoryCards()
    {
        if (inventoryData == null) return new List<TarotCardData>();
        return inventoryData.GetAllCards();
    }
    
    // Get inventory statistics for UI display
    public InventoryStats GetInventoryStats()
    {
        if (inventoryData == null) return new InventoryStats();
        return inventoryData.GetInventoryStats();
    }
    
    // Check if inventory has space for a new card
    public bool HasStorageSpace()
    {
        if (inventoryData == null) return false;
        
        foreach (var slot in inventoryData.storageSlots)
        {
            if (!slot.isOccupied) return true;
        }
        return false;
    }
    
    // Check if there are available equipment slots
    public bool HasEquipmentSpace()
    {
        if (inventoryData == null)
        {
            Debug.LogWarning("[InventoryManager] HasEquipmentSpace: inventoryData is null");
            return false;
        }
        
        if (inventoryData.equipmentSlots == null || inventoryData.equipmentSlots.Count == 0)
        {
            Debug.LogWarning("[InventoryManager] HasEquipmentSpace: equipmentSlots is null or empty");
            return false;
        }
        
        int emptySlots = 0;
        int occupiedSlots = 0;
        
        foreach (var slot in inventoryData.equipmentSlots)
        {
            if (slot == null)
            {
                Debug.LogWarning("[InventoryManager] HasEquipmentSpace: Found null slot in equipmentSlots");
                continue;
            }
            
            // FIX: Fix any data inconsistencies first
            slot.FixSlotState();
            
            // Check both isOccupied flag AND if storedCard is actually null
            bool actuallyOccupied = slot.isOccupied && slot.storedCard != null;
            
            if (actuallyOccupied)
            {
                occupiedSlots++;
            }
            else
            {
                emptySlots++;
            }
        }
        
        Debug.Log($"[InventoryManager] HasEquipmentSpace: {emptySlots} empty, {occupiedSlots} occupied out of {inventoryData.equipmentSlots.Count} total");
        
        return emptySlots > 0;
    }
    
    // Get storage slot data for UI
    public InventorySlotData GetStorageSlot(int index)
    {
        if (inventoryData == null || index < 0 || index >= inventoryData.storageSlots.Count) 
            return null;
        return inventoryData.storageSlots[index];
    }
    
    // Get equipment slot data for UI
    public InventorySlotData GetEquipmentSlot(int index)
    {
        if (inventoryData == null || index < 0 || index >= inventoryData.equipmentSlots.Count) 
            return null;
        return inventoryData.equipmentSlots[index];
    }
    
    // Sync inventory with existing PlayerStats (for migration)
    public void SyncWithPlayerStats()
    {
        if (PlayerStats.instance == null || inventoryData == null) return;
        
        // Clear current inventory
        foreach (var slot in inventoryData.storageSlots)
        {
            slot.RemoveCard();
        }
        foreach (var slot in inventoryData.equipmentSlots)
        {
            slot.RemoveCard();
        }
        
        // Add all owned cards to storage
        foreach (var card in PlayerStats.instance.ownedCards)
        {
            if (card != null && card.CanBeUsed())
            {
                inventoryData.AddCardToStorage(card);
            }
        }
        
        Debug.Log($"Synced {PlayerStats.instance.ownedCards.Count} cards from PlayerStats to Inventory");
    }
    
    // Clear saved inventory data (useful for testing)
    [ContextMenu("Clear Saved Inventory")]
    public void ClearSavedInventory()
    {
        PlayerPrefs.DeleteKey(INVENTORY_SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("Cleared saved inventory data");
    }
    
    // Force sync with tarot panel (useful for debugging)
    [ContextMenu("Force Sync With Tarot Panel")]
    public void ForceSyncWithTarotPanel()
    {
        Debug.Log("[InventoryManager] Force sync requested");
        
        // If not initialized, initialize first
        if (!tarotPanelInitialized)
        {
            Debug.Log("[InventoryManager] Tarot panel not initialized, initializing now");
            InitializeTarotPanelSlots();
        }
        else
        {
            RefreshAllTarotSlots();
            Debug.Log("[InventoryManager] Forced synchronization with tarot panel");
        }
    }
    
    // Force reinitialize tarot panel
    [ContextMenu("Force Reinitialize Tarot Panel")]
    public void ForceReinitializeTarotPanel()
    {
        Debug.Log("[InventoryManager] Force reinitialization requested");
        
        // Clear existing references
        tarotSlotObjects.Clear();
        tarotSlotCards.Clear();
        tarotPanelInitialized = false;
        
        // Reinitialize
        InitializeTarotPanelSlots();
    }
    
    // Force save inventory
    [ContextMenu("Force Save Inventory")]
    public void ForceSaveInventory()
    {
        SaveInventoryToPlayerPrefs();
    }
    
    [ContextMenu("Clear PlayerPrefs (Reset Save)")]
    public void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteKey(INVENTORY_SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("🗑️ PlayerPrefs cleared! Inventory save data deleted.");
    }
    
    // Central method to use an equipped card - ensures single source of truth
    public void UseEquippedCard(TarotCardData usedCard)
    {
        if (inventoryData == null || usedCard == null) return;
        
        // Find the matching card in equipment slots (the authoritative store)
        foreach (var slot in inventoryData.equipmentSlots)
        {
            if (slot.isOccupied && slot.storedCard != null &&
                slot.storedCard.cardType == usedCard.cardType &&
                slot.storedCard.cardName == usedCard.cardName)
            {
                // Update durability in the authoritative instance
                slot.storedCard.UseCard();
                
                // Sync UI card reference to point to authoritative instance
                if (shopManager != null && shopManager.tarotPanel != null)
                {
                    TarotCard[] allTarotCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
                    foreach (var uiCard in allTarotCards)
                    {
                        if (uiCard != null && !uiCard.isInShop && 
                            uiCard.cardData != null &&
                            uiCard.cardData.cardType == slot.storedCard.cardType &&
                            uiCard.cardData.cardName == slot.storedCard.cardName)
                        {
                            // Force UI card to reference the authoritative instance
                            uiCard.cardData = slot.storedCard;
                            uiCard.UpdateCardDisplay();
                            break;
                        }
                    }
                }
                
                // Save immediately
                SaveInventoryToPlayerPrefs();
                break;
            }
        }
    }
    
    [ContextMenu("Debug Tarot Panel State")]
    public void DebugTarotPanelState()
    {
        Debug.Log("=== TAROT PANEL STATE ===");
        Debug.Log($"Initialized: {tarotPanelInitialized}");
        
        foreach (var kvp in tarotSlotObjects)
        {
            var slotIndex = kvp.Key;
            var cardObject = kvp.Value;
            var card = tarotSlotCards.ContainsKey(slotIndex) ? tarotSlotCards[slotIndex] : null;
            
            Debug.Log($"Slot {slotIndex}:");
            Debug.Log($"  - GameObject: {(cardObject != null ? cardObject.name : "null")}");
            Debug.Log($"  - Active: {(cardObject != null ? cardObject.activeSelf.ToString() : "null")}");
            Debug.Log($"  - Card Data: {(card != null && card.cardData != null ? card.cardData.cardName : "null")}");
            
            // Check equipment slot
            if (inventoryData != null && slotIndex < inventoryData.equipmentSlots.Count)
            {
                var equipSlot = inventoryData.equipmentSlots[slotIndex];
                Debug.Log($"  - Equipment Slot: {(equipSlot.isOccupied ? equipSlot.storedCard.cardName : "empty")}");
            }
        }
    }
    
    [ContextMenu("Debug GetInventoryStats")]
    public void DebugGetInventoryStats()
    {
        if (inventoryData == null)
        {
            Debug.LogError("❌ inventoryData is null");
            return;
        }
        
        Debug.Log("🔍 DEBUG GetInventoryStats:");
        
        int storageUsed = 0;
        int equipmentUsed = 0;
        
        Debug.Log($"   Storage slots count: {inventoryData.storageSlots?.Count ?? 0}");
        if (inventoryData.storageSlots != null)
        {
            for (int i = 0; i < inventoryData.storageSlots.Count; i++)
            {
                var slot = inventoryData.storageSlots[i];
                bool occupied = slot != null && slot.isOccupied && slot.storedCard != null;
                if (occupied) storageUsed++;
                Debug.Log($"   Storage[{i}]: slot={slot != null}, isOccupied={slot?.isOccupied}, storedCard={slot?.storedCard?.cardName ?? "null"} -> Counts as occupied: {occupied}");
            }
        }
        
        Debug.Log($"   Equipment slots count: {inventoryData.equipmentSlots?.Count ?? 0}");
        if (inventoryData.equipmentSlots != null)
        {
            for (int i = 0; i < inventoryData.equipmentSlots.Count; i++)
            {
                var slot = inventoryData.equipmentSlots[i];
                bool occupied = slot != null && slot.isOccupied && slot.storedCard != null;
                if (occupied) equipmentUsed++;
                Debug.Log($"   Equipment[{i}]: slot={slot != null}, isOccupied={slot?.isOccupied}, storedCard={slot?.storedCard?.cardName ?? "null"} -> Counts as occupied: {occupied}");
            }
        }
        
        Debug.Log($"   CALCULATED: Storage {storageUsed}/{inventoryData.storageSlotCount}, Equipment {equipmentUsed}/{inventoryData.equipmentSlotCount}");
        
        var actualStats = GetInventoryStats();
        Debug.Log($"   ACTUAL STATS: Storage {actualStats.storageUsed}/{actualStats.storageTotal}, Equipment {actualStats.equipmentUsed}/{actualStats.equipmentTotal}");
    }
    
    /// <summary>
    /// Immediately force update a specific equipment slot UI, even when inventory is hidden
    /// </summary>
    private void ForceUpdateEquipmentSlotUI(int equipmentSlotIndex, TarotCardData card)
    {
        Debug.Log($"🔧 ForceUpdateEquipmentSlotUI: Forcing immediate update for equipment slot {equipmentSlotIndex} with card {card?.cardName ?? "null"}");
        
        // Find the inventory panel UI
        InventoryPanelUI inventoryUI = FindObjectOfType<InventoryPanelUI>();
        if (inventoryUI == null)
        {
            Debug.LogWarning("⚠️ Could not find InventoryPanelUI to force update - this is normal if inventory UI hasn't been initialized yet");
            return;
        }
        
        // Use the public method to force update the equipment slot
        try
        {
            inventoryUI.ForceUpdateEquipmentSlot(equipmentSlotIndex, card);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error during ForceUpdateEquipmentSlot: {e.Message}");
        }
        
        // Force canvas update to ensure immediate visual refresh
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"✅ Completed forced update for equipment slot {equipmentSlotIndex}");
    }
    
    // Sync equipped cards TO the tarot panel (populate it with equipped cards)
    public void SyncEquippedCardsToTarotPanel()
    {
        // Ensure shopManager is found if null
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
        }
        
        if (shopManager == null || shopManager.tarotPanel == null || inventoryData == null) 
        {
            Debug.LogWarning("Cannot sync to tarot panel - missing references");
            return;
        }
        
        // 🔧 FIX: ALWAYS clear ALL existing non-shop cards first (prevents ghost cards)
        TarotCard[] existingCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        foreach (var card in existingCards)
        {
            if (card != null && !card.isInShop)
            {
                Destroy(card.gameObject);
            }
        }
        
        // Get currently equipped cards
        List<TarotCardData> equippedCards = new List<TarotCardData>();
        foreach (var slot in inventoryData.equipmentSlots)
        {
            if (slot.isOccupied && slot.storedCard != null && slot.storedCard.CanBeUsed())
            {
                equippedCards.Add(slot.storedCard);
            }
        }
        
        // If no equipped cards, we're done (already cleared above)
        if (equippedCards.Count == 0)
        {
            return;
        }
        
        // Re-create all equipped cards in the tarot panel
        foreach (var equippedCard in equippedCards)
        {
            CreateTarotCardInPanel(equippedCard);
        }
    }
    
    private void CreateTarotCardInPanel(TarotCardData cardData)
    {
        // Ensure shopManager is found if null
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
        }
        
        if (shopManager == null || shopManager.tarotCardPrefab == null) 
        {
            Debug.LogWarning($"Cannot create tarot card {cardData?.cardName ?? "null"} - missing shopManager or prefab");
            return;
        }
        
        Transform emptySlot = shopManager.GetEmptyTarotSlot();
        if (emptySlot == null) 
        {
            Debug.LogWarning($"No empty tarot slots available for {cardData.cardName}");
            return;
        }
        
        GameObject cardObject = Instantiate(shopManager.tarotCardPrefab, emptySlot);
        TarotCard card = cardObject.GetComponent<TarotCard>();
        
        if (card != null)
        {
            card.cardData = cardData;
            card.isInShop = false;
            card.deck = shopManager.deck;
            card.transform.localPosition = Vector3.zero;
            card.transform.localScale = Vector3.one * 0.8f;
            
            // Set up the card's RectTransform
            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(100, 150);
            }
        }
        else
        {
            Debug.LogError($"❌ Failed to get TarotCard component from instantiated prefab for {cardData.cardName}");
            Destroy(cardObject);
        }
    }
    
    // NEW SLOT-BASED TAROT PANEL MANAGEMENT
    
    private void InitializeTarotPanelSlots()
    {
        if (!tarotPanelInitialized)
        {
            StartCoroutine(InitializeTarotPanelSlotsCoroutine());
        }
    }
    
    private IEnumerator InitializeTarotPanelSlotsCoroutine()
    {
        Debug.Log("[InventoryManager] Starting tarot panel initialization coroutine");
        
        // Wait for shopManager to be available
        while (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
            if (shopManager == null)
            {
                Debug.Log("[InventoryManager] Waiting for ShopManager...");
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        Debug.Log("[InventoryManager] ShopManager found");
        
        // Wait for tarot panel to be available
        while (shopManager.tarotPanel == null)
        {
            Debug.Log("[InventoryManager] Waiting for tarot panel...");
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("[InventoryManager] Tarot panel found");
        
        // Pre-create card objects in all 3 tarot slots
        for (int i = 0; i < 3; i++)
        {
            Transform slot = shopManager.GetTarotSlotByIndex(i);
            if (slot != null)
            {
                GameObject cardObject = Instantiate(shopManager.tarotCardPrefab, slot);
                TarotCard card = cardObject.GetComponent<TarotCard>();
                
                if (card != null)
                {
                    // Configure the card object
                    card.isInShop = false;
                    card.deck = shopManager.deck;
                    card.transform.localPosition = Vector3.zero;
                    card.transform.localScale = Vector3.one * 0.8f;
                    
                    // Set up the card's RectTransform
                    RectTransform cardRect = card.GetComponent<RectTransform>();
                    if (cardRect != null)
                    {
                        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                        cardRect.pivot = new Vector2(0.5f, 0.5f);
                        cardRect.sizeDelta = new Vector2(100, 150);
                    }
                    
                    // Store references
                    tarotSlotObjects[i] = cardObject;
                    tarotSlotCards[i] = card;
                    
                    // Initially hide the card
                    cardObject.SetActive(false);
                    
                    Debug.Log($"[InventoryManager] Pre-created tarot card object in slot {i}");
                }
            }
        }
        
        tarotPanelInitialized = true;
        Debug.Log($"[InventoryManager] Tarot panel initialization complete. Created {tarotSlotObjects.Count} slots");
        
        // Now sync current equipment to tarot slots
        RefreshAllTarotSlots();
    }
    
    public void RefreshAllTarotSlots()
    {
        if (!tarotPanelInitialized || inventoryData == null) return;
        
        // Update each slot based on equipment data
        for (int i = 0; i < inventoryData.equipmentSlots.Count && i < 3; i++)
        {
            var slot = inventoryData.equipmentSlots[i];
            if (slot.isOccupied && slot.storedCard != null && slot.storedCard.CanBeUsed())
            {
                UpdateTarotSlot(i, slot.storedCard);
            }
            else
            {
                ShowTarotSlot(i, false);
            }
        }
    }
    
    private void UpdateTarotSlot(int slotIndex, TarotCardData cardData)
    {
        // If tarot panel not initialized yet, initialize it first
        if (!tarotPanelInitialized)
        {
            Debug.Log($"Tarot panel not initialized yet. Initializing now for slot {slotIndex}");
            InitializeTarotPanelSlots();
            // Queue the update for after initialization
            StartCoroutine(UpdateTarotSlotAfterInit(slotIndex, cardData));
            return;
        }
        
        if (!tarotSlotCards.ContainsKey(slotIndex) || !tarotSlotObjects.ContainsKey(slotIndex))
        {
            Debug.LogWarning($"Tarot slot {slotIndex} not initialized");
            return;
        }
        
        var card = tarotSlotCards[slotIndex];
        var cardObject = tarotSlotObjects[slotIndex];
        
        if (card != null && cardObject != null)
        {
            // Update card data
            card.cardData = cardData;
            Debug.Log($"[InventoryManager] Set card data for slot {slotIndex}: {cardData.cardName}");
            
            // Force update the display
            card.UpdateCardDisplay();
            Debug.Log($"[InventoryManager] Called UpdateCardDisplay for slot {slotIndex}");
            
            // Show the card
            ShowTarotSlot(slotIndex, true);
            
            Debug.Log($"[InventoryManager] Successfully updated tarot slot {slotIndex} with {cardData.cardName}");
        }
    }
    
    private void ShowTarotSlot(int slotIndex, bool show)
    {
        if (tarotSlotObjects.ContainsKey(slotIndex))
        {
            var cardObject = tarotSlotObjects[slotIndex];
            if (cardObject != null)
            {
                cardObject.SetActive(show);
                Debug.Log($"[InventoryManager] Tarot slot {slotIndex} visibility set to: {show}");
                
                // Also check if parent is active
                if (show && cardObject.transform.parent != null)
                {
                    bool parentActive = cardObject.transform.parent.gameObject.activeInHierarchy;
                    Debug.Log($"[InventoryManager] Tarot slot {slotIndex} parent active: {parentActive}");
                }
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] Tarot slot {slotIndex} card object is null!");
            }
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] Tarot slot {slotIndex} not found in tarotSlotObjects dictionary!");
        }
    }
    
    private IEnumerator UpdateTarotSlotAfterInit(int slotIndex, TarotCardData cardData)
    {
        // Wait for initialization to complete
        while (!tarotPanelInitialized)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Now update the slot
        UpdateTarotSlot(slotIndex, cardData);
    }
    
    // PERSISTENCE METHODS
    
    private void SaveInventoryToPlayerPrefs()
    {
        if (!enablePersistence || inventoryData == null) return;
        
        try
        {
            InventorySaveData saveData = new InventorySaveData();
            
            // Save storage slots
            foreach (var slot in inventoryData.storageSlots)
            {
                if (slot.isOccupied && slot.storedCard != null)
                {
                    saveData.storageCards.Add(CardSaveData.FromTarotCardData(slot.storedCard, slot.slotIndex));
                }
            }
            
            // Save equipment slots
            foreach (var slot in inventoryData.equipmentSlots)
            {
                if (slot.isOccupied && slot.storedCard != null)
                {
                    saveData.equippedCards.Add(CardSaveData.FromTarotCardData(slot.storedCard, slot.slotIndex));
                }
            }
            
            string jsonData = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(INVENTORY_SAVE_KEY, jsonData);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save inventory: {e.Message}");
        }
    }
    
    private void LoadInventoryFromPlayerPrefs()
    {
        if (!enablePersistence || inventoryData == null) return;
        
        try
        {
            string jsonData = PlayerPrefs.GetString(INVENTORY_SAVE_KEY, "");
            if (string.IsNullOrEmpty(jsonData))
            {
                return;
            }
            
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(jsonData);
            
            // Clear existing slots first
            foreach (var slot in inventoryData.storageSlots)
            {
                slot.RemoveCard();
            }
            foreach (var slot in inventoryData.equipmentSlots)
            {
                slot.RemoveCard();
            }
            
            // Load storage cards
            foreach (var cardSave in saveData.storageCards)
            {
                TarotCardData card = CreateCardFromSaveData(cardSave);
                if (card != null && cardSave.slotIndex < inventoryData.storageSlots.Count)
                {
                    inventoryData.storageSlots[cardSave.slotIndex].StoreCard(card);
                }
            }
            
            // Load equipped cards
            foreach (var cardSave in saveData.equippedCards)
            {
                TarotCardData card = CreateCardFromSaveData(cardSave);
                if (card != null && cardSave.slotIndex < inventoryData.equipmentSlots.Count)
                {
                    inventoryData.equipmentSlots[cardSave.slotIndex].StoreCard(card);
                }
            }
            
            // Force a UI refresh after loading cards to ensure images display
            StartCoroutine(ForceUIRefreshAfterLoad());
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load inventory: {e.Message}");
        }
    }
    
    private TarotCardData CreateCardFromSaveData(CardSaveData saveData)
    {
        // Create TarotCardData from save data
        TarotCardData card = saveData.ToTarotCardData();
        bool foundOriginalSprite = false;
        
        // Method 1: Try to find original card data from registry or Resources
        TarotCardData originalCard = FindOriginalCardDataFromResources(saveData.cardType);
        if (originalCard != null && originalCard.cardImage != null)
        {
            card.cardImage = originalCard.cardImage;
            card.description = originalCard.description;
            card.price = originalCard.price;
            foundOriginalSprite = true;
            Debug.Log($"[InventoryManager] Found original sprite for {card.cardName} from registry/resources");
        }
        else
        {
            // Method 2: Try to find existing card sprite from scene cards
            TarotCard[] existingCards = FindObjectsOfType<TarotCard>();
            foreach (var existingCard in existingCards)
            {
                if (existingCard.cardData != null && existingCard.cardData.cardType == saveData.cardType 
                    && existingCard.cardData.cardImage != null)
                {
                    card.cardImage = existingCard.cardData.cardImage;
                    card.description = existingCard.cardData.description;
                    card.price = existingCard.cardData.price;
                    
                    // Register this card for future lookups
                    RegisterCardData(existingCard.cardData);
                    foundOriginalSprite = true;
                    Debug.Log($"[InventoryManager] Found original sprite for {card.cardName} from scene cards");
                    break;
                }
            }
        }
        
        // If no sprite found, use placeholder but mark for later refresh
        if (!foundOriginalSprite || card.cardImage == null)
        {
            card.cardImage = CreatePlaceholderSprite();
            card.description = "Inventory card";
            card.price = 100;
            
            // Mark this card for sprite refresh when original data becomes available
            MarkCardForSpriteRefresh(card);
            Debug.Log($"[InventoryManager] Using placeholder sprite for {card.cardName} - marked for refresh");
        }
        
        // Always use correct material based on saved material type
        if (card.assignedMaterial != null)
        {
            // Try to load the actual MaterialData from Resources first
            MaterialData originalMaterial = LoadMaterialFromResources(saveData.materialType);
            if (originalMaterial != null && originalMaterial.backgroundSprite != null)
            {
                card.assignedMaterial.backgroundSprite = originalMaterial.backgroundSprite;
            }
            else
            {
                // Fallback to creating a colored sprite
                card.assignedMaterial.backgroundSprite = CreateMaterialBackgroundSprite(saveData.materialType);
            }
        }
        
        return card;
    }
    
    private TarotCardData FindOriginalCardDataFromResources(TarotCardType cardType)
    {
        // Method 0: Check our static registry first (most reliable)
        if (cardDataRegistry.ContainsKey(cardType) && cardDataRegistry[cardType] != null)
        {
            Debug.Log($"[InventoryManager] Found card {cardType} in registry cache");
            return cardDataRegistry[cardType];
        }
        
        // Try to load from Resources folder first
        TarotCardData[] allCards = Resources.LoadAll<TarotCardData>("");
        foreach (var card in allCards)
        {
            if (card.cardType == cardType)
            {
                // Cache it for future use
                RegisterCardData(card);
                return card;
            }
        }
        
        // Try from Materials subfolder in Resources
        TarotCardData[] materialCards = Resources.LoadAll<TarotCardData>("Materials");
        foreach (var card in materialCards)
        {
            if (card.cardType == cardType)
            {
                // Cache it for future use
                RegisterCardData(card);
                return card;
            }
        }
        
        // For now, since ScriptableObjects are not in Resources, we'll use UnityEditor to load them
        #if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TarotCardData", new[] {"Assets/ScriptableObject"});
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            TarotCardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<TarotCardData>(path);
            if (card != null && card.cardType == cardType)
            {
                // Cache it for future use
                RegisterCardData(card);
                return card;
            }
        }
        #endif
        
        return null;
    }
    
    /// <summary>
    /// Register a TarotCardData in the static registry for future lookups.
    /// Call this whenever a card is encountered (e.g., when shop loads).
    /// </summary>
    public static void RegisterCardData(TarotCardData card)
    {
        if (card == null) return;
        
        if (!cardDataRegistry.ContainsKey(card.cardType) || cardDataRegistry[card.cardType] == null)
        {
            cardDataRegistry[card.cardType] = card;
            Debug.Log($"[InventoryManager] Registered card data: {card.cardName} ({card.cardType})");
        }
        
        // Check if any cards need sprite refresh
        RefreshPendingCardSprites(card.cardType);
    }
    
    /// <summary>
    /// Mark a card as needing sprite refresh (will be fixed when original data becomes available)
    /// </summary>
    private void MarkCardForSpriteRefresh(TarotCardData card)
    {
        if (card != null && !cardsNeedingSpriteRefresh.Contains(card))
        {
            cardsNeedingSpriteRefresh.Add(card);
            Debug.Log($"[InventoryManager] Card {card.cardName} marked for sprite refresh");
        }
    }
    
    /// <summary>
    /// Refresh sprites for any cards that were loaded with placeholders
    /// </summary>
    private static void RefreshPendingCardSprites(TarotCardType cardType)
    {
        if (cardsNeedingSpriteRefresh.Count == 0) return;
        
        // Find the registered original card
        if (!cardDataRegistry.ContainsKey(cardType) || cardDataRegistry[cardType] == null) return;
        
        TarotCardData originalCard = cardDataRegistry[cardType];
        if (originalCard.cardImage == null) return;
        
        // Find and update cards of this type that need refresh
        List<TarotCardData> toRemove = new List<TarotCardData>();
        foreach (var card in cardsNeedingSpriteRefresh)
        {
            if (card != null && card.cardType == cardType)
            {
                card.cardImage = originalCard.cardImage;
                card.description = originalCard.description;
                card.price = originalCard.price;
                toRemove.Add(card);
                Debug.Log($"[InventoryManager] Refreshed sprite for {card.cardName}");
            }
        }
        
        foreach (var card in toRemove)
        {
            cardsNeedingSpriteRefresh.Remove(card);
        }
        
        // If cards were refreshed, trigger UI update
        if (toRemove.Count > 0 && Instance != null)
        {
            Instance.StartCoroutine(Instance.TriggerDelayedUIRefresh());
        }
    }
    
    /// <summary>
    /// Trigger a delayed UI refresh to show updated sprites
    /// </summary>
    private IEnumerator TriggerDelayedUIRefresh()
    {
        yield return new WaitForSeconds(0.1f);
        
        // Find and refresh inventory UI
        InventoryPanelUI inventoryUI = FindObjectOfType<InventoryPanelUI>();
        if (inventoryUI != null)
        {
            var refreshMethod = inventoryUI.GetType().GetMethod("ForceRefreshInventoryDisplay", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(inventoryUI, null);
            }
        }
        
        // Find and refresh V3 panel if exists
        InventoryPanelUIV3 panelV3 = FindObjectOfType<InventoryPanelUIV3>();
        if (panelV3 != null)
        {
            panelV3.RefreshAllSlots();
            panelV3.RefreshEquipmentSlots();
        }
        
        // Find and refresh tarot window
        TarotWindowUI tarotWindow = FindObjectOfType<TarotWindowUI>();
        if (tarotWindow != null)
        {
            tarotWindow.Refresh();
        }
        
        Debug.Log("[InventoryManager] Triggered delayed UI refresh after sprite update");
    }
    
    /// <summary>
    /// Force scan scene for TarotCards and register their data
    /// </summary>
    public void ScanAndRegisterSceneCards()
    {
        TarotCard[] sceneCards = FindObjectsOfType<TarotCard>();
        int registered = 0;
        foreach (var card in sceneCards)
        {
            if (card.cardData != null && card.cardData.cardImage != null)
            {
                RegisterCardData(card.cardData);
                registered++;
            }
        }
        Debug.Log($"[InventoryManager] Scanned scene and registered {registered} cards");
    }
    
    private MaterialData LoadMaterialFromResources(TarotMaterialType materialType)
    {
        // Try to load the specific material from Resources/Materials folder
        string materialName = materialType.ToString();
        MaterialData material = Resources.Load<MaterialData>($"Materials/{materialName}");
        
        if (material != null)
        {
            return material;
        }
        
        // Try alternative naming (CardBoard vs Cardboard)
        if (materialType == TarotMaterialType.Cardboard)
        {
            material = Resources.Load<MaterialData>("Materials/CardBoard");
            if (material != null)
            {
                return material;
            }
        }
        
        return null;
    }
    
    private Sprite CreatePlaceholderSprite()
    {
        // Try to find an existing sprite in the game first
        Sprite existingSprite = TryGetExistingCardSprite();
        if (existingSprite != null)
        {
            Debug.Log("✅ Using existing card sprite for placeholder");
            return existingSprite;
        }
        
        // Create a simple 64x64 texture with a border as placeholder
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[64 * 64];
        
        // Create a simple card-like image with border
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                // Create border
                if (x < 2 || x > 61 || y < 2 || y > 61)
                {
                    pixels[y * 64 + x] = Color.black;
                }
                // Inner area - make it more visible
                else if (x > 10 && x < 54 && y > 10 && y < 54)
                {
                    pixels[y * 64 + x] = new Color(0.8f, 0.8f, 1f, 1f); // Light blue
                }
                else
                {
                    pixels[y * 64 + x] = new Color(0.9f, 0.9f, 0.9f, 1f); // Light gray
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = "PlaceholderCardSprite";
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        sprite.name = "PlaceholderCardSprite";
        
        Debug.Log("✅ Created placeholder card sprite");
        return sprite;
    }
    
    private Sprite TryGetExistingCardSprite()
    {
        // Try to find any existing tarot card in the scene and use its sprite
        TarotCard existingCard = FindObjectOfType<TarotCard>();
        if (existingCard != null && existingCard.cardData != null && existingCard.cardData.cardImage != null)
        {
            return existingCard.cardData.cardImage;
        }
        
        // Try to get Unity's built-in sprite
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
    }
    
    private Sprite CreateMaterialBackgroundSprite(TarotMaterialType materialType)
    {
        // Create a colored background based on material type
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color materialColor = GetMaterialColor(materialType);
        
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = materialColor;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = $"Material_{materialType}";
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        sprite.name = $"Material_{materialType}";
        
        Debug.Log($"✅ Created {materialType} material background sprite");
        return sprite;
    }
    
    private Color GetMaterialColor(TarotMaterialType materialType)
    {
        switch (materialType)
        {
            case TarotMaterialType.Paper: return new Color(0.9f, 0.9f, 0.8f, 0.8f); // Light beige
            case TarotMaterialType.Cardboard: return new Color(0.7f, 0.6f, 0.4f, 0.8f); // Brown
            case TarotMaterialType.Wood: return new Color(0.6f, 0.4f, 0.2f, 0.8f); // Dark brown
            case TarotMaterialType.Copper: return new Color(0.8f, 0.5f, 0.2f, 0.8f); // Copper color
            case TarotMaterialType.Silver: return new Color(0.8f, 0.8f, 0.9f, 0.8f); // Silver
            case TarotMaterialType.Gold: return new Color(1f, 0.8f, 0.2f, 0.8f); // Gold
            case TarotMaterialType.Platinum: return new Color(0.9f, 0.9f, 1f, 0.8f); // Platinum
            case TarotMaterialType.Diamond: return new Color(0.9f, 0.9f, 1f, 1f); // Bright white/diamond
            default: return Color.white;
        }
    }
    
    private System.Collections.IEnumerator ForceUIRefreshAfterLoad()
    {
        yield return new WaitForSeconds(0.5f); // Wait a bit for UI to be ready
        
        // Scan scene for cards to populate registry (shop might be loaded now)
        ScanAndRegisterSceneCards();
        
        // Check if any pending cards can now be refreshed
        if (cardsNeedingSpriteRefresh.Count > 0)
        {
            Debug.Log($"[InventoryManager] {cardsNeedingSpriteRefresh.Count} cards still need sprite refresh, attempting...");
            
            // Try to refresh any pending cards from the newly registered data
            List<TarotCardData> toRemove = new List<TarotCardData>();
            foreach (var card in cardsNeedingSpriteRefresh)
            {
                if (card != null && cardDataRegistry.ContainsKey(card.cardType))
                {
                    TarotCardData original = cardDataRegistry[card.cardType];
                    if (original != null && original.cardImage != null)
                    {
                        card.cardImage = original.cardImage;
                        card.description = original.description;
                        card.price = original.price;
                        toRemove.Add(card);
                        Debug.Log($"[InventoryManager] Late refresh: Updated sprite for {card.cardName}");
                    }
                }
            }
            
            foreach (var card in toRemove)
            {
                cardsNeedingSpriteRefresh.Remove(card);
            }
        }
        
        // Find and refresh the inventory UI
        InventoryPanelUI inventoryUI = FindObjectOfType<InventoryPanelUI>();
        if (inventoryUI != null)
        {
            Debug.Log("🔄 Forcing UI refresh after card loading...");
            
            var refreshMethod = inventoryUI.GetType().GetMethod("ForceRefreshInventoryDisplay", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(inventoryUI, null);
                Debug.Log("✅ UI refreshed after card loading");
            }
            else
            {
                // Fallback to regular refresh
                inventoryUI.ForceRefreshInventoryDisplay();
                Debug.Log("✅ UI refreshed after card loading (direct call)");
            }
        }
        
        // Also refresh V3 panel
        InventoryPanelUIV3 panelV3 = FindObjectOfType<InventoryPanelUIV3>();
        if (panelV3 != null)
        {
            panelV3.RefreshAllSlots();
            panelV3.RefreshEquipmentSlots();
        }
        
        // Refresh tarot window
        TarotWindowUI tarotWindow = FindObjectOfType<TarotWindowUI>();
        if (tarotWindow != null)
        {
            tarotWindow.Refresh();
        }
    }
    
    // TAROT PANEL SYNC METHODS
    
    public void SyncWithTarotPanel()
    {
        if (shopManager == null || shopManager.tarotPanel == null) return;
        
        // Get all cards currently in the tarot panel
        TarotCard[] tarotCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        
        foreach (var tarotCard in tarotCards)
        {
            if (tarotCard.cardData != null && !tarotCard.isInShop)
            {
                // Check if this card is in our inventory
                bool foundInInventory = false;
                
                // Check equipment slots
                foreach (var slot in inventoryData.equipmentSlots)
                {
                    if (slot.isOccupied && slot.storedCard == tarotCard.cardData)
                    {
                        foundInInventory = true;
                        break;
                    }
                }
                
                // If not equipped, remove from tarot panel
                if (!foundInInventory)
                {
                    Debug.Log($"Removing {tarotCard.cardData.cardName} from tarot panel - not equipped in inventory");
                    Destroy(tarotCard.gameObject);
                }
            }
        }
    }
    
    public void RemoveCardFromTarotPanel(TarotCardData card)
    {
        if (card == null)
        {
            Debug.LogWarning("RemoveCardFromTarotPanel: card is null");
            return;
        }
        
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
            if (shopManager == null)
            {
                Debug.LogWarning("RemoveCardFromTarotPanel: ShopManager not found");
                return;
            }
        }
        
        if (shopManager.tarotPanel == null)
        {
            Debug.LogWarning("RemoveCardFromTarotPanel: tarotPanel is null");
            return;
        }
        
        TarotCard[] tarotCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        Debug.Log($"RemoveCardFromTarotPanel: Found {tarotCards.Length} cards in tarot panel, looking for {card.cardName}");
        
        bool foundCard = false;
        for (int i = 0; i < tarotCards.Length; i++)
        {
            var tarotCard = tarotCards[i];
            if (tarotCard.cardData != null && !tarotCard.isInShop)
            {
                // Try multiple matching criteria
                bool isMatch = tarotCard.cardData == card || 
                              (tarotCard.cardData.cardName == card.cardName && 
                               tarotCard.cardData.cardType == card.cardType);
                
                Debug.Log($"Checking tarot card {i}: {tarotCard.cardData.cardName} (isInShop: {tarotCard.isInShop}) - Match: {isMatch}");
                
                if (isMatch)
                {
                    Debug.Log($"Removing {card.cardName} from tarot panel at index {i}");
                    Destroy(tarotCard.gameObject);
                    foundCard = true;
                    break;
                }
            }
        }
        
        if (!foundCard)
        {
            Debug.LogWarning($"Could not find {card.cardName} in tarot panel to remove");
        }
    }
    
    
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && enablePersistence)
        {
            SaveInventoryToPlayerPrefs();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && enablePersistence)
        {
            SaveInventoryToPlayerPrefs();
        }
    }
    
    private void OnDestroy()
    {
        if (enablePersistence)
        {
            SaveInventoryToPlayerPrefs();
        }
    }
}

[System.Serializable]
public class InventorySaveData
{
    public List<CardSaveData> storageCards = new List<CardSaveData>();
    public List<CardSaveData> equippedCards = new List<CardSaveData>();
}

[System.Serializable]
public class CardSaveData
{
    public string cardName;
    public TarotCardType cardType;
    public int currentUses;
    public int maxUses;
    public string materialName;
    public TarotMaterialType materialType;
    public int slotIndex;
    
    // Convert from TarotCardData
    public static CardSaveData FromTarotCardData(TarotCardData card, int slot = -1)
    {
        return new CardSaveData
        {
            cardName = card.cardName,
            cardType = card.cardType,
            currentUses = card.currentUses,
            maxUses = card.maxUses,
            materialName = card.assignedMaterial?.materialName ?? "",
            materialType = card.assignedMaterial?.materialType ?? TarotMaterialType.Paper,
            slotIndex = slot
        };
    }
    
    // Convert back to TarotCardData
    public TarotCardData ToTarotCardData()
    {
        // Create runtime TarotCardData
        TarotCardData card = ScriptableObject.CreateInstance<TarotCardData>();
        card.cardName = cardName;
        card.cardType = cardType;
        
        // Create runtime MaterialData
        MaterialData material = ScriptableObject.CreateInstance<MaterialData>();
        material.materialName = materialName;
        material.materialType = materialType;
        material.maxUses = maxUses;
        
        card.AssignMaterial(material);
        
        // Restore durability AFTER AssignMaterial (which resets currentUses to 0)
        card.currentUses = currentUses;
        card.maxUses = maxUses;
        
        return card;
    }
}

