using UnityEngine;
using System.Collections;

/// <summary>
/// Whisper of the Past - Lose 1/4 of your bet, temporarily discard all cards from your played side.
/// Active effect that clears player's hand.
/// </summary>
public class WhisperOfThePastEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.WhisperOfThePast;
    
    public override bool RequiresCoroutine => true;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if bet has been placed
        if (context.deck.CurrentBetAmount <= 0)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck.CurrentBetAmount <= 0)
        {
            return "No bet placed to sacrifice";
        }
        
        return string.Empty;
    }
    
    public override bool Execute(TarotEffectContext context)
    {
        // This effect requires coroutine, so just start it
        if (context.deck != null)
        {
            // Deduct 1/4 of the bet first
            float quarterBet = context.deck.CurrentBetAmount * 0.25f;
            if (GameProgressionManager.Instance != null)
            {
                GameProgressionManager.Instance.DamagePlayer(quarterBet);
            }
            Log($"Deducted 1/4 of the bet: -{quarterBet:F0}");
            
            context.deck.StartCoroutine(ExecuteCoroutine(context));
            return true;
        }
        return false;
    }
    
    public override IEnumerator ExecuteCoroutine(TarotEffectContext context)
    {
        Log("Activated: Removing player cards...");
        
        Deck deck = context.deck;
        
        // Clear player hand using the fixed ClearHand method (destroys only cards, not SlotsContainer)
        if (context.playerHand != null)
        {
            context.playerHand.ClearHand();
        }
        
        // Reset hit counter since player's hand is completely cleared
        deck.ResetHitsThisHand();
        
        // Wait for destroy to process
        yield return new WaitForSeconds(0.4f);
        
        // Update displays - player will need to hit to get new cards
        deck.UpdateScoreDisplays();
        deck.UpdateDiscardButtonState();
        deck.UpdatePeekButtonState();
        deck.UpdateTransformButtonState();
        
        // Re-enable hit/stick buttons
        deck.hitButton.interactable = true;
        deck.stickButton.interactable = true;
        
        Log("Effect completed - player hand cleared, player can hit for new cards");
    }
}

