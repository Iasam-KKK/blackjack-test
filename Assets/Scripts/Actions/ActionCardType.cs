using UnityEngine;

/// <summary>
/// Types of action cards available in the game
/// </summary>
public enum ActionCardType
{
    None,
    SwapTwoCards,           // Swap positions/values of two cards in hand
    AddOneToCard,           // Add +1 to a selected card
    SubtractOneFromCard,    // Subtract -1 from a selected card
    PeekDealerCard,         // Peek at dealer's hidden card
    ForceRedraw,            // Discard a card and draw a new one
    DoubleCardValue,        // Double a card's value (capped at 10)
    SetCardToTen,           // Set any card to value 10
    FlipAce,                // Change Ace between 1 and 11
    ShieldCard,             // Protect a card from boss mechanics/curses
    CopyCard,               // Duplicate a card's value to another card
    
    // Low-impact action card modifiers
    ValuePlusOne,           // Add +1 to any card (including empty cards)
    MinorSwapWithDealer,    // Swap player card with dealer's face-up card
    MinorHeal               // +10 health points (limited to 3 uses per game)
}

/// <summary>
/// ScriptableObject for action card configuration
/// </summary>
[CreateAssetMenu(fileName = "NewActionCard", menuName = "BlackJack/Action Card", order = 5)]
public class ActionCardData : ScriptableObject
{
    [Header("Action Identity")]
    public string actionName;
    public ActionCardType actionType;
    public Sprite actionIcon;
    
    [TextArea(2, 4)]
    public string actionDescription;
    
    [Header("Action Properties")]
    public int actionsRequired = 1; // How many action points this costs (default 1)
    public bool canBeUsedMultipleTimes = false; // Can use multiple times per hand?
    
    [Header("Limited Use (Game-wide)")]
    [Tooltip("If true, this action has limited uses across the entire game (not just per hand)")]
    public bool hasLimitedGameUses = false;
    [Tooltip("Maximum times this action can be used in an entire game session")]
    public int maxGameUses = 3;
    
    [Header("Availability")]
    public int unlockLevel = 0; // Level required to unlock (0 = always available)
    public bool isStarterAction = true; // Available from game start?
    
    [Header("Visual")]
    public Color cardColor = Color.blue;
    public Sprite cardBackground;
}

