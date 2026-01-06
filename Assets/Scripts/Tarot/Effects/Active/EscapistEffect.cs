using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// The Escapist - Prevents losing by destroying the last card hit by player, then destroys itself.
/// One-time use effect with animation.
/// </summary>
public class EscapistEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.TheEscapist;
    
    public override bool RequiresCoroutine => true;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if there's a last hit card to remove
        if (context.deck._lastHitCard == null)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck._lastHitCard == null)
        {
            return "No last hit card to remove - you need to hit a card first!";
        }
        
        return string.Empty;
    }
    
    public override bool Execute(TarotEffectContext context)
    {
        // This effect requires coroutine, so just start it
        if (context.deck != null)
        {
            context.deck.StartCoroutine(ExecuteCoroutine(context));
            return true;
        }
        return false;
    }
    
    public override IEnumerator ExecuteCoroutine(TarotEffectContext context)
    {
        if (!CanExecute(context))
        {
            LogWarning(GetCannotExecuteReason(context));
            yield break;
        }
        
        Log("Activated! Removing last hit card...");
        
        GameObject cardToRemove = context.deck._lastHitCard;
        CardHand playerHand = context.playerHand;
        
        // Remove the card from hand
        if (playerHand != null && playerHand.cards.Contains(cardToRemove))
        {
            playerHand.cards.Remove(cardToRemove);
            
            // Animate the card removal
            yield return AnimateCardRemoval(cardToRemove, playerHand);
            
            // Clear the last hit card reference
            context.deck._lastHitCard = null;
            
            // Re-enable player controls
            context.deck.hitButton.interactable = true;
            context.deck.stickButton.interactable = true;
            
            // Show message
            if (context.finalMessage != null)
            {
                context.finalMessage.text = "The Escapist saved you! Continue playing...";
            }
            
            Log("Card removal completed - game continues");
        }
        else
        {
            LogWarning("Last hit card not found in player's hand");
        }
    }
    
    /// <summary>
    /// Animate the removal of a card by The Escapist
    /// </summary>
    private IEnumerator AnimateCardRemoval(GameObject cardToRemove, CardHand playerHand)
    {
        if (cardToRemove == null) yield break;
        
        // Create a dramatic escape animation
        Sequence escapeSequence = DOTween.Sequence();
        
        // Flash the card white to indicate The Escapist's intervention
        Image spriteRenderer = cardToRemove.GetComponent<Image>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            escapeSequence.Append(spriteRenderer.DOColor(Color.white, 0.1f));
            escapeSequence.Append(spriteRenderer.DOColor(originalColor, 0.1f));
            escapeSequence.Append(spriteRenderer.DOColor(Color.white, 0.1f));
            escapeSequence.Append(spriteRenderer.DOColor(originalColor, 0.1f));
        }
        
        // Scale up and rotate for dramatic effect
        escapeSequence.Append(cardToRemove.transform.DOScale(cardToRemove.transform.localScale * 1.3f, 0.2f)
            .SetEase(Ease.OutQuad));
        escapeSequence.Join(cardToRemove.transform.DORotate(new Vector3(0, 0, 360f), 0.3f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad));
        
        // Fade out and shrink
        if (spriteRenderer != null)
        {
            escapeSequence.Join(spriteRenderer.DOFade(0f, 0.3f));
        }
        escapeSequence.Join(cardToRemove.transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InQuart));
        
        yield return escapeSequence.WaitForCompletion();
        
        // Destroy the card
        if (cardToRemove != null)
        {
            Object.Destroy(cardToRemove);
        }
        
        // Rearrange remaining cards
        if (playerHand != null)
        {
            playerHand.ArrangeCardsInWindow();
            playerHand.UpdatePoints();
        }
        
        Log("Card removal animation completed");
    }
}

