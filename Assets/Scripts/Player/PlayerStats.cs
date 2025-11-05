using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;
    public List<TarotCardData> ownedCards;

    void Awake()
    {
        instance = this;
    }

    public bool PlayerHasCard(TarotCardType type)
    {
        return ownedCards.Any(card => card.cardType == type);
    }
    
    // New method to check if player has an equipped card of the specified type
    public bool PlayerHasEquippedCard(TarotCardType type)
    {
        if (InventoryManager.Instance != null)
        {
            var equippedCards = InventoryManager.Instance.GetEquippedUsableCards();
            return equippedCards.Any(card => card.cardType == type);
        }
        
        // Fallback to old system if inventory manager not available
        return PlayerHasCard(type);
    }
}