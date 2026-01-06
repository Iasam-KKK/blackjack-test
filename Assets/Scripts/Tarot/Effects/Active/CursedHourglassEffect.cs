using UnityEngine;
using System.Collections;

/// <summary>
/// Cursed Hourglass - Lose half of your bet, but temporarily discard all cards in play from both sides.
/// Active effect that clears all hands and re-deals.
/// </summary>
public class CursedHourglassEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.CursedHourglass;
    
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
            // Deduct half of the bet first
            float halfBet = context.deck.CurrentBetAmount / 2f;
            if (GameProgressionManager.Instance != null)
            {
                GameProgressionManager.Instance.DamagePlayer(halfBet);
            }
            
            // Update bet to remaining half
            context.deck.CurrentBetAmount /= 2;
            Log($"Deducted half of the bet: -{halfBet:F0}");
            
            context.deck.StartCoroutine(ExecuteCoroutine(context));
            return true;
        }
        return false;
    }
    
    public override IEnumerator ExecuteCoroutine(TarotEffectContext context)
    {
        Log("Activated: Removing all cards and re-dealing...");
        
        Deck deck = context.deck;
        
        // Clear hands using the fixed ClearHand method (destroys only cards, not SlotsContainer)
        if (context.playerHand != null)
        {
            context.playerHand.ClearHand();
        }
        if (context.dealerHand != null)
        {
            context.dealerHand.ClearHand();
        }
        
        // Reset hit counter since we're starting fresh
        deck.ResetHitsThisHand();
        
        // Wait for destroy to process
        yield return new WaitForSeconds(0.4f);
        
        // Re-deal 2 cards to each hand
        for (int i = 0; i < 2; i++)
        {
            // Deal to player using reflection to access private method
            yield return InvokePushAnimated(deck, true);
            yield return new WaitForSeconds(Constants.CardDealDelay);
            
            // Deal to dealer
            yield return InvokePushAnimated(deck, false);
            yield return new WaitForSeconds(Constants.CardDealDelay);
        }
        
        // Update displays
        deck.UpdateScoreDisplays();
        
        Log("Effect completed - hands re-dealt");
    }
    
    /// <summary>
    /// Invoke the private PushPlayerAnimated or PushDealerAnimated method
    /// </summary>
    private IEnumerator InvokePushAnimated(Deck deck, bool isPlayer)
    {
        string methodName = isPlayer ? "PushPlayerAnimated" : "PushDealerAnimated";
        
        System.Reflection.MethodInfo method = typeof(Deck).GetMethod(
            methodName,
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
            LogWarning($"Could not find {methodName} method");
        }
    }
}

