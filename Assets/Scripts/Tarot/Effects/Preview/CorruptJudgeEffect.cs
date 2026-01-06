using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The Corrupt Judge - Peek into the next three cards in your hand and rearrange the first two if desired.
/// Preview effect that allows card reordering.
/// </summary>
public class CorruptJudgeEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.CorruptJudge;
    
    private const int CARDS_TO_PREVIEW = 3;
    private const int CARDS_TO_REARRANGE = 2;
    
    // Stored reference to context for callback
    private TarotEffectContext _storedContext;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already used this round
        if (context.deck._hasUsedCorruptJudgeThisRound)
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
        
        if (context.deck._hasUsedCorruptJudgeThisRound)
        {
            return "Corrupt Judge ability already used this round";
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
        context.deck._hasUsedCorruptJudgeThisRound = true;
        
        // Store context for callback
        _storedContext = context;
        
        // Get next 3 cards from deck
        List<CardInfo> nextCards = new List<CardInfo>();
        int cardsToShow = Mathf.Min(CARDS_TO_PREVIEW, context.deck.values.Length - context.deck.CardIndex);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            int index = context.deck.CardIndex + i;
            if (index < context.deck.values.Length)
            {
                nextCards.Add(context.deck.GetCardInfo(index));
            }
        }
        
        // Show preview using CardPreviewManager with rearrange capability
        if (nextCards.Count > 0 && context.cardPreviewManager != null)
        {
            Log($"Showing preview for {nextCards.Count} cards with rearrangement option");
            context.cardPreviewManager.ShowCorruptJudgePreview(
                nextCards,
                OnConfirm,
                null // Cancel callback
            );
        }
        else
        {
            LogWarning("No cards to show or CardPreviewManager not found");
            return false;
        }
        
        Log("Effect executed - showing card rearrangement preview");
        return true;
    }
    
    /// <summary>
    /// Called when player confirms the rearrangement
    /// </summary>
    private void OnConfirm(List<CardInfo> rearrangedCards)
    {
        if (_storedContext == null || _storedContext.deck == null)
        {
            LogError("Context lost during confirmation");
            return;
        }
        
        // Apply the rearrangement to the actual deck
        // Only the first two cards can be rearranged
        if (rearrangedCards.Count >= CARDS_TO_REARRANGE)
        {
            Deck deck = _storedContext.deck;
            
            // Update the deck arrays with the new order
            for (int i = 0; i < Mathf.Min(CARDS_TO_REARRANGE, rearrangedCards.Count); i++)
            {
                int deckPosition = deck.CardIndex + i;
                if (deckPosition < deck.values.Length)
                {
                    // Find and update the deck position with the rearranged card
                    for (int j = 0; j < deck.faces.Length; j++)
                    {
                        if (deck.faces[j] == rearrangedCards[i].cardSprite)
                        {
                            deck.faces[deckPosition] = rearrangedCards[i].cardSprite;
                            deck.values[deckPosition] = rearrangedCards[i].value;
                            deck.originalIndices[deckPosition] = rearrangedCards[i].index;
                            break;
                        }
                    }
                }
            }
            Log("Rearranged the first two upcoming cards");
        }
        
        _storedContext = null;
    }
}

