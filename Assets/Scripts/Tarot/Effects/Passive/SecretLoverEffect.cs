using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The Secret Lover - Adds +50 bonus per Heart card in winning hand.
/// Passive effect that must be activated once per round.
/// </summary>
public class SecretLoverEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.SecretLover;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already activated this round
        if (context.deck._hasActivatedSecretLoverThisRound)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck._hasActivatedSecretLoverThisRound)
        {
            return "The Secret Lover already activated this round";
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
        context.deck._hasActivatedSecretLoverThisRound = true;
        
        Log("Activated! Will provide +50 bonus per Heart in winning hands for this round");
        
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
        if (!context.deck.PlayerActuallyHasCard(TarotCardType.SecretLover) || 
            !context.deck.PlayerHasActivatedCard(TarotCardType.SecretLover))
        {
            return 0;
        }
        
        // Count hearts in player's hand
        List<CardInfo> handCards = context.deck.GetHandCardInfo(context.player);
        int heartCount = handCards.Count(card => card.suit == CardSuit.Hearts);
        
        uint bonus = (uint)(heartCount * Constants.SuitBonusAmount);
        
        if (bonus > 0)
        {
            Log($"Bonus calculated: {heartCount} Hearts = +{bonus}");
        }
        
        return bonus;
    }
}

