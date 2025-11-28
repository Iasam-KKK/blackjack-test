using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInventoryData", menuName = "BlackJack/Inventory Data", order = 3)]
public class InventoryData : ScriptableObject
{
    [Header("Inventory Configuration")]
    public int storageSlotCount = 16; // Number of storage slots
    public int equipmentSlotCount = 4; // Number of equipment slots
    
    [Header("Current Inventory State")]
    public List<InventorySlotData> storageSlots = new List<InventorySlotData>();
    public List<InventorySlotData> equipmentSlots = new List<InventorySlotData>();
    
    private void OnEnable()
    {
        InitializeSlots();
    }
    
    // Initialize all slots if they haven't been created yet
    public void InitializeSlots()
    {
        // Initialize storage slots
        if (storageSlots.Count != storageSlotCount)
        {
            storageSlots.Clear();
            for (int i = 0; i < storageSlotCount; i++)
            {
                storageSlots.Add(new InventorySlotData(i, false));
            }
        }
        
        // Initialize equipment slots
        if (equipmentSlots.Count != equipmentSlotCount)
        {
            equipmentSlots.Clear();
            for (int i = 0; i < equipmentSlotCount; i++)
            {
                equipmentSlots.Add(new InventorySlotData(i, true));
            }
        }
        
        // FIX: Fix any data inconsistencies in existing equipment slots
        foreach (var slot in equipmentSlots)
        {
            if (slot != null)
            {
                slot.FixSlotState();
            }
        }
    }
    
    // Add a card to the first available storage slot
    public bool AddCardToStorage(TarotCardData card)
    {
        if (card == null) return false;
        
        foreach (var slot in storageSlots)
        {
            if (slot.CanAcceptCard(card))
            {
                return slot.StoreCard(card);
            }
        }
        return false; // No empty slots
    }
    
    // Equip a card to the first available equipment slot
    public bool EquipCard(TarotCardData card)
    {
        if (card == null || !card.CanBeUsed()) return false;
        
        foreach (var slot in equipmentSlots)
        {
            if (slot.CanAcceptCard(card))
            {
                return slot.StoreCard(card);
            }
        }
        return false; // No empty equipment slots
    }
    
    // Unequip a card from equipment slot and move to storage
    public bool UnequipCard(int equipmentSlotIndex)
    {
        if (equipmentSlotIndex < 0 || equipmentSlotIndex >= equipmentSlots.Count) return false;
        
        var equipmentSlot = equipmentSlots[equipmentSlotIndex];
        if (!equipmentSlot.isOccupied) return false;
        
        TarotCardData card = equipmentSlot.RemoveCard();
        return AddCardToStorage(card);
    }
    
    // Move card from storage to equipment
    public bool MoveCardToEquipment(int storageSlotIndex, int equipmentSlotIndex)
    {
        if (storageSlotIndex < 0 || storageSlotIndex >= storageSlots.Count)
        {
            Debug.LogError($"[InventoryData] MoveCardToEquipment: Invalid storage slot index {storageSlotIndex} (count: {storageSlots.Count})");
            return false;
        }
        if (equipmentSlotIndex < 0 || equipmentSlotIndex >= equipmentSlots.Count)
        {
            Debug.LogError($"[InventoryData] MoveCardToEquipment: Invalid equipment slot index {equipmentSlotIndex} (count: {equipmentSlots.Count})");
            return false;
        }
        
        var storageSlot = storageSlots[storageSlotIndex];
        var equipmentSlot = equipmentSlots[equipmentSlotIndex];
        
        if (storageSlot == null || equipmentSlot == null)
        {
            Debug.LogError($"[InventoryData] MoveCardToEquipment: Null slot - storage={storageSlot == null}, equipment={equipmentSlot == null}");
            return false;
        }
        
        // FIX: Fix any data inconsistencies in equipment slot first
        equipmentSlot.FixSlotState();
        
        // Check storage slot
        bool storageOccupied = storageSlot.isOccupied && storageSlot.storedCard != null;
        if (!storageOccupied)
        {
            Debug.LogWarning($"[InventoryData] MoveCardToEquipment: Storage slot {storageSlotIndex} is not occupied (isOccupied={storageSlot.isOccupied}, card={storageSlot.storedCard != null})");
            return false;
        }
        
        // Check equipment slot - must be empty (check both flag AND card)
        bool equipmentOccupied = equipmentSlot.isOccupied && equipmentSlot.storedCard != null;
        if (equipmentOccupied)
        {
            Debug.LogWarning($"[InventoryData] MoveCardToEquipment: Equipment slot {equipmentSlotIndex} is already occupied (isOccupied={equipmentSlot.isOccupied}, card={equipmentSlot.storedCard?.cardName ?? "null"})");
            return false;
        }
        
        // Check if card can be used
        if (!storageSlot.storedCard.CanBeUsed())
        {
            Debug.LogWarning($"[InventoryData] MoveCardToEquipment: Card {storageSlot.storedCard.cardName} cannot be used (used up)");
            return false;
        }
        
        TarotCardData card = storageSlot.RemoveCard();
        if (card == null)
        {
            Debug.LogError($"[InventoryData] MoveCardToEquipment: RemoveCard returned null from storage slot {storageSlotIndex}");
            return false;
        }
        
        bool storeSuccess = equipmentSlot.StoreCard(card);
        if (!storeSuccess)
        {
            Debug.LogError($"[InventoryData] MoveCardToEquipment: Failed to store card in equipment slot {equipmentSlotIndex} (isOccupied={equipmentSlot.isOccupied}, card={equipmentSlot.storedCard != null})");
            // Try to restore card to storage
            storageSlot.StoreCard(card);
            return false;
        }
        
        Debug.Log($"[InventoryData] MoveCardToEquipment: Successfully moved {card.cardName} from storage {storageSlotIndex} to equipment {equipmentSlotIndex}");
        
        // Compact storage slots to fill empty gaps
        CompactStorageSlots();
        
        return true;
    }
    
