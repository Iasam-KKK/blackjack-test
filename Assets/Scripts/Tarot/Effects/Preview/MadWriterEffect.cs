using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The Mad Writer - Take a look at the next card and shuffle the whole deck if desired.
/// Preview effect with optional deck shuffle.
/// </summary>
public class MadWriterEffect : TarotEffectBase
{
    public override TarotCardType EffectType => TarotCardType.MadWriter;
    
    // Stored reference to context for callback
    private TarotEffectContext _storedContext;
    
    public override bool CanExecute(TarotEffectContext context)
    {
        if (!base.CanExecute(context)) return false;
        
        // Check if already used this round
        if (context.deck._hasUsedMadWriterThisRound)
        {
            return false;
        }
        
        // Check if there are cards remaining in deck
        if (context.deck.CardIndex >= context.deck.values.Length)
        {
            return false;
        }
        
        return true;
    }
    
    public override string GetCannotExecuteReason(TarotEffectContext context)
    {
        string baseReason = base.GetCannotExecuteReason(context);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;
        
        if (context.deck._hasUsedMadWriterThisRound)
        {
            return "Mad Writer ability already used this round";
        }
        
        if (context.deck.CardIndex >= context.deck.values.Length)
        {
            return "No more cards in deck";
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
        
        // Mark as used
        context.deck._hasUsedMadWriterThisRound = true;
        
        // Store context for callback
        _storedContext = context;
        
        // Get next card from deck
        CardInfo nextCard = context.deck.GetCardInfo(context.deck.CardIndex);
        
        // Show preview using CardPreviewManager with shuffle option
        if (context.cardPreviewManager != null)
        {
            Log($"Showing preview for: {nextCard.cardName} with shuffle option");
            context.cardPreviewManager.ShowMadWriterPreview(
                nextCard,
                OnKeepOrder,   // Keep deck order callback
                OnShuffleDeck, // Shuffle deck callback
                null           // Cancel callback
            );
        }
        else
        {
            LogWarning("CardPreviewManager not found");
            return false;
        }
        
        Log("Effect executed - showing shuffle decision preview");
        return true;
    }
    
    /// <summary>
    /// Called when player chooses to keep the deck order
    /// </summary>
    private void OnKeepOrder()
    {
        Log("Player chose to keep deck order");
        _storedContext = null;
    }
    
    /// <summary>
    /// Called when player chooses to shuffle the deck
    /// </summary>
    private void OnShuffleDeck()
    {
        if (_storedContext == null || _storedContext.deck == null)
        {
            LogError("Context lost during shuffle decision");
            return;
        }
        
        // Shuffle the deck using reflection to access private method
        // This calls the private ShuffleCards method in Deck
        System.Reflection.MethodInfo shuffleMethod = typeof(Deck).GetMethod(
            "ShuffleCards", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (shuffleMethod != null)
        {
            shuffleMethod.Invoke(_storedContext.deck, null);
            Log("Shuffled the deck!");
        }
        else
        {
            LogWarning("Could not find ShuffleCards method");
        }
        
        _storedContext = null;
    }
}

