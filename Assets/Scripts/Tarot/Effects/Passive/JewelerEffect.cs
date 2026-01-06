using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The Jeweler - Adds +50 bonus per Diamond card in winning hand.
/// Passive effect that must be activated once per round.
/// </summary>
public class JewelerEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.Jeweler;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already activated this round
        if (context.deck._hasActivatedJewelerThisRound)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck._hasActivatedJewelerThisRound)
        {
            return "The Jeweler already activated this round";
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
        
        // Activate the effect for this round
        context.deck._hasActivatedJewelerThisRound = true;
        
        Log("Activated! Will provide +50 bonus per Diamond in winning hands for this round");
        
        return true;
    }
    
    /// <summary>
    /// Calculate the bonus for the current hand.
    /// Called during EndHand to calculate suit bonuses.
    /// </summary>
    public uint CalculateBonus(TarotEffectContext context)
    {
        if (context.deck == null) return 0;
        
        // Must be activated and player must have the card
        if (!context.deck.PlayerActuallyHasCard(TarotCardType.Jeweler) || 
            !context.deck.PlayerHasActivatedCard(TarotCardType.Jeweler))
        {
            return 0;
        }
        
        // Count diamonds in player's hand
        List<CardInfo> handCards = context.deck.GetHandCardInfo(context.player);
        int diamondCount = handCards.Count(card => card.suit == CardSuit.Diamonds);
        
        uint bonus = (uint)(diamondCount * Constants.SuitBonusAmount);
        
        if (bonus > 0)
        {
            Log($"Bonus calculated: {diamondCount} Diamonds = +{bonus}");
        }
        
        return bonus;
    }
}

