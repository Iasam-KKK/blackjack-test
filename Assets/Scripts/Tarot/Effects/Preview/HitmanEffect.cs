using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The Hitman - Peek into the first three cards on your deck and remove one at discretion from play.
/// Preview effect that allows card removal.
/// </summary>
public class HitmanEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.Hitman;
    
    private const int CARDS_TO_PREVIEW = 3;
    private const int MAX_CARDS_TO_REMOVE = 1;
    
    // Stored references for callback
    private TarotEffectContext _storedContext;
    private List<CardInfo> _originalCards;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already used this round
        if (context.deck._hasUsedHitmanThisRound)
        {
            return false;
        }
        
        // Check if there are cards remaining in deck
        if (context.deck.CardIndex >= context.deck.values.Length)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck._hasUsedHitmanThisRound)
        {
            return "Hitman ability already used this round";
        }
        
        if (context.deck.CardIndex >= context.deck.values.Length)
        {
            return "No more cards in deck";
        }
        
        return string.Empty;
    }
    
    public override bool Execute(TarotEffectContext context)
    {
        if (!CanExecute(context))
        {
            LogWarning(GetCannotExecuteReason(context));
            return false;
        }
        
        // Mark as used
        context.deck._hasUsedHitmanThisRound = true;
        
        // Store context for callback
        _storedContext = context;
        
        // Get next 3 cards from deck
        _originalCards = new List<CardInfo>();
        int cardsToShow = Mathf.Min(CARDS_TO_PREVIEW, context.deck.values.Length - context.deck.CardIndex);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            int index = context.deck.CardIndex + i;
            if (index < context.deck.values.Length)
            {
                _originalCards.Add(context.deck.GetCardInfo(index));
            }
        }
        
        // Show preview using CardPreviewManager with remove capability
        if (_originalCards.Count > 0 && context.cardPreviewManager != null)
        {
            Log($"Showing preview for {_originalCards.Count} cards with removal option");
            context.cardPreviewManager.ShowPreview(
                _originalCards,
                "The Hitman - Remove One Card",
                false, // No rearranging
                true,  // Allow removing
                MAX_CARDS_TO_REMOVE,
                OnConfirm,
                null // Cancel callback
            );
        }
        else
        {
            LogWarning("No cards to show or CardPreviewManager not found");
            return false;
        }
        
        Log("Effect executed - showing card removal preview");
        return true;
    }
    
    /// <summary>
    /// Called when player confirms the removal
    /// </summary>
    private void OnConfirm(List<CardInfo> remainingCards)
    {
        if (_storedContext == null || _storedContext.deck == null || _originalCards == null)
        {
            LogError("Context or original cards lost during confirmation");
            return;
        }
        
        // Find which card was removed
        if (remainingCards.Count < _originalCards.Count)
        {
            Deck deck = _storedContext.deck;
            
            // Find the removed card
            for (int i = 0; i < _originalCards.Count; i++)
            {
                bool cardStillExists = false;
                foreach (var remaining in remainingCards)
                {
                    if (remaining.index == _originalCards[i].index)
                    {
                        cardStillExists = true;
                        break;
                    }
                }
                
                if (!cardStillExists)
                {
                    // Remove this card from the deck
                    int indexToRemove = deck.CardIndex + i;
                    RemoveCardFromDeck(deck, indexToRemove);
                    Log($"Removed: {_originalCards[i].cardName}");
                    break;
                }
            }
        }
        
        _storedContext = null;
        _originalCards = null;
    }
    
    /// <summary>
    /// Remove a card from the deck at the specified index
    /// </summary>
    private void RemoveCardFromDeck(Deck deck, int indexToRemove)
    {
        if (indexToRemove < 0 || indexToRemove >= deck.values.Length) return;
        
        // Shift all cards after the removed index forward
        for (int i = indexToRemove; i < deck.values.Length - 1; i++)
        {
            deck.values[i] = deck.values[i + 1];
            deck.faces[i] = deck.faces[i + 1];
            deck.originalIndices[i] = deck.originalIndices[i + 1];
        }
        
        // Create new smaller arrays
        int[] newValues = new int[deck.values.Length - 1];
        Sprite[] newFaces = new Sprite[deck.faces.Length - 1];
        int[] newOriginalIndices = new int[deck.originalIndices.Length - 1];
        
        System.Array.Copy(deck.values, newValues, newValues.Length);
        System.Array.Copy(deck.faces, newFaces, newFaces.Length);
        System.Array.Copy(deck.originalIndices, newOriginalIndices, newOriginalIndices.Length);
        
        deck.values = newValues;
        deck.faces = newFaces;
        deck.originalIndices = newOriginalIndices;
    }
}

