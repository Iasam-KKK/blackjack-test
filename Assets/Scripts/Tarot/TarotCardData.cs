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
    
    [Header("Material System")]
    public MaterialData assignedMaterial; // The material assigned to this card instance
    public int currentUses = 0; // Track how many times this card has been used
    [HideInInspector]
    public int maxUses = 1; // Will be set based on assigned material
    
    [Header("Gameplay")]
    public TarotCardType cardType;
    public bool isReusable = false; // If true, can be used multiple times per round
    
    // Check if this card type should be reusable (passive cards that activate once per round)
    public bool IsPassiveCard()
    {
        return cardType == TarotCardType.Botanist ||
               cardType == TarotCardType.Assassin ||
               cardType == TarotCardType.SecretLover ||
               cardType == TarotCardType.Jeweler ||
               cardType == TarotCardType.HouseKeeper ||
               cardType == TarotCardType.WitchDoctor ||
               cardType == TarotCardType.Artificer;
    }
    
    // Animation settings (optional)
    [Header("Animation")]
    public float animationDuration = 0.5f;
    public AnimationCurve animationCurve;
    
    // Initialize material data when the card is created or material is assigned
    public void InitializeMaterial()
    {
        if (assignedMaterial != null)
        {
            maxUses = assignedMaterial.maxUses;
            currentUses = 0;
        }
        else
        {
            maxUses = 1; // Default to single use if no material assigned
            currentUses = 0;
        }
    }
    
    // Assign a material to this card
    public void AssignMaterial(MaterialData material)
    {
        assignedMaterial = material;
        InitializeMaterial();
    }
    
    // Check if the card can still be used based on material durability
    public bool CanBeUsed()
    {
        if (assignedMaterial == null) return currentUses < 1; // Default single use
        if (assignedMaterial.HasUnlimitedUses()) return true; // Unlimited uses (Diamond)
        return currentUses < maxUses;
    }
    
    // Use the card (increment usage counter)
    public bool UseCard()
    {
        if (!CanBeUsed()) return false;
        
        if (assignedMaterial == null || !assignedMaterial.HasUnlimitedUses())
        {
            currentUses++;
        }
        return true;
    }
    
    // Reset usage for new round (but keep material durability)
    public void ResetForNewRound()
    {
        // Note: We don't reset currentUses here because material durability persists across rounds
        // Only the isReusable flag affects per-round usage
    }
    
    // Get remaining uses
    public int GetRemainingUses()
    {
        if (assignedMaterial == null) return Mathf.Max(0, 1 - currentUses); // Default single use
        if (assignedMaterial.HasUnlimitedUses()) return -1; // Unlimited
        return Mathf.Max(0, maxUses - currentUses);
    }
    
    // Get material display info
    public string GetMaterialDisplayName()
    {
        if (assignedMaterial == null) return "No Material";
        return assignedMaterial.GetDisplayName();
    }
    
    // Get material color
    public Color GetMaterialColor()
    {
        if (assignedMaterial == null) return Color.white;
        return assignedMaterial.GetMaterialColor();
    }
    
    // Get material background sprite
    public Sprite GetMaterialBackgroundSprite()
    {
        if (assignedMaterial == null) return null;
        return assignedMaterial.backgroundSprite;
    }
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
    MakeupArtist, //Allows to select a card and allow to pass it for another
    CursedHourglass, //Lose half of your bet, but temporarily discard all cards in play from both sides
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
    MadWriter,    // NEW: Take a look at the next card and shuffle the whole deck if desired
    WhisperOfThePast, //Lose ¼ of your bet, temporarily discard all cards from your played side
    Saboteur,     //Lose ¼ of your bet, temporarily discard all cards from the enemy's played side
    Scammer,      //Reverts the enemy's winning play discarding the played card at the cost of half the bet
    TheEscapist   //Prevents losing by destroying the last card hit by player, then destroys itself
} 