using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The Fortune Teller - Take a peek into the next two cards on your deck.
/// Preview effect that shows upcoming player cards.
/// </summary>
public class FortuneTellerEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.FortuneTeller;
    
    private const int CARDS_TO_PREVIEW = 2;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already used this round
        if (context.deck._hasUsedFortuneTellerThisRound)
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
        
        if (context.deck._hasUsedFortuneTellerThisRound)
        {
            return "Fortune Teller ability already used this round";
        }
        
        if (context.deck.CardIndex >= context.deck.values.Length)
        {
            return "No more cards in deck to show";
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
        context.deck._hasUsedFortuneTellerThisRound = true;
        
        // Get next 2 cards that will be dealt from the deck
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
        
        // Show preview using CardPreviewManager
        if (nextCards.Count > 0 && context.cardPreviewManager != null)
        {
            Log($"Showing preview for {nextCards.Count} upcoming player cards");
            context.cardPreviewManager.ShowPreview(
                nextCards,
                "The Fortune Teller - Next Player Cards",
                false, // No rearranging
                false, // No removing
                0,
                null, // No confirm callback needed
                null  // No cancel callback needed
            );
        }
        else
        {
            LogWarning("No cards to show or CardPreviewManager not found");
            return false;
        }
        
        Log("Effect executed - revealed next 2 player cards");
        return true;
    }
}

