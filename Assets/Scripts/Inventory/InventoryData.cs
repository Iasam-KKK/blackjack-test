using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInventoryData", menuName = "BlackJack/Inventory Data", order = 3)]
public class InventoryData : ScriptableObject
{
    [Header("Inventory Configuration")]
    public int storageSlotCount = 16; // Number of storage slots
    public int equipmentSlotCount = 3; // Number of equipment slots
    
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
        if (storageSlotIndex < 0 || storageSlotIndex >= storageSlots.Count) return false;
        if (equipmentSlotIndex < 0 || equipmentSlotIndex >= equipmentSlots.Count) return false;
        
        var storageSlot = storageSlots[storageSlotIndex];
        var equipmentSlot = equipmentSlots[equipmentSlotIndex];
        
        if (!storageSlot.isOccupied || equipmentSlot.isOccupied) return false;
        if (!storageSlot.storedCard.CanBeUsed()) return false; // Can't equip used up cards
        
        TarotCardData card = storageSlot.RemoveCard();
        return equipmentSlot.StoreCard(card);
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
