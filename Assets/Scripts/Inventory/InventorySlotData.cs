using UnityEngine;

[System.Serializable]
public class InventorySlotData
{
    [Header("Slot Configuration")]
    public int slotIndex;
    public bool isEquipmentSlot; // True if this is an equipment slot, false if storage
    
    [Header("Card Data")]
    public TarotCardData storedCard; // The card stored in this slot
    public bool isOccupied; // Whether this slot contains a card
    
    public InventorySlotData(int index, bool isEquipment = false)
    {
        slotIndex = index;
        isEquipmentSlot = isEquipment;
        storedCard = null;
        isOccupied = false;
    }
    
    // Store a card in this slot
    public bool StoreCard(TarotCardData card)
    {
        if (isOccupied || card == null) return false;
        
        storedCard = card;
        isOccupied = true;
        return true;
    }
    
    // Remove the card from this slot
    public TarotCardData RemoveCard()
    {
        if (!isOccupied) return null;
        
        TarotCardData removedCard = storedCard;
        storedCard = null;
        isOccupied = false;
        return removedCard;
    }
    
    // Check if the slot can accept a card
    public bool CanAcceptCard(TarotCardData card)
    {
        if (card == null || isOccupied) return false;
        
        // Equipment slots can only accept cards with remaining durability
        if (isEquipmentSlot && !card.CanBeUsed()) return false;
        
        return true;
    }
    
    // Get display info for the slot
    public string GetSlotDisplayInfo()
    {
        if (!isOccupied) return isEquipmentSlot ? "Equipment Slot" : "Storage Slot";
        
        string cardInfo = $"{storedCard.cardName} ({storedCard.GetMaterialDisplayName()})";
        if (storedCard.CanBeUsed())
        {
            int remainingUses = storedCard.GetRemainingUses();
            cardInfo += remainingUses == -1 ? " - Unlimited" : $" - {remainingUses} uses";
        }
        else
        {
            cardInfo += " - No uses left";
        }
        
        return cardInfo;
    }
}
