using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("Inventory Configuration")]
    public InventoryData inventoryData; // Reference to the ScriptableObject
    
    [Header("Persistence")]
    public bool enablePersistence = true;
    private const string INVENTORY_SAVE_KEY = "InventoryData_v1";
    
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
    
    // Sync equipped cards TO the tarot panel (populate it with equipped cards)
    public void SyncEquippedCardsToTarotPanel()
    {
        if (shopManager == null || shopManager.tarotPanel == null || inventoryData == null) 
        {
            Debug.LogWarning("Cannot sync to tarot panel - missing references");
            return;
        }
        
        // First, clear the tarot panel
        TarotCard[] existingCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        foreach (var card in existingCards)
        {
            if (card != null && !card.isInShop)
            {
                Destroy(card.gameObject);
            }
        }
        
        // Then add all equipped cards to the tarot panel
        foreach (var slot in inventoryData.equipmentSlots)
        {
            if (slot.isOccupied && slot.storedCard != null && slot.storedCard.CanBeUsed())
            {
                CreateTarotCardInPanel(slot.storedCard);
            }
        }
        
        Debug.Log("Synced equipped cards to tarot panel");
    }
    
    private void CreateTarotCardInPanel(TarotCardData cardData)
    {
        if (shopManager == null || shopManager.tarotCardPrefab == null) return;
        
        Transform emptySlot = shopManager.GetEmptyTarotSlot();
        if (emptySlot == null) 
        {
            Debug.LogWarning("No empty tarot slots available");
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
            
            Debug.Log($"Created {cardData.cardName} in tarot panel");
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
                    saveData.storageCards.Add(new CardSaveData
                    {
                        cardName = slot.storedCard.cardName,
                        cardType = slot.storedCard.cardType,
                        currentUses = slot.storedCard.currentUses,
                        maxUses = slot.storedCard.maxUses,
                        materialName = slot.storedCard.assignedMaterial?.materialName ?? "",
                        slotIndex = slot.slotIndex
                    });
                }
            }
            
            // Save equipment slots
            foreach (var slot in inventoryData.equipmentSlots)
            {
                if (slot.isOccupied && slot.storedCard != null)
                {
                    saveData.equippedCards.Add(new CardSaveData
                    {
                        cardName = slot.storedCard.cardName,
                        cardType = slot.storedCard.cardType,
                        currentUses = slot.storedCard.currentUses,
                        maxUses = slot.storedCard.maxUses,
                        materialName = slot.storedCard.assignedMaterial?.materialName ?? "",
                        slotIndex = slot.slotIndex
                    });
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
            
            // Load storage cards
            foreach (var cardSave in saveData.storageCards)
            {
                Debug.Log($"🔄 Loading storage card: {cardSave.cardName} (Type: {cardSave.cardType}, Uses: {cardSave.currentUses}/{cardSave.maxUses}, Slot: {cardSave.slotIndex})");
                
                TarotCardData card = FindOrCreateCardData(cardSave);
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
                
                TarotCardData card = FindOrCreateCardData(cardSave);
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
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load inventory: {e.Message}");
        }
    }
    
    private TarotCardData FindOrCreateCardData(CardSaveData saveData)
    {
        // Try to find existing card data first
        if (PlayerStats.instance != null)
        {
            var existingCard = PlayerStats.instance.ownedCards.FirstOrDefault(c => 
                c.cardName == saveData.cardName && 
                c.cardType == saveData.cardType &&
                c.currentUses == saveData.currentUses);
            
            if (existingCard != null) return existingCard;
        }
        
        // Create new card data if not found
        TarotCardData newCard = ScriptableObject.CreateInstance<TarotCardData>();
        newCard.cardName = saveData.cardName;
        newCard.cardType = saveData.cardType;
        newCard.currentUses = saveData.currentUses;
        newCard.maxUses = saveData.maxUses;
        
        // Try to find and assign material
        if (!string.IsNullOrEmpty(saveData.materialName))
        {
            // You might want to implement a material lookup system here
            // For now, create a basic material
            MaterialData material = ScriptableObject.CreateInstance<MaterialData>();
            material.materialName = saveData.materialName;
            material.maxUses = saveData.maxUses;
            newCard.AssignMaterial(material);
        }
        
        return newCard;
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
    public int slotIndex;
}
