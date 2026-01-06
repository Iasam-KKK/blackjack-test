using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The House Keeper - Adds +10 bonus per Jack/Queen/King card in winning hand.
/// Passive effect that must be activated once per round.
/// </summary>
public class HouseKeeperEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.HouseKeeper;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already activated this round
        if (context.deck._hasActivatedHouseKeeperThisRound)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck._hasActivatedHouseKeeperThisRound)
        {
            return "The House Keeper already activated this round";
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
        context.deck._hasActivatedHouseKeeperThisRound = true;
        
        Log("Activated! Will provide +10 bonus per Jack/Queen/King in winning hands for this round");
        
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
        if (!context.deck.PlayerActuallyHasCard(TarotCardType.HouseKeeper) || 
            !context.deck.PlayerHasActivatedCard(TarotCardType.HouseKeeper))
        {
            return 0;
        }
        
        // Count face cards (Jack=10, Queen=11, King=12 in suitIndex) in player's hand
        List<CardInfo> handCards = context.deck.GetHandCardInfo(context.player);
        int faceCardCount = handCards.Count(card => 
            card.suitIndex == 10 || // Jack
            card.suitIndex == 11 || // Queen
            card.suitIndex == 12    // King
        );
        
        uint bonus = (uint)(faceCardCount * Constants.HouseKeeperBonusAmount);
        
        if (bonus > 0)
        {
            Log($"Bonus calculated: {faceCardCount} Face Cards = +{bonus}");
        }
        
        return bonus;
    }
}

