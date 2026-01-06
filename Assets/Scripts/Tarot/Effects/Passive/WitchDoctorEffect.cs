using UnityEngine;

/// <summary>
/// The Witch Doctor - Refunds 10% of bet if you lose.
/// Passive effect that must be activated once per round.
/// </summary>
public class WitchDoctorEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.WitchDoctor;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already activated this round
        if (context.deck._hasActivatedWitchDoctorThisRound)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck._hasActivatedWitchDoctorThisRound)
        {
            return "Witch Doctor already activated this round";
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
        context.deck._hasActivatedWitchDoctorThisRound = true;
        
        Log("Activated! Will provide 10% refund on losses for this round");
        
        return true;
    }
    
    /// <summary>
    /// Apply the refund effect when player loses.
    /// Called from EndHand when player loses.
    /// </summary>
    /// <param name="context">Effect context</param>
    /// <param name="betAmount">The bet amount that was lost</param>
    /// <returns>The refund amount</returns>
    public float ApplyRefund(TarotEffectContext context, float betAmount)
    {
        if (context.deck == null) return 0f;
        
        // Must be activated and player must have the card
        if (!context.deck.PlayerActuallyHasCard(TarotCardType.WitchDoctor) || 
            !context.deck.PlayerHasActivatedCard(TarotCardType.WitchDoctor))
        {
            return 0f;
        }
        
        // Calculate 10% refund
        float refund = betAmount * 0.1f;
        
        // Apply healing
        if (GameProgressionManager.Instance != null && refund > 0)
        {
            GameProgressionManager.Instance.HealPlayer(refund);
            Log($"Refunded 10% of bet: +{refund:F1}");
        }
        
        return refund;
    }
}

