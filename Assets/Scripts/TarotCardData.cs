using UnityEngine;

// Define the different card suits
public enum CardSuit
{
    Hearts,    // 0-12
    Diamonds,  // 13-25
    Clubs,     // 26-38
    Spades     // 39-51
}

[CreateAssetMenu(fileName = "NewTarotCard", menuName = "BlackJack/Tarot Card", order = 1)]
public class TarotCardData : ScriptableObject
{
    [Header("Card Identity")]
    public string cardName;
    public Sprite cardImage;
    
    [Header("Shop Details")]
    public int price = 100;
    [TextArea(2, 5)]
    public string description;
    
    [Header("Gameplay")]
    public TarotCardType cardType;
    public bool isReusable = false; // If true, can be used multiple times per round
    
    // Animation settings (optional)
    [Header("Animation")]
    public float animationDuration = 0.5f;
    public AnimationCurve animationCurve;
}

// Define the different types of tarot cards
public enum TarotCardType
{
    Peek,       // Peek at dealer's card
    Discard,    // Discard a card from hand (no tokens needed)
    Transform,   // Transform one card into another
    WitchDoctor, // NEW: Rescues 10% of bet if you lose
    Artificer,   // NEW: Boosts win multiplier by 10% if streak is active
    Botanist,    // NEW: Adds +50 bonus per clover in winning hand
    Assassin,    // NEW: Adds +50 bonus per spade in winning hand
    SecretLover, // NEW: Adds +50 bonus per heart in winning hand
    Jeweler,     // NEW: Adds +50 bonus per diamond in winning hand
    Scavenger,   // NEW: Removes all cards with value < 7 from player's hand
    Gardener,    // NEW: Removes all club cards from both player and dealer hands
    BetrayedCouple, // NEW: Removes all heart cards from both player and dealer hands
    Blacksmith,  // NEW: Removes all spade cards from both player and dealer hands
    TaxCollector, // NEW: Removes all diamond cards from both player and dealer hands
    HouseKeeper, // NEW: Adds +10 bonus per Jack/Queen/King in player's winning hand
    
    // NEW PREVIEW CARDS - Allow peeking at future cards
    Spy,         // NEW: Allows to peek at the next enemy card (dealer's next card)
    BlindSeer,   // NEW: Allows to see the next cards to be played from dealer's hand  
    CorruptJudge, // NEW: Peek into the next three cards in your hand and rearrange the first two if desired
    Hitman,      // NEW: Peek into the first three cards on your deck and remove one at discretion from play
    FortuneTeller, // NEW: Take a peek into the next two cards on your deck
    MadWriter    // NEW: Take a look at the next card and shuffle the whole deck if desired
} 