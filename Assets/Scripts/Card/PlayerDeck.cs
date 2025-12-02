using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a card in the player's 30-card deck
/// </summary>
[System.Serializable]
public class PlayerDeckCard
{
    public int id;                  // Unique identifier
    public int value;               // Card value (1=Ace, 2-10, 11=J, 12=Q, 13=K)
    public CardSuit suit;           // Card suit
    public Sprite cardSprite;       // Visual representation
    public bool isDealt;            // Has this card been dealt?
    public bool isActionCard;       // Is this an action card?
    public bool isBlankSlot;        // Is this a blank slot for pickups?
    public string displayName;      // Human readable name
    
    public PlayerDeckCard(int id, int value, CardSuit suit, Sprite sprite, string name, bool isAction = false, bool isBlank = false)
    {
        this.id = id;
        this.value = value;
        this.suit = suit;
        this.cardSprite = sprite;
        this.displayName = name;
        this.isDealt = false;
        this.isActionCard = isAction;
        this.isBlankSlot = isBlank;
    }
    
    /// <summary>
    /// Get the blackjack value of this card (Aces=1 or 11, Face cards=10)
    /// </summary>
    public int GetBlackjackValue()
    {
        if (isActionCard || isBlankSlot) return 0;
        if (value == 1) return 1; // Ace (can be 11 in game logic)
        if (value >= 11) return 10; // Face cards
        return value;
    }
    
    /// <summary>
    /// Get the rank name (A, 2-10, J, Q, K)
    /// </summary>
    public string GetRankName()
    {
        if (isBlankSlot) return "Blank";
        if (isActionCard) return "Action";
        
        switch (value)
        {
            case 1: return "A";
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            default: return value.ToString();
        }
    }
    
    /// <summary>
    /// Check if this is a face card (J, Q, K)
    /// </summary>
    public bool IsFaceCard() => value >= 11 && value <= 13;
    
    /// <summary>
    /// Check if this is a numbered card (2-10)
    /// </summary>
    public bool IsNumberedCard() => value >= 2 && value <= 10;
    
    /// <summary>
    /// Check if this is an Ace
    /// </summary>
    public bool IsAce() => value == 1;
}

/// <summary>
/// Manages the player's 30-card deck for the deck inspector system
/// Deck Structure: 22 Value Cards + 6 Action Cards + 2 Blank slots
/// Value Distribution: 2 Aces, 8 low cards (2-6), 8 mid cards (7-9), 4 tens/faces
/// </summary>
public class PlayerDeck : MonoBehaviour
{
    [Header("Deck Configuration")]
    public const int TOTAL_CARDS = 30;
    public const int VALUE_CARDS = 22;
    public const int ACTION_CARDS = 6;
    public const int BLANK_SLOTS = 2;
    
    [Header("Card Sprites")]
    [Tooltip("Reference to the main Deck's faces array for card sprites")]
    public Sprite[] cardSprites; // Should be 52 sprites (standard deck)
    
    [Header("Action Card Sprites")]
    public Sprite actionCardSprite; // Sprite for action cards
    public Sprite blankSlotSprite;  // Sprite for blank slots
    
    [Header("Deck State")]
    [SerializeField] private List<PlayerDeckCard> allCards = new List<PlayerDeckCard>();
    [SerializeField] private List<PlayerDeckCard> drawPile = new List<PlayerDeckCard>();
    [SerializeField] private List<PlayerDeckCard> discardPile = new List<PlayerDeckCard>();
    
    // Events
    public System.Action OnDeckChanged;
    
    // Properties
    public List<PlayerDeckCard> AllCards => allCards;
    public List<PlayerDeckCard> DrawPile => drawPile;
    public List<PlayerDeckCard> DiscardPile => discardPile;
    public int RemainingCards => drawPile.Count;
    public int TotalCards => allCards.Count;
    
    private void Awake()
    {
        // Try to get card sprites from main Deck if not assigned
        if (cardSprites == null || cardSprites.Length == 0)
        {
            Deck mainDeck = FindObjectOfType<Deck>();
            if (mainDeck != null && mainDeck.faces != null)
            {
                cardSprites = mainDeck.faces;
            }
        }
    }
    
    private void Start()
    {
        InitializeDeck();
    }
    
