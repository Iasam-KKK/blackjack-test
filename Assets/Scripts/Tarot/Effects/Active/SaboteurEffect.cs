using UnityEngine;
using System.Collections;

/// <summary>
/// Saboteur - Lose 1/4 of your bet, temporarily discard all cards from the enemy's played side.
/// Active effect that clears dealer's hand and re-deals.
/// </summary>
public class SaboteurEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.Saboteur;
    
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
        Log("Activated: Removing dealer cards...");
        
        Deck deck = context.deck;
        
        // Clear dealer hand using the fixed ClearHand method (destroys only cards, not SlotsContainer)
        if (context.dealerHand != null)
        {
            context.dealerHand.ClearHand();
        }
        
        // Wait for destroy to process
        yield return new WaitForSeconds(0.3f);
        
        // Re-deal 2 new cards to the dealer
        for (int i = 0; i < Constants.InitialCardsDealt; i++)
        {
            yield return InvokePushDealerAnimated(deck);
            yield return new WaitForSeconds(Constants.CardDealDelay);
        }
        
        // Update displays
        deck.UpdateScoreDisplays();
        deck.UpdateDiscardButtonState();
        deck.UpdatePeekButtonState();
        deck.UpdateTransformButtonState();
        
        Log("Effect completed - dealer hand re-dealt");
    }
    
    /// <summary>
    /// Invoke the private PushDealerAnimated method
    /// </summary>
    private IEnumerator InvokePushDealerAnimated(Deck deck)
    {
        System.Reflection.MethodInfo method = typeof(Deck).GetMethod(
            "PushDealerAnimated",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (method != null)
        {
            object result = method.Invoke(deck, null);
            if (result is IEnumerator enumerator)
            {
                yield return deck.StartCoroutine(enumerator);
            }
        }
        else
        {
            LogWarning("Could not find PushDealerAnimated method");
        }
    }
}

