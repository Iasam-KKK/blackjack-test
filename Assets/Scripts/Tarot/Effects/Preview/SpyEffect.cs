using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The Spy - Allows to peek at the next enemy card (dealer's next card).
/// Preview effect that shows upcoming cards.
/// </summary>
public class SpyEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.Spy;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already used this round
        if (context.deck._hasUsedSpyThisRound)
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
        
        if (context.deck._hasUsedSpyThisRound)
        {
            return "Spy ability already used this round";
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
        context.deck._hasUsedSpyThisRound = true;
        
        // Get the next card that would be dealt
        CardInfo nextCard = context.deck.GetCardInfo(context.deck.CardIndex);
        List<CardInfo> previewCards = new List<CardInfo> { nextCard };
        
        // Show preview using CardPreviewManager
        if (context.cardPreviewManager != null)
        {
            Log($"Showing preview for: {nextCard.cardName}");
            context.cardPreviewManager.ShowPreview(
                previewCards,
                "The Spy - Next Dealer Card",
                false, // No rearranging
                false, // No removing
                0,
                null, // No confirm callback needed
                null  // No cancel callback needed
            );
        }
        else
        {
            LogWarning("CardPreviewManager not found! Check if it's assigned in Deck component.");
            return false;
        }
        
        Log("Effect executed - revealed next dealer card");
        return true;
    }
}