    /// <summary>
    /// Initialize the 30-card deck with the specified distribution
    /// </summary>
    public void InitializeDeck()
    {
        allCards.Clear();
        int cardId = 0;
        
        // === VALUE CARDS (22 total) ===
        
        // Aces (2 cards) - one from two different suits
        AddValueCard(ref cardId, 1, CardSuit.Spades);   // Ace of Spades
        AddValueCard(ref cardId, 1, CardSuit.Hearts);   // Ace of Hearts
        
        // Low cards 2-6 (8 cards distributed)
        // 2 twos, 2 threes, 2 fours, 1 five, 1 six
        AddValueCard(ref cardId, 2, CardSuit.Clubs);
        AddValueCard(ref cardId, 2, CardSuit.Diamonds);
        AddValueCard(ref cardId, 3, CardSuit.Spades);
        AddValueCard(ref cardId, 3, CardSuit.Hearts);
        AddValueCard(ref cardId, 4, CardSuit.Clubs);
        AddValueCard(ref cardId, 4, CardSuit.Diamonds);
        AddValueCard(ref cardId, 5, CardSuit.Spades);
        AddValueCard(ref cardId, 6, CardSuit.Hearts);
        
        // Mid cards 7-9 (8 cards distributed)
        // 3 sevens, 3 eights, 2 nines
        AddValueCard(ref cardId, 7, CardSuit.Clubs);
        AddValueCard(ref cardId, 7, CardSuit.Diamonds);
        AddValueCard(ref cardId, 7, CardSuit.Spades);
        AddValueCard(ref cardId, 8, CardSuit.Hearts);
        AddValueCard(ref cardId, 8, CardSuit.Clubs);
        AddValueCard(ref cardId, 8, CardSuit.Diamonds);
        AddValueCard(ref cardId, 9, CardSuit.Spades);
        AddValueCard(ref cardId, 9, CardSuit.Hearts);
        
        // 10s and face cards (4 cards)
        // 1 ten, 1 jack, 1 queen, 1 king
        AddValueCard(ref cardId, 10, CardSuit.Clubs);
        AddValueCard(ref cardId, 11, CardSuit.Diamonds);  // Jack
        AddValueCard(ref cardId, 12, CardSuit.Spades);    // Queen
        AddValueCard(ref cardId, 13, CardSuit.Hearts);    // King
        
        // === ACTION CARDS (6 total) ===
        for (int i = 0; i < ACTION_CARDS; i++)
        {
            allCards.Add(new PlayerDeckCard(
                cardId++,
                0,
                CardSuit.Hearts, // Default suit for action cards
                actionCardSprite,
                $"Action Card {i + 1}",
                isAction: true
            ));
        }
        
        // === BLANK SLOTS (2 total) ===
        for (int i = 0; i < BLANK_SLOTS; i++)
        {
            allCards.Add(new PlayerDeckCard(
                cardId++,
                0,
                CardSuit.Hearts, // Default suit for blank slots
                blankSlotSprite,
                $"Blank Slot {i + 1}",
                isBlank: true
            ));
        }
        
        // Initialize draw pile with all cards
        ResetDrawPile();
        
        Debug.Log($"[PlayerDeck] Initialized with {allCards.Count} cards ({VALUE_CARDS} value, {ACTION_CARDS} action, {BLANK_SLOTS} blank)");
    }
    
    /// <summary>
    /// Add a value card to the deck
    /// </summary>
    private void AddValueCard(ref int cardId, int value, CardSuit suit)
    {
        Sprite sprite = GetCardSprite(value, suit);
        string name = GetCardName(value, suit);
        
        allCards.Add(new PlayerDeckCard(cardId++, value, suit, sprite, name));
    }
    
    /// <summary>
    /// Get the sprite for a card based on value and suit
    /// </summary>
    private Sprite GetCardSprite(int value, CardSuit suit)
    {
        if (cardSprites == null || cardSprites.Length < 52)
        {
            Debug.LogWarning("[PlayerDeck] Card sprites not properly assigned");
            return null;
        }
        
        // Convert value to suit index (0-12: A, 2-10, J, Q, K)
        int suitIndex;
        if (value == 1) suitIndex = 0;        // Ace
        else if (value <= 10) suitIndex = value - 1; // 2-10
        else suitIndex = value - 1;           // J=10, Q=11, K=12
        
        // Calculate deck index: suit * 13 + suitIndex
        int deckIndex = (int)suit * 13 + suitIndex;
        
        if (deckIndex >= 0 && deckIndex < cardSprites.Length)
        {
            return cardSprites[deckIndex];
        }
        
        return null;
    }
    
    /// <summary>
    /// Get the display name for a card
    /// </summary>
    private string GetCardName(int value, CardSuit suit)
    {
        string valueName;
        switch (value)
        {
            case 1: valueName = "Ace"; break;
            case 11: valueName = "Jack"; break;
            case 12: valueName = "Queen"; break;
            case 13: valueName = "King"; break;
            default: valueName = value.ToString(); break;
        }
        
        return $"{valueName} of {suit}";
    }
    
    /// <summary>
    /// Reset the draw pile with all cards and shuffle
    /// </summary>
    public void ResetDrawPile()
    {
        drawPile.Clear();
        discardPile.Clear();
        
        foreach (var card in allCards)
        {
            card.isDealt = false;
            drawPile.Add(card);
        }
        
        ShuffleDeck();
        OnDeckChanged?.Invoke();
    }
    
