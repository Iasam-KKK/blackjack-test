using UnityEngine;

/// <summary>
/// The Artificer - Boosts win multiplier by 10% if streak is active.
/// Passive effect that must be activated once per round.
/// </summary>
public class ArtificerEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.Artificer;
    
    /// <summary>
    /// The multiplier bonus when Artificer is active (10% = 1.1x)
    /// </summary>
    public const float MULTIPLIER_BONUS = 1.1f;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already activated this round
        if (context.deck._hasActivatedArtificerThisRound)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck._hasActivatedArtificerThisRound)
        {
            return "Artificer already activated this round";
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
        context.deck._hasActivatedArtificerThisRound = true;
        
        Log("Activated! Will boost win multiplier by 10% when you have a streak for this round");
        
        return true;
    }
    
    /// <summary>
    /// Check if the Artificer bonus should be applied to the win multiplier.
    /// Called from CalculateWinMultiplier in Deck.
    /// </summary>
    /// <param name="context">Effect context</param>
    /// <param name="currentStreak">The current streak value</param>
    /// <returns>The multiplier bonus (1.1 if active, 1.0 otherwise)</returns>
    public float GetMultiplierBonus(TarotEffectContext context, int currentStreak)
    {
        if (context.deck == null) return 1.0f;
        
        // Must be activated, have the card, and have an active streak
        if (!context.deck.PlayerActuallyHasCard(TarotCardType.Artificer) || 
            !context.deck.PlayerHasActivatedCard(TarotCardType.Artificer))
        {
            return 1.0f;
        }
        
        // Only apply bonus if there's an active streak
        if (currentStreak <= 0)
        {
            return 1.0f;
        }
        
        Log($"Bonus applied: {MULTIPLIER_BONUS:F2}x multiplier boost");
        return MULTIPLIER_BONUS;
    }
    
    /// <summary>
    /// Static helper to check if Artificer bonus applies without context
    /// </summary>
    public static bool ShouldApplyBonus(Deck deck)
    {
        if (deck == null) return false;
        
        return deck.PlayerActuallyHasCard(TarotCardType.Artificer) && 
               deck.PlayerHasActivatedCard(TarotCardType.Artificer);
    }
}

