using UnityEngine;
using System.Collections;

/// <summary>
/// Scammer - Reverts the enemy's winning play discarding the played card at the cost of half the bet.
/// Active effect that removes dealer's last card when they're winning.
/// </summary>
public class ScammerEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.Scammer;
    
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
            float halfBet = context.deck.CurrentBetAmount * 0.5f;
            if (GameProgressionManager.Instance != null)
            {
                GameProgressionManager.Instance.DamagePlayer(halfBet);
            }
            Log($"Deducted half of the bet: -{halfBet:F0}");
            
            context.deck.StartCoroutine(ExecuteCoroutine(context));
            return true;
        }
        return false;
    }
    
    public override IEnumerator ExecuteCoroutine(TarotEffectContext context)
    {
        Log("Attempting to reverse dealer win...");
        
        Deck deck = context.deck;
        
        int playerScore = deck.GetPlayerPoints();
        int dealerScore = deck.GetDealerPoints();
        
        // Check if dealer is actually winning
        if (dealerScore > playerScore && dealerScore <= 21)
        {
            CardHand dealerHand = context.dealerHand;
            
            if (dealerHand != null && dealerHand.cards.Count > 0)
            {
                // Remove dealer's last card
                GameObject lastCard = dealerHand.cards[dealerHand.cards.Count - 1];
                dealerHand.cards.RemoveAt(dealerHand.cards.Count - 1);
                Object.Destroy(lastCard);
                
                Log("Removed dealer's last card to weaken their score");
                
                yield return new WaitForSeconds(0.2f);
                
                // Update score displays
                deck.UpdateScoreDisplays();
                
                // Show continue message
                if (context.finalMessage != null)
                {
                    context.finalMessage.text = "Continue";
                    context.finalMessage.gameObject.SetActive(true);
                    deck.hitButton.interactable = true;
                    deck.stickButton.interactable = true;
                }
                
                // Wait then hide message
                yield return new WaitForSeconds(1f);
                
                if (context.finalMessage != null)
                {
                    context.finalMessage.gameObject.SetActive(false);
                }
                
                // Check if dealer needs to continue
                int newDealerScore = deck.GetDealerPoints();
                
                if (newDealerScore < 17)
                {
                    // Dealer needs to hit again - invoke dealer turn
                    yield return InvokePushDealerAnimated(deck);
                }
                else
                {
                    // Re-evaluate game state
                    ResolveEndOfRound(deck);
                }
            }
        }
        else
        {
            Log("Dealer wasn't winning, no need to reverse");
        }
    }
    
    /// <summary>
    /// Resolve the end of round after scammer effect
    /// </summary>
    private void ResolveEndOfRound(Deck deck)
    {
        int playerScore = deck.GetPlayerPoints();
        int dealerScore = deck.GetDealerPoints();
        
        // Use reflection to call EndHand since it's private
        System.Reflection.MethodInfo endHandMethod = typeof(Deck).GetMethod(
            "EndHand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (endHandMethod != null)
        {
            WinCode result;
            if (dealerScore > 21 || playerScore > dealerScore)
            {
                result = WinCode.PlayerWins;
            }
            else if (dealerScore == playerScore)
            {
                result = WinCode.Draw;
            }
            else
            {
                result = WinCode.DealerWins;
            }
            
            endHandMethod.Invoke(deck, new object[] { result });
        }
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
    }
}