    /// <summary>
    /// Shuffle the draw pile using Fisher-Yates algorithm
    /// </summary>
    public void ShuffleDeck()
    {
        System.Random rng = new System.Random();
        int n = drawPile.Count;
        
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var temp = drawPile[k];
            drawPile[k] = drawPile[n];
            drawPile[n] = temp;
        }
        
        Debug.Log("[PlayerDeck] Deck shuffled");
        OnDeckChanged?.Invoke();
    }
    
    /// <summary>
    /// Draw a card from the deck
    /// </summary>
    public PlayerDeckCard DrawCard()
    {
        if (drawPile.Count == 0)
        {
            Debug.LogWarning("[PlayerDeck] No cards left to draw!");
            return null;
        }
        
        PlayerDeckCard card = drawPile[0];
        drawPile.RemoveAt(0);
        card.isDealt = true;
        
        OnDeckChanged?.Invoke();
        return card;
    }
    
    /// <summary>
    /// Discard a card
    /// </summary>
    public void DiscardCard(PlayerDeckCard card)
    {
        if (card == null) return;
        
        discardPile.Add(card);
        OnDeckChanged?.Invoke();
    }
    
    /// <summary>
    /// Reshuffle discard pile back into draw pile
    /// </summary>
    public void ReshuffleDiscardPile()
    {
        foreach (var card in discardPile)
        {
            card.isDealt = false;
            drawPile.Add(card);
        }
        discardPile.Clear();
        
        ShuffleDeck();
    }
    
    // ============ COUNTING METHODS FOR UI ============
    
    /// <summary>
    /// Get count of cards matching a filter
    /// </summary>
    public int GetCount(System.Func<PlayerDeckCard, bool> filter, bool remainingOnly = false)
    {
        var source = remainingOnly ? drawPile : allCards;
        return source.Count(filter);
    }
    
    /// <summary>
    /// Get Aces count
    /// </summary>
    public int GetAcesCount(bool remainingOnly = false) => 
        GetCount(c => c.IsAce() && !c.isActionCard && !c.isBlankSlot, remainingOnly);
    
    /// <summary>
    /// Get KQJ (face cards) count
    /// </summary>
    public int GetFaceCardsCount(bool remainingOnly = false) => 
        GetCount(c => c.IsFaceCard() && !c.isActionCard && !c.isBlankSlot, remainingOnly);
    
    /// <summary>
    /// Get numbered cards (2-10) count
    /// </summary>
    public int GetNumberedCardsCount(bool remainingOnly = false) => 
        GetCount(c => c.IsNumberedCard() && !c.isActionCard && !c.isBlankSlot, remainingOnly);
    
    /// <summary>
    /// Get count by suit
    /// </summary>
    public int GetSuitCount(CardSuit suit, bool remainingOnly = false) => 
        GetCount(c => c.suit == suit && !c.isActionCard && !c.isBlankSlot, remainingOnly);
    
    /// <summary>
    /// Get count by specific rank (1=A, 2-10, 11=J, 12=Q, 13=K)
    /// </summary>
    public int GetRankCount(int rank, bool remainingOnly = false) => 
        GetCount(c => c.value == rank && !c.isActionCard && !c.isBlankSlot, remainingOnly);
    
    /// <summary>
    /// Get action cards count
    /// </summary>
    public int GetActionCardsCount(bool remainingOnly = false) => 
        GetCount(c => c.isActionCard, remainingOnly);
    
    /// <summary>
    /// Get blank slots count
    /// </summary>
    public int GetBlankSlotsCount(bool remainingOnly = false) => 
        GetCount(c => c.isBlankSlot, remainingOnly);
    
    /// <summary>
    /// Get value cards only (excludes action cards and blank slots)
    /// </summary>
    public List<PlayerDeckCard> GetValueCards(bool remainingOnly = false)
    {
        var source = remainingOnly ? drawPile : allCards;
        return source.Where(c => !c.isActionCard && !c.isBlankSlot).ToList();
    }
    
    /// <summary>
    /// Get action cards only
    /// </summary>
    public List<PlayerDeckCard> GetActionCards(bool remainingOnly = false)
    {
        var source = remainingOnly ? drawPile : allCards;
        return source.Where(c => c.isActionCard).ToList();
    }
    
    /// <summary>
    /// Get cards for display based on filter type
    /// </summary>
    public List<PlayerDeckCard> GetCardsForDisplay(DeckFilterType filter)
    {
        switch (filter)
        {
            case DeckFilterType.Remaining:
                return drawPile.ToList();
            case DeckFilterType.FullDeck:
                return allCards.ToList();
            case DeckFilterType.ActionCards:
                return allCards.Where(c => c.isActionCard).ToList();
            default:
                return drawPile.ToList();
        }
    }
}

/// <summary>
/// Filter types for the deck inspector
/// </summary>
public enum DeckFilterType
{
    Remaining,
    FullDeck,
    ActionCards
}