    /// <summary>
    /// Compact storage slots by shifting all cards to fill empty slots
    /// This ensures no gaps in the storage inventory
    /// </summary>
    public void CompactStorageSlots()
    {
        // Collect all cards from storage slots
        List<TarotCardData> cards = new List<TarotCardData>();
        foreach (var slot in storageSlots)
        {
            if (slot != null && slot.isOccupied && slot.storedCard != null)
            {
                cards.Add(slot.storedCard);
            }
        }
        
        // Clear all storage slots
        foreach (var slot in storageSlots)
        {
            if (slot != null)
            {
                slot.RemoveCard();
            }
        }
        
        // Re-add cards starting from index 0, filling empty slots
        for (int i = 0; i < cards.Count && i < storageSlots.Count; i++)
        {
            if (storageSlots[i] != null)
            {
                storageSlots[i].StoreCard(cards[i]);
            }
        }
        
        Debug.Log($"[InventoryData] CompactStorageSlots: Rearranged {cards.Count} cards to fill empty slots");
    }
    
    // Get all equipped cards that can be used
    public List<TarotCardData> GetEquippedUsableCards()
    {
        List<TarotCardData> usableCards = new List<TarotCardData>();
        
        foreach (var slot in equipmentSlots)
        {
            if (slot.isOccupied && slot.storedCard.CanBeUsed())
            {
                usableCards.Add(slot.storedCard);
            }
        }
        
        return usableCards;
    }
    
    // Get all cards in inventory (both storage and equipped)
    public List<TarotCardData> GetAllCards()
    {
        List<TarotCardData> allCards = new List<TarotCardData>();
        
        foreach (var slot in storageSlots)
        {
            if (slot.isOccupied) allCards.Add(slot.storedCard);
        }
        
        foreach (var slot in equipmentSlots)
        {
            if (slot.isOccupied) allCards.Add(slot.storedCard);
        }
        
        return allCards;
    }
    
    // Remove a card completely from inventory (when it's used up)
    public bool RemoveCard(TarotCardData card)
    {
        // Check storage slots
        foreach (var slot in storageSlots)
        {
            if (slot.isOccupied && slot.storedCard == card)
            {
                slot.RemoveCard();
                return true;
            }
        }
        
        // Check equipment slots
        foreach (var slot in equipmentSlots)
        {
            if (slot.isOccupied && slot.storedCard == card)
            {
                slot.RemoveCard();
                return true;
            }
        }
        
        return false;
    }
    
    // Get inventory statistics
    public InventoryStats GetInventoryStats()
    {
        int usedStorage = 0, usedEquipment = 0;
        int usableCards = 0, unusableCards = 0;
        
        foreach (var slot in storageSlots)
        {
            if (slot.isOccupied)
            {
                usedStorage++;
                if (slot.storedCard.CanBeUsed()) usableCards++;
                else unusableCards++;
            }
        }
        
        foreach (var slot in equipmentSlots)
        {
            if (slot.isOccupied)
            {
                usedEquipment++;
                if (slot.storedCard.CanBeUsed()) usableCards++;
                else unusableCards++;
            }
        }
        
        return new InventoryStats
        {
            storageUsed = usedStorage,
            storageTotal = storageSlotCount,
            equipmentUsed = usedEquipment,
            equipmentTotal = equipmentSlotCount,
            usableCards = usableCards,
            unusableCards = unusableCards
        };
    }
}

[System.Serializable]
public struct InventoryStats
{
    public int storageUsed;
    public int storageTotal;
    public int equipmentUsed;
    public int equipmentTotal;
    public int usableCards;
    public int unusableCards;
}
