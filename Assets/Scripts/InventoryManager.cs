using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
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
        
        // Sync equipped cards with tarot panel on startup
        Invoke("SyncEquippedCardsToTarotPanel", 0.1f);
        
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
            
            // Sync equipped cards with tarot panel
            Invoke("SyncEquippedCardsToTarotPanel", 0.1f);
            
            // Find shop manager for old system integration
            if (shopManager == null)
            {
                shopManager = FindObjectOfType<ShopManager>();
                if (shopManager != null)
                {
                    Debug.Log("✅ Found and assigned ShopManager reference");
                }
            }
            
            Debug.Log("✅ Inventory force-initialized with " + inventoryData.storageSlotCount + " storage slots and " + 
                     inventoryData.equipmentSlotCount + " equipment slots");
        }
    }
    
    // Add a purchased card to inventory
    public virtual bool AddPurchasedCard(TarotCardData card)
    {
        if (card == null) return false;
        
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
                if (!inventoryData.equipmentSlots[i].isOccupied)
                {
                    equipmentSlotIndex = i;
                    break;
                }
            }
        }
        
        if (equipmentSlotIndex == -1) return false; // No available equipment slots
        
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
            
            // Sync with tarot panel
            SyncEquippedCardsToTarotPanel();
            
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
        
        // Remove from tarot panel first
        RemoveCardFromTarotPanel(card);
        
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
            
            // Sync with tarot panel
            SyncEquippedCardsToTarotPanel();
            
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
        
        // Remove from tarot panel if present
        RemoveCardFromTarotPanel(card);
        
        bool removed = inventoryData.RemoveCard(card);
        if (removed)
        {
            Debug.Log($"Removed used up card {card.cardName} from inventory");
            OnCardRemoved?.Invoke(card);
            
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
        if (inventoryData == null) return false;
        
        foreach (var slot in inventoryData.equipmentSlots)
        {
            if (!slot.isOccupied) return true;
        }
        return false;
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
        SyncEquippedCardsToTarotPanel();
        Debug.Log("Forced synchronization with tarot panel");
    }
    
    // Force save inventory (useful for debugging)
    [ContextMenu("Force Save Inventory")]
    public void ForceSaveInventory()
    {
        SaveInventoryToPlayerPrefs();
        Debug.Log("Forced save inventory to PlayerPrefs");
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
    
    [ContextMenu("Debug Tarot Panel State")]
    public void DebugTarotPanelState()
    {
        if (shopManager == null || shopManager.tarotPanel == null)
        {
            Debug.LogError("❌ shopManager or tarotPanel is null");
            return;
        }
        
        Debug.Log("🔍 DEBUG Tarot Panel State:");
        
        TarotCard[] allCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        Debug.Log($"   Total cards in tarot panel: {allCards.Length}");
        
        int shopCards = 0;
        int ownedCards = 0;
        
        for (int i = 0; i < allCards.Length; i++)
        {
            var card = allCards[i];
            if (card.isInShop)
            {
                shopCards++;
                Debug.Log($"   [{i}] SHOP CARD: {card.cardData?.cardName ?? "null"}");
            }
            else
            {
                ownedCards++;
                Debug.Log($"   [{i}] OWNED CARD: {card.cardData?.cardName ?? "null"} (Parent: {card.transform.parent?.name ?? "null"})");
            }
        }
        
        Debug.Log($"   Summary: {shopCards} shop cards, {ownedCards} owned cards");
        
        // Check tarot slots
        if (shopManager.tarotSlots != null)
        {
            Debug.Log($"   Tarot slots available: {shopManager.tarotSlots.Count}");
            for (int i = 0; i < shopManager.tarotSlots.Count; i++)
            {
                var slot = shopManager.tarotSlots[i];
                Debug.Log($"   Slot[{i}] ({slot.name}): {slot.childCount} children");
            }
        }
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
        if (shopManager == null || shopManager.tarotPanel == null || inventoryData == null) 
        {
            Debug.LogWarning("Cannot sync to tarot panel - missing references");
            return;
        }
        
        Debug.Log("🔄 SyncEquippedCardsToTarotPanel: Starting sync...");
        
        // Get currently equipped cards
        List<TarotCardData> equippedCards = new List<TarotCardData>();
        foreach (var slot in inventoryData.equipmentSlots)
        {
            if (slot.isOccupied && slot.storedCard != null && slot.storedCard.CanBeUsed())
            {
                equippedCards.Add(slot.storedCard);
                Debug.Log($"   📋 Found equipped card: {slot.storedCard.cardName}");
            }
        }
        
        // Get existing tarot panel cards (non-shop)
        TarotCard[] existingCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        List<TarotCard> nonShopCards = new List<TarotCard>();
        
        foreach (var card in existingCards)
        {
            if (card != null && !card.isInShop)
            {
                nonShopCards.Add(card);
                Debug.Log($"   🎯 Found existing tarot panel card: {card.cardData?.cardName ?? "null"}");
            }
        }
        
        // Remove cards that are no longer equipped
        foreach (var tarotCard in nonShopCards)
        {
            bool stillEquipped = false;
            if (tarotCard.cardData != null)
            {
                foreach (var equippedCard in equippedCards)
                {
                    if (tarotCard.cardData == equippedCard || 
                        (tarotCard.cardData.cardName == equippedCard.cardName && 
                         tarotCard.cardData.cardType == equippedCard.cardType))
                    {
                        stillEquipped = true;
                        break;
                    }
                }
            }
            
            if (!stillEquipped)
            {
                Debug.Log($"   🗑️ Removing unequipped card from tarot panel: {tarotCard.cardData?.cardName ?? "null"}");
                Destroy(tarotCard.gameObject);
            }
        }
        
        // Add newly equipped cards that aren't in the tarot panel yet
        foreach (var equippedCard in equippedCards)
        {
            bool alreadyInPanel = false;
            foreach (var tarotCard in nonShopCards)
            {
                if (tarotCard != null && tarotCard.cardData != null &&
                    (tarotCard.cardData == equippedCard || 
                     (tarotCard.cardData.cardName == equippedCard.cardName && 
                      tarotCard.cardData.cardType == equippedCard.cardType)))
                {
                    alreadyInPanel = true;
                    break;
                }
            }
            
            if (!alreadyInPanel)
            {
                Debug.Log($"   ➕ Adding new equipped card to tarot panel: {equippedCard.cardName}");
                CreateTarotCardInPanel(equippedCard);
            }
        }
        
        Debug.Log($"✅ Sync complete: {equippedCards.Count} equipped cards should be in tarot panel");
    }
    
    private void CreateTarotCardInPanel(TarotCardData cardData)
    {
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
        
        Debug.Log($"🃏 Creating tarot card {cardData.cardName} in empty slot {emptySlot.name}");
        
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
            
            Debug.Log($"✅ Successfully created {cardData.cardName} in tarot panel slot {emptySlot.name}");
        }
        else
        {
            Debug.LogError($"❌ Failed to get TarotCard component from instantiated prefab for {cardData.cardName}");
            Destroy(cardObject);
        }
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
            
            Debug.Log("Inventory saved to PlayerPrefs");
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
                Debug.Log("📭 No saved inventory data found in PlayerPrefs");
                return;
            }
            
            Debug.Log($"📥 Loading inventory data: {jsonData.Substring(0, Math.Min(100, jsonData.Length))}...");
            
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(jsonData);
            
            Debug.Log($"📊 Parsed save data: {saveData.storageCards.Count} storage cards, {saveData.equippedCards.Count} equipped cards");
            
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
                Debug.Log($"🔄 Loading storage card: {cardSave.cardName} (Type: {cardSave.cardType}, Uses: {cardSave.currentUses}/{cardSave.maxUses}, Slot: {cardSave.slotIndex})");
                
                TarotCardData card = CreateCardFromSaveData(cardSave);
                if (card != null && cardSave.slotIndex < inventoryData.storageSlots.Count)
                {
                    inventoryData.storageSlots[cardSave.slotIndex].StoreCard(card);
                    Debug.Log($"✅ Successfully loaded {card.cardName} into storage slot {cardSave.slotIndex}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to load card {cardSave.cardName} - card is null or invalid slot index {cardSave.slotIndex}");
                }
            }
            
            // Load equipped cards
            foreach (var cardSave in saveData.equippedCards)
            {
                Debug.Log($"🔄 Loading equipped card: {cardSave.cardName} (Type: {cardSave.cardType}, Uses: {cardSave.currentUses}/{cardSave.maxUses}, Slot: {cardSave.slotIndex})");
                
                TarotCardData card = CreateCardFromSaveData(cardSave);
                if (card != null && cardSave.slotIndex < inventoryData.equipmentSlots.Count)
                {
                    inventoryData.equipmentSlots[cardSave.slotIndex].StoreCard(card);
                    Debug.Log($"✅ Successfully loaded {card.cardName} into equipment slot {cardSave.slotIndex}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to load card {cardSave.cardName} - card is null or invalid slot index {cardSave.slotIndex}");
                }
            }
            
            Debug.Log($"✅ Inventory loaded from PlayerPrefs: {saveData.storageCards.Count} storage, {saveData.equippedCards.Count} equipped");
            
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
        
        // Method 1: Try to find original card data from Resources folder first
        TarotCardData originalCard = FindOriginalCardDataFromResources(saveData.cardType);
        if (originalCard != null)
        {
            card.cardImage = originalCard.cardImage;
            card.description = originalCard.description;
            card.price = originalCard.price;
            
            Debug.Log($"✅ Enhanced card with Resources sprite: {saveData.cardName}");
        }
        else
        {
            // Method 2: Try to find existing card sprite from scene cards
            TarotCard[] existingCards = FindObjectsOfType<TarotCard>();
            foreach (var existingCard in existingCards)
            {
                if (existingCard.cardData != null && existingCard.cardData.cardType == saveData.cardType)
                {
                    card.cardImage = existingCard.cardData.cardImage;
                    card.description = existingCard.cardData.description;
                    card.price = existingCard.cardData.price;
                    
                    Debug.Log($"✅ Enhanced card with scene sprite: {saveData.cardName}");
                    break;
                }
            }
        }
        
        // If no sprite found, use placeholder
        if (card.cardImage == null)
        {
            card.cardImage = CreatePlaceholderSprite();
            card.description = "Inventory card";
            card.price = 100;
            Debug.Log($"⚠️ Using placeholder sprite for: {saveData.cardName}");
        }
        
        // Always use correct material based on saved material type
        if (card.assignedMaterial != null)
        {
            // Try to load the actual MaterialData from Resources first
            MaterialData originalMaterial = LoadMaterialFromResources(saveData.materialType);
            if (originalMaterial != null && originalMaterial.backgroundSprite != null)
            {
                card.assignedMaterial.backgroundSprite = originalMaterial.backgroundSprite;
                Debug.Log($"🎨 Loaded original material background for {saveData.cardName}: {saveData.materialType}");
            }
            else
            {
                // Fallback to creating a colored sprite
                card.assignedMaterial.backgroundSprite = CreateMaterialBackgroundSprite(saveData.materialType);
                Debug.Log($"🎨 Created fallback material background for {saveData.cardName}: {saveData.materialType}");
            }
        }
        
        Debug.Log($"✅ Recreated card: {saveData.cardName} ({saveData.materialType}, {saveData.currentUses}/{saveData.maxUses} uses)");
        return card;
    }
    
    private TarotCardData FindOriginalCardDataFromResources(TarotCardType cardType)
    {
        // Try to load from Resources folder first
        TarotCardData[] allCards = Resources.LoadAll<TarotCardData>("");
        foreach (var card in allCards)
        {
            if (card.cardType == cardType)
            {
                Debug.Log($"✅ Found original card data for {cardType} from Resources root");
                return card;
            }
        }
        
        // Try from Materials subfolder in Resources
        TarotCardData[] materialCards = Resources.LoadAll<TarotCardData>("Materials");
        foreach (var card in materialCards)
        {
            if (card.cardType == cardType)
            {
                Debug.Log($"✅ Found original card data for {cardType} from Resources/Materials");
                return card;
            }
        }
        
        // For now, since ScriptableObjects are not in Resources, we'll use UnityEditor to load them
        // This is a workaround - ideally these should be moved to Resources folder
        #if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TarotCardData", new[] {"Assets/ScriptableObject"});
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            TarotCardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<TarotCardData>(path);
            if (card != null && card.cardType == cardType)
            {
                Debug.Log($"✅ Found original card data for {cardType} from ScriptableObject folder: {path}");
                return card;
            }
        }
        #endif
        
        Debug.Log($"⚠️ Could not find original card data for {cardType} anywhere");
        return null;
    }
    
    private MaterialData LoadMaterialFromResources(TarotMaterialType materialType)
    {
        // Try to load the specific material from Resources/Materials folder
        string materialName = materialType.ToString();
        MaterialData material = Resources.Load<MaterialData>($"Materials/{materialName}");
        
        if (material != null)
        {
            Debug.Log($"✅ Loaded MaterialData from Resources: {materialName}");
            return material;
        }
        
        // Try alternative naming (CardBoard vs Cardboard)
        if (materialType == TarotMaterialType.Cardboard)
        {
            material = Resources.Load<MaterialData>("Materials/CardBoard");
            if (material != null)
            {
                Debug.Log($"✅ Loaded MaterialData from Resources: CardBoard (alt naming)");
                return material;
            }
        }
        
        Debug.Log($"⚠️ Could not load MaterialData for {materialType} from Resources");
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
        card.currentUses = currentUses;
        card.maxUses = maxUses;
        
        // Create runtime MaterialData
        MaterialData material = ScriptableObject.CreateInstance<MaterialData>();
        material.materialName = materialName;
        material.materialType = materialType;
        material.maxUses = maxUses;
        
        card.AssignMaterial(material);
        return card;
    }
}
