using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Settings")]
    public float timeToWait = 0.5f; // Delay before showing tooltip
    
    [Header("Card Data")]
    public TarotCardData cardData; // The card data to show tooltip for
    
    private Coroutine showTooltipCoroutine;
    
    private void Start()
    {
        // If no cardData is assigned, try to get it from TarotCard component
        if (cardData == null)
        {
            TarotCard tarotCard = GetComponent<TarotCard>();
            if (tarotCard != null)
            {
                cardData = tarotCard.cardData;
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Stop any existing coroutine
        if (showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }
        
        // Start timer to show tooltip
        showTooltipCoroutine = StartCoroutine(StartTimer());
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Stop any running coroutine
        if (showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
            showTooltipCoroutine = null;
        }
        
        // Hide tooltip
        TooltipHoverManager.OnMouseLoseFocus?.Invoke();
    }
    
    private IEnumerator StartTimer()
    {
        // Wait for the specified time
        yield return new WaitForSeconds(timeToWait);
        
        // Show tooltip with dynamic content
        ShowTooltip();
    }
    
    private void ShowTooltip()
    {
        if (cardData == null) return;
        
        // Get the dynamic tooltip content
        string tooltipContent = GetTooltipContent();
        
        // Show tooltip at mouse position
        TooltipHoverManager.OnMouseHover?.Invoke(tooltipContent, Input.mousePosition);
    }
    
    private string GetTooltipContent()
    {
        if (cardData == null) return "No card data available";
        
        // Start with card name
        string content = $"<b>{cardData.cardName}</b>\n\n";
        
        // Add description
        string description = cardData.description;
        if (string.IsNullOrEmpty(description))
        {
            // Get default description based on card type
            description = GetDefaultDescription(cardData.cardType);
        }
        content += description;
        
        // Add material information
        if (cardData.assignedMaterial != null)
        {
            content += $"\n\n<color=#FFD700>Material: {cardData.GetMaterialDisplayName()}</color>";
            
            int remainingUses = cardData.GetRemainingUses();
            if (remainingUses == -1)
            {
                content += "\n<color=#00FFFF>Durability: Unlimited</color>";
            }
            else
            {
                content += $"\n<color=#FFFF00>Durability: {remainingUses}/{cardData.maxUses}</color>";
            }
        }
        
        // Add price information for shop cards
        TarotCard tarotCard = GetComponent<TarotCard>();
        if (tarotCard != null && tarotCard.isInShop)
        {
            uint finalPrice = tarotCard.GetFinalPrice();
            content += $"\n\n<color=#FFD700>Price: {finalPrice} coins</color>";
        }
        
        return content;
    }
    
    private string GetDefaultDescription(TarotCardType cardType)
    {
        switch (cardType)
        {
            case TarotCardType.Peek:
                return "Eye of Providence: Peek at the dealer's hidden card for 2 seconds. Can only be used once per round.";
            case TarotCardType.Discard:
                return "Discard: Remove the selected card from your hand. Can be used once per round.";
            case TarotCardType.Transform:
                return "Transformation: Replace the first selected card with a duplicate of the second selected card. Can only be used once per round.";
            case TarotCardType.WitchDoctor:
                return "Witch Doctor: Click to activate! Refunds 10% of your bet when you lose a hand for this round. Must be activated each round.";
            case TarotCardType.Artificer:
                return "Artificer: Click to activate! Boosts win multiplier by 10% when you have an active streak for this round. Must be activated each round.";
            case TarotCardType.Botanist:
                return "The Botanist: Click to activate! Adds a +50 bonus for each club (clover) card in your winning hand for this round. Must be activated each round.";
            case TarotCardType.Assassin:
                return "The Assassin: Click to activate! Adds a +50 bonus for each spade card in your winning hand for this round. Must be activated each round.";
            case TarotCardType.SecretLover:
                return "The Secret Lover: Click to activate! Adds a +50 bonus for each heart card in your winning hand for this round. Must be activated each round.";
            case TarotCardType.Jeweler:
                return "The Jeweler: Click to activate! Adds a +50 bonus for each diamond card in your winning hand for this round. Must be activated each round.";
            case TarotCardType.Scavenger:
                return "The Scavenger: Removes all cards with value less than 7 from your hand. Can only be used once per round.";
            case TarotCardType.Gardener:
                return "The Gardener: Removes all club (clover) cards from both your hand and the dealer's hand. Can only be used once per round.";
            case TarotCardType.BetrayedCouple:
                return "The Betrayed Couple: Removes all heart cards from both your hand and the dealer's hand. Can only be used once per round.";
            case TarotCardType.Blacksmith:
                return "The Blacksmith: Removes all spade cards from both your hand and the dealer's hand. Can only be used once per round.";
            case TarotCardType.TaxCollector:
                return "The Tax Collector: Removes all diamond cards from both your hand and the dealer's hand. Can only be used once per round.";
            case TarotCardType.HouseKeeper:
                return "The House Keeper: Click to activate! Adds a +10 bonus for each Jack, Queen, or King card in your winning hand for this round. Must be activated each round.";
            case TarotCardType.Spy:
                return "The Spy: Allows you to peek at the next card that would be dealt to the dealer. Can only be used once per round.";
            case TarotCardType.BlindSeer:
                return "The Blind Seer: Reveals all cards currently in the dealer's hand, including hidden cards. Can only be used once per round.";
            case TarotCardType.CorruptJudge:
                return "The Corrupt Judge: Peek at the next three cards in the deck and rearrange the first two if desired. Can only be used once per round.";
            case TarotCardType.Hitman:
                return "The Hitman: Peek at the next three cards in the deck and remove one from play permanently. Can only be used once per round.";
            case TarotCardType.FortuneTeller:
                return "The Fortune Teller: Take a peek at the next two cards that will be dealt from the deck. Can only be used once per round.";
            case TarotCardType.MadWriter:
                return "The Mad Writer: Look at the next card in the deck and choose to shuffle the entire deck if desired. Can only be used once per round.";
            case TarotCardType.TheEscapist:
                return "The Escapist: Click to remove your last hit card and continue playing. Like an 'escape from jail' card - removes the problematic card but lets you keep playing. Destroys itself after use. Can only be used once per round.";
            default:
                return "A mystical tarot card with special powers.";
        }
    }
    
    // Method to update card data (useful if card data changes)
    public void SetCardData(TarotCardData newCardData)
    {
        cardData = newCardData;
    }
}
