using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static utility class for easy access to card-related functions across the project
/// </summary>
public static class CardUtils
{
    private static Deck _deckInstance;
    
    /// <summary>
    /// Get the current deck instance (automatically finds it if null)
    /// </summary>
    public static Deck DeckInstance
    {
        get
        {
            if (_deckInstance == null)
            {
                _deckInstance = Object.FindObjectOfType<Deck>();
            }
            return _deckInstance;
        }
    }
    
    /// <summary>
    /// Quick access to get card information by deck index
    /// </summary>
    public static CardInfo GetCard(int deckIndex)
    {
        return DeckInstance?.GetCardInfo(deckIndex) ?? new CardInfo();
    }
    
    /// <summary>
    /// Quick access to get card information by sprite
    /// </summary>
    public static CardInfo GetCard(Sprite cardSprite)
    {
        return DeckInstance?.GetCardInfoBySprite(cardSprite) ?? new CardInfo();
    }
    
    /// <summary>
    /// Quick access to get all cards of a specific suit
    /// </summary>
    public static List<CardInfo> GetSuitCards(CardSuit suit)
    {
        return DeckInstance?.GetCardsOfSuit(suit) ?? new List<CardInfo>();
    }
    
    /// <summary>
    /// Quick access to get player hand information
    /// </summary>
    public static List<CardInfo> GetPlayerHand()
    {
        return DeckInstance?.GetHandCardInfo(DeckInstance.player) ?? new List<CardInfo>();
    }
    
    /// <summary>
    /// Quick access to get dealer hand information
    /// </summary>
    public static List<CardInfo> GetDealerHand()
    {
        return DeckInstance?.GetHandCardInfo(DeckInstance.dealer) ?? new List<CardInfo>();
    }
    
    /// <summary>
    /// Quick access to get player suit counts
    /// </summary>
    public static Dictionary<CardSuit, int> GetPlayerSuitCounts()
    {
        return DeckInstance?.GetAllHandSuitCounts(DeckInstance.player) ?? new Dictionary<CardSuit, int>();
    }
    
    /// <summary>
    /// Quick access to check if player has a specific card
    /// </summary>
    public static bool PlayerHasCard(int value, CardSuit suit)
    {
        return DeckInstance?.HandContainsCard(DeckInstance.player, value, suit) ?? false;
    }
    
    /// <summary>
    /// Quick access to calculate all tarot bonuses for player hand
    /// </summary>
    public static uint CalculatePlayerTarotBonuses()
    {
        return DeckInstance?.CalculateSuitBonuses() ?? 0;
    }
    
    /// <summary>
    /// Quick access to get individual tarot bonus breakdown
    /// </summary>
    public static Dictionary<TarotCardType, uint> GetPlayerTarotBreakdown()
    {
        return DeckInstance?.GetSuitBonusBreakdown() ?? new Dictionary<TarotCardType, uint>();
    }
    
    /// <summary>
    /// Get card index from suit and position within suit
    /// </summary>
    public static int GetCardIndex(int suitPosition, CardSuit suit)
    {
        return DeckInstance?.GetCardIndex(suitPosition, suit) ?? -1;
    }
    
    /// <summary>
    /// Convert card index to readable string format
    /// </summary>
    public static string CardIndexToString(int deckIndex)
    {
        CardInfo card = GetCard(deckIndex);
        return card.cardName;
    }
    
    /// <summary>
    /// Get all cards in a specific value range (useful for face cards, etc.)
    /// </summary>
    public static List<CardInfo> GetCardsByValue(int value)
    {
        List<CardInfo> matchingCards = new List<CardInfo>();
        
        if (DeckInstance == null) return matchingCards;
        
        for (int i = 0; i < Constants.DeckCards; i++)
        {
            CardInfo card = GetCard(i);
            if (card.value == value)
            {
                matchingCards.Add(card);
            }
        }
        
        return matchingCards;
    }
    
    /// <summary>
    /// Check if current player hand has any cards of a specific suit
    /// </summary>
    public static bool PlayerHasAnySuit(CardSuit suit)
    {
        var suitCounts = GetPlayerSuitCounts();
        return suitCounts.ContainsKey(suit) && suitCounts[suit] > 0;
    }
    
    /// <summary>
    /// Get the most common suit in player's hand
    /// </summary>
    public static CardSuit GetPlayerDominantSuit()
    {
        var suitCounts = GetPlayerSuitCounts();
        return suitCounts.OrderByDescending(x => x.Value).FirstOrDefault().Key;
    }
    
    /// <summary>
    /// Debug method to print current player hand to console
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DebugPrintPlayerHand()
    {
        List<CardInfo> hand = GetPlayerHand();
        Debug.Log("=== PLAYER HAND ===");
        foreach (CardInfo card in hand)
        {
            Debug.Log(card.cardName + " (Index: " + card.index + ", Value: " + card.value + ")");
        }
        Debug.Log("=== END PLAYER HAND ===");
    }
} 