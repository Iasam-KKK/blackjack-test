using UnityEngine;

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
    Discard,    // Discard a card from hand
    Transform,   // Transform one card into another
    WitchDoctor, // NEW: Rescues 10% of bet if you lose
    Artificer     // NEW: Boosts win multiplier by 10% if streak is active
    
} 