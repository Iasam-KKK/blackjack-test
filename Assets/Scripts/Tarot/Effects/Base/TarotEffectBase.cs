using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Abstract base class for all tarot card effects.
/// Provides common functionality and utility methods for derived effects.
/// </summary>
public abstract class TarotEffectBase : ITarotEffect
{
    /// <summary>
    /// The type of tarot card this effect belongs to
    /// </summary>
    public abstract TarotCardType EffectType { get; }
    
    /// <summary>
    /// Whether this effect requires coroutine execution (has animations)
    /// Override in derived classes that need coroutine execution
    /// </summary>
    public virtual bool RequiresCoroutine => false;
    
    /// <summary>
    /// Check if the effect can be executed given current game state
    /// Override in derived classes to add specific conditions
    /// </summary>
    public virtual bool CanExecute(TarotEffectContext context)
    {
        if (context == null || context.deck == null)
        {
            Debug.LogError($"[{GetType().Name}] Invalid context - deck is null");
            return false;
        }
        
        // Basic check: bet must be placed for most effects
        if (!context.deck._isBetPlaced)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Execute the effect synchronously.
    /// Override in derived classes to implement the effect logic.
    /// </summary>
    public abstract bool Execute(TarotEffectContext context);
    
    /// <summary>
    /// Execute the effect as a coroutine.
    /// Override in derived classes that need animation/timing.
    /// Default implementation just calls Execute synchronously.
    /// </summary>
    public virtual IEnumerator ExecuteCoroutine(TarotEffectContext context)
    {
        Execute(context);
        yield return null;
    }
    
    /// <summary>
    /// Get the reason why the effect cannot be executed.
    /// Override in derived classes for specific error messages.
    /// </summary>
    public virtual string GetCannotExecuteReason(TarotEffectContext context)
    {
        if (context == null || context.deck == null)
        {
            return "Game not initialized";
        }
        
        if (!context.deck._isBetPlaced)
        {
            return "Place a bet first";
        }
        
        return string.Empty;
    }
    
    #region Utility Methods for Derived Classes
    
    /// <summary>
    /// Log a debug message with the effect name prefix
    /// </summary>
    protected void Log(string message)
    {
        Debug.Log($"[{EffectType}] {message}");
    }
    
    /// <summary>
    /// Log a warning with the effect name prefix
    /// </summary>
    protected void LogWarning(string message)
    {
        Debug.LogWarning($"[{EffectType}] {message}");
    }
    
    /// <summary>
    /// Log an error with the effect name prefix
    /// </summary>
    protected void LogError(string message)
    {
        Debug.LogError($"[{EffectType}] {message}");
    }
    
    /// <summary>
    /// Get cards from the deck starting at current index
    /// </summary>
    protected List<CardInfo> GetNextCardsFromDeck(TarotEffectContext context, int count)
    {
        List<CardInfo> cards = new List<CardInfo>();
        Deck deck = context.deck;
        
        int cardsToGet = Mathf.Min(count, deck.values.Length - deck.CardIndex);
        
        for (int i = 0; i < cardsToGet; i++)
        {
            int index = deck.CardIndex + i;
            if (index < deck.values.Length)
            {
                cards.Add(deck.GetCardInfo(index));
            }
        }
        
        return cards;
    }
    
    /// <summary>
    /// Get all cards of a specific suit from a hand
    /// </summary>
    protected List<CardInfo> GetCardsBySuit(TarotEffectContext context, CardHand hand, CardSuit suit)
    {
        List<CardInfo> suitCards = new List<CardInfo>();
        
        if (hand == null || hand.cards == null) return suitCards;
        
        foreach (GameObject cardObj in hand.cards)
        {
            CardModel cardModel = cardObj.GetComponent<CardModel>();
            if (cardModel != null)
            {
                CardInfo cardInfo = context.deck.GetCardInfoFromModel(cardModel);
                if (cardInfo.suit == suit)
                {
                    suitCards.Add(cardInfo);
                }
            }
        }
        
        return suitCards;
    }
    
    /// <summary>
    /// Count cards of a specific suit in the player's hand
    /// </summary>
    protected int CountPlayerCardsBySuit(TarotEffectContext context, CardSuit suit)
    {
        return GetCardsBySuit(context, context.playerHand, suit).Count;
    }
    
    /// <summary>
    /// Check if the game is currently in progress
    /// </summary>
    protected bool IsGameInProgress(TarotEffectContext context)
    {
        return context.deck != null && context.deck._gameInProgress;
    }
    
    /// <summary>
    /// Check if it's currently the player's turn
    /// </summary>
    protected bool IsPlayerTurn(TarotEffectContext context)
    {
        return context.deck != null && context.deck._currentTurn == Deck.GameTurn.Player;
    }
    
    /// <summary>
    /// Get the current player score
    /// </summary>
    protected int GetPlayerScore(TarotEffectContext context)
    {
        return context.deck?.GetPlayerPoints() ?? 0;
    }
    
    /// <summary>
    /// Get the current dealer score
    /// </summary>
    protected int GetDealerScore(TarotEffectContext context)
    {
        return context.deck?.GetDealerPoints() ?? 0;
    }
    
    /// <summary>
    /// Show a feedback message to the player
    /// </summary>
    protected void ShowMessage(TarotEffectContext context, string message, float duration = 1.5f)
    {
        context.ShowMessage(message, duration);
    }
    
    /// <summary>
    /// Update all score displays after an effect
    /// </summary>
    protected void UpdateScoreDisplays(TarotEffectContext context)
    {
        context.deck?.UpdateScoreDisplays();
    }
    
    /// <summary>
    /// Update all button states after an effect
    /// </summary>
    protected void UpdateButtonStates(TarotEffectContext context)
    {
        if (context.deck != null)
        {
            context.deck.UpdateDiscardButtonState();
            context.deck.UpdatePeekButtonState();
            context.deck.UpdateTransformButtonState();
        }
    }
    
    #endregion
}

