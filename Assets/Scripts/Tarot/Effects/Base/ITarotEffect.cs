using UnityEngine;
using System.Collections;

/// <summary>
/// Interface for all tarot card effects.
/// Each tarot card type should have its own implementation of this interface.
/// </summary>
public interface ITarotEffect
{
    /// <summary>
    /// The type of tarot card this effect belongs to
    /// </summary>
    TarotCardType EffectType { get; }
    
    /// <summary>
    /// Check if the effect can be executed given current game state
    /// </summary>
    /// <param name="context">The context containing references to game objects</param>
    /// <returns>True if the effect can be executed</returns>
    bool CanExecute(TarotEffectContext context);
    
    /// <summary>
    /// Execute the effect synchronously (for simple effects)
    /// </summary>
    /// <param name="context">The context containing references to game objects</param>
    /// <returns>True if the effect was successfully executed</returns>
    bool Execute(TarotEffectContext context);
    
    /// <summary>
    /// Execute the effect as a coroutine (for effects with animations)
    /// </summary>
    /// <param name="context">The context containing references to game objects</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    IEnumerator ExecuteCoroutine(TarotEffectContext context);
    
    /// <summary>
    /// Whether this effect requires coroutine execution (has animations)
    /// </summary>
    bool RequiresCoroutine { get; }
    
    /// <summary>
    /// Get the reason why the effect cannot be executed (for UI feedback)
    /// </summary>
    /// <param name="context">The context containing references to game objects</param>
    /// <returns>Error message or empty string if can execute</returns>
    string GetCannotExecuteReason(TarotEffectContext context);
}

/// <summary>
/// Context object containing all references needed by tarot effects.
/// This decouples the effects from the Deck class directly.
/// </summary>
[System.Serializable]
public class TarotEffectContext
{
    // Core game references
    public Deck deck;
    public GameObject player;
    public GameObject dealer;
    public CardHand playerHand;
    public CardHand dealerHand;
    
    // Managers
    public BossManager bossManager;
    public CardPreviewManager cardPreviewManager;
    public GameProgressionManager progressionManager;
    
    // UI references (for feedback messages)
    public UnityEngine.UI.Text finalMessage;
    
    // Card-specific context (optional, set by caller)
    public CardModel selectedCard;
    public TarotCardData cardData;
    
    /// <summary>
    /// Create a context from the Deck instance
    /// </summary>
    public static TarotEffectContext FromDeck(Deck deck)
    {
        if (deck == null) return null;
        
        return new TarotEffectContext
        {
            deck = deck,
            player = deck.player,
            dealer = deck.dealer,
            playerHand = deck.player?.GetComponent<CardHand>(),
            dealerHand = deck.dealer?.GetComponent<CardHand>(),
            bossManager = deck.bossManager,
            cardPreviewManager = deck.cardPreviewManager,
            progressionManager = GameProgressionManager.Instance,
            finalMessage = deck.finalMessage
        };
    }
    
    /// <summary>
    /// Show a temporary message to the player
    /// </summary>
    public void ShowMessage(string message, float duration = 1.5f)
    {
        if (finalMessage != null)
        {
            finalMessage.text = message;
            finalMessage.gameObject.SetActive(true);
            
            if (deck != null && duration > 0)
            {
                deck.StartCoroutine(ClearMessageAfterDelay(duration));
            }
        }
    }
    
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (finalMessage != null && deck._gameInProgress)
        {
            finalMessage.text = "";
        }
    }
}

