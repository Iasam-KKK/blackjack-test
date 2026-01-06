using UnityEngine;
using System.Collections;

/// <summary>
/// Makeup Artist - Allows to select a card and pass it for another (replace with new card from deck).
/// Active effect that replaces a selected card.
/// </summary>
public class MakeupArtistEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.MakeupArtist;
    
    public override bool RequiresCoroutine => true;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if a card is selected
        if (context.deck.selectedCardForMakeupArtist == null)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck.selectedCardForMakeupArtist == null)
        {
            return "Select a card in your hand first";
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
        CardModel cardToReplace = context.deck.selectedCardForMakeupArtist;
        
        if (cardToReplace == null)
        {
            LogWarning("No card selected for replacement");
            yield break;
        }
        
        Log("Replacing selected card...");
        
        Deck deck = context.deck;
        CardHand hand = context.playerHand;
        
        if (hand == null)
        {
            LogError("Player hand not found");
            yield break;
        }
        
        // Remove the selected card from hand logic
        hand.cards.Remove(cardToReplace.gameObject);
        Object.Destroy(cardToReplace.gameObject);
        
        // Clear the selection
        deck.selectedCardForMakeupArtist = null;
        
        yield return new WaitForSeconds(0.2f);
        
        // Deal one new card to player
        yield return InvokePushPlayerAnimated(deck);
        
        // Update displays
        deck.UpdateScoreDisplays();
        
        Log("Effect completed - card replaced");
    }
    
    /// <summary>
    /// Invoke the private PushPlayerAnimated method
    /// </summary>
    private IEnumerator InvokePushPlayerAnimated(Deck deck)
    {
        System.Reflection.MethodInfo method = typeof(Deck).GetMethod(
            "PushPlayerAnimated",
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
            LogWarning("Could not find PushPlayerAnimated method");
        }
    }
}

