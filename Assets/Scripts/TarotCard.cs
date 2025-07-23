using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class TarotCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Card Data")]
    public TarotCardData cardData;
    
    [Header("UI Components")]
    public Image cardImage;
    public Text cardNameText;
    public Text priceText;
    
    [Header("State")]
    public bool isInShop = true;        // Whether card is in shop or player's inventory
    public bool isEquipped = false;     // Whether player has equipped the card
    public bool hasBeenUsedThisRound = false; // Track if card has been used this round
    
    [HideInInspector]
    public Deck deck; // Reference to the game deck
    private ShopManager shopManager; // Reference to the shop manager
    private RectTransform rectTransform;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Find components if not assigned
        if (cardImage == null) cardImage = GetComponent<Image>();
        if (cardNameText == null) cardNameText = GetComponentInChildren<Text>();
        
        // Find references in scene
        if (deck == null) deck = FindObjectOfType<Deck>();
        shopManager = FindObjectOfType<ShopManager>();
    }
    
    private void Start()
    {
        UpdateCardDisplay();
    }
    
    // Update card visuals based on data
    private void UpdateCardDisplay()
    {
        if (cardData != null)
        {
            // Update image and texts
            if (cardImage != null && cardData.cardImage != null)
            {
                cardImage.sprite = cardData.cardImage;
            }
            
            if (cardNameText != null)
            {
                cardNameText.text = cardData.cardName;
            }
            
            if (priceText != null)
            {
                if (isInShop)
                {
                    priceText.text = cardData.price + "$";
                    priceText.gameObject.SetActive(true);
                }
                else
                {
                    priceText.gameObject.SetActive(false);
                }
            }
            
            // Visual state for used cards
            if (hasBeenUsedThisRound)
            {
                cardImage.color = new Color(0.5f, 0.5f, 0.5f);
            }
            else
            {
                cardImage.color = Color.white;
            }
        }
    }
    
    // Called when the mouse enters the card area
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show tooltip with card description - ONLY for tarot cards (not shop items)
        if (TooltipManager.Instance != null && cardData != null && !isInShop)
        {
            // Get default descriptions based on card type if description is empty
            string description = cardData.description;
            if (string.IsNullOrEmpty(description))
            {
                switch (cardData.cardType)
                {
                    case TarotCardType.Peek:
                        description = "Eye of Providence: Peek at the dealer's hidden card for 2 seconds. Can only be used once per round.";
                        break;
                    case TarotCardType.Discard:
                        description = "Discard: Remove the selected card from your hand. Can be used once per round.";
                        break;
                    case TarotCardType.Transform:
                        description = "Transformation: Replace the first selected card with a duplicate of the second selected card. Can only be used once per round.";
                        break;
                    case TarotCardType.WitchDoctor:
                        description = "Witch Doctor: Automatically refunds 10% of your bet when you lose a hand. Passive ability.";
                        break;
                    case TarotCardType.Artificer:
                        description = "Artificer: Boosts win multiplier by 10% when you have an active streak. Passive ability.";
                        break;
                    case TarotCardType.Botanist:
                        description = "The Botanist: Adds a +50 bonus for each club (clover) card in your winning hand. Passive ability.";
                        break;
                    case TarotCardType.Assassin:
                        description = "The Assassin: Adds a +50 bonus for each spade card in your winning hand. Passive ability.";
                        break;
                    case TarotCardType.SecretLover:
                        description = "The Secret Lover: Adds a +50 bonus for each heart card in your winning hand. Passive ability.";
                        break;
                    case TarotCardType.Jeweler:
                        description = "The Jeweler: Adds a +50 bonus for each diamond card in your winning hand. Passive ability.";
                        break;
                    default:
                        description = "A mystical tarot card with special powers.";
                        break;
                }
            }
            
            TooltipManager.Instance.ShowTooltip(cardData.cardName, description, transform.position, true);
        }
        
        // Visual feedback
        transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad);
    }
    
    // Called when the mouse exits the card area
    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide tooltip - only if it was shown (not for shop items)
        if (TooltipManager.Instance != null && !isInShop)
        {
            TooltipManager.Instance.HideTooltip();
        }
        
        // Reset visual feedback
        transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
    }
    
    // Called when card is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isInShop)
        {
            TryPurchaseCard();
        }
        else if (!hasBeenUsedThisRound || cardData.isReusable)
        {
            TryUseCard();
        }
    }
    
    // Try to purchase the card from the shop
    public void TryPurchaseCard()
    {
        if (deck != null && deck.Balance >= (uint)cardData.price)
        {
            uint cost = (uint)cardData.price;
            
            // Deduct balance
            deck.Balance -= cost;
            
            // Add card to PlayerStats
            if (PlayerStats.instance != null && cardData != null)
            {
                PlayerStats.instance.ownedCards.Add(cardData);
                Debug.Log("Added " + cardData.cardName + " to player's owned cards");
            }
            
            // Notify the deck about the purchase
            deck.OnCardPurchased(cost);
            
            // Notify shop manager (optional)
            if (shopManager != null)
            {
                shopManager.OnCardPurchased(this);
            }
            
            // Move card to player's tarot panel
            MoveToTarotPanel();
        }
        else
        {
            // Not enough balance - show feedback
            transform.DOShakePosition(0.5f, 10, 10, 90, false, true);
            
            if (deck != null)
            {
                Debug.Log("Not enough balance to purchase card. Balance: " + deck.Balance + ", Price: " + cardData.price);
            }
        }
    }
    
    // Move card from shop to tarot panel
    private void MoveToTarotPanel()
    {
        isInShop = false;
        
        // Get reference to the shop manager if not already assigned
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
        }
        
        if (shopManager != null)
        {
            // Store original position and scale for animation
            Vector3 originalPos = transform.position;
            Vector3 originalScale = transform.localScale;
            
            // Find a target slot through ShopManager
            Transform targetSlot = shopManager.GetEmptyTarotSlot();
            
            if (targetSlot == null)
            {
                Debug.LogWarning("No empty slot in tarot panel! Card purchase canceled.");
                return;
            }
            
            // Calculate animation path
            Vector3[] path = new Vector3[2];
            path[0] = originalPos + Vector3.up * 100; // Arc peak
            path[1] = targetSlot.position; // Destination
            
            // Sequence of animations
            Sequence moveSequence = DOTween.Sequence();
            
            // Zoom up
            moveSequence.Append(transform.DOScale(originalScale * 1.5f, 0.3f).SetEase(Ease.OutQuad));
            
            // Move along path
            moveSequence.Append(transform.DOPath(path, cardData.animationDuration, PathType.CatmullRom).SetEase(Ease.OutQuad));
            
            // Final adjustment
            moveSequence.AppendCallback(() => {
                // Use ShopManager helper to properly place in slot
                shopManager.AddCardToTarotPanel(this);
                
                // Update display (hide price)
                UpdateCardDisplay();
            });
        }
        else
        {
            Debug.LogError("ShopManager not found!");
            
            // Fallback to old implementation
            Transform tarotPanel = GameObject.FindGameObjectWithTag("TarotPanel")?.transform;
            if (tarotPanel != null)
            {
                Transform targetSlot = FindEmptyTarotSlot(tarotPanel);
                if (targetSlot != null)
                {
                    // Just place the card without animation
                    transform.SetParent(targetSlot, false);
                    transform.localPosition = Vector3.zero;
                    transform.localScale = Vector3.one * 0.8f;
                    
                    // Update display (hide price)
                    UpdateCardDisplay();
                }
            }
        }
    }
    
    // Add this helper method to find an empty slot
    private Transform FindEmptyTarotSlot(Transform tarotPanel)
    {
        // Look for slots in the tarot panel
        foreach (Transform slot in tarotPanel)
        {
            // If the slot name contains "Slot" and has no TarotCard children, it's empty
            if (slot.name.Contains("Slot") && slot.childCount == 0)
            {
                return slot;
            }
        }
        return null;
    }
    
    // Use the card's ability
    private void TryUseCard()
    {
        if (hasBeenUsedThisRound && !cardData.isReusable)
        {
            Debug.Log("Card has already been used this round");
            return;
        }
        
        // Trigger the appropriate effect based on card type
        if (deck != null)
        {
            bool effectApplied = false;
            
            switch (cardData.cardType)
            {
                case TarotCardType.Peek:
                    if (!deck._hasUsedPeekThisRound)
                    {
                        deck.PeekAtDealerCard();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Peek ability already used this round");
                    }
                    break;
                    
                case TarotCardType.Discard:
                    if (deck.player.GetComponent<CardHand>().HasSelectedCard())
                    {
                        deck.DiscardSelectedCard();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Select a card to discard first");
                    }
                    break;
                    
                case TarotCardType.Transform:
                    if (!deck._hasUsedTransformThisRound && 
                        deck.player.GetComponent<CardHand>().GetSelectedCardCount() == Constants.MaxSelectedCards)
                    {
                        deck.TransformSelectedCards();
                        effectApplied = true;
                    }
                    else if (deck._hasUsedTransformThisRound)
                    {
                        Debug.Log("Transform ability already used this round");
                    }
                    else
                    {
                        Debug.Log("Select exactly " + Constants.MaxSelectedCards + " cards to transform");
                    }
                    break;
                    
                case TarotCardType.WitchDoctor:
                    Debug.Log("Witch Doctor card is active and will provide 10% refund on losses");
                    // Don't mark as used - it's a passive effect
                    break;

                case TarotCardType.Artificer:
                    Debug.Log("Artificer card is active and will boost win multiplier by 10% when you have a streak");
                    // Don't mark as used - it's a passive effect
                    break;

                case TarotCardType.Botanist:
                    Debug.Log("The Botanist card is active and will provide +50 bonus per club in winning hands");
                    // Don't mark as used - it's a passive effect
                    break;

                case TarotCardType.Assassin:
                    Debug.Log("The Assassin card is active and will provide +50 bonus per spade in winning hands");
                    // Don't mark as used - it's a passive effect
                    break;

                case TarotCardType.SecretLover:
                    Debug.Log("The Secret Lover card is active and will provide +50 bonus per heart in winning hands");
                    // Don't mark as used - it's a passive effect
                    break;

                case TarotCardType.Jeweler:
                    Debug.Log("The Jeweler card is active and will provide +50 bonus per diamond in winning hands");
                    // Don't mark as used - it's a passive effect
                    break;
                    
                default:
                    Debug.LogWarning("Unknown card type: " + cardData.cardType);
                    return;
            }
            
            // Mark as used for this round (if effect was successfully applied and card is not reusable)
            if (effectApplied && !cardData.isReusable)
            {
                hasBeenUsedThisRound = true;
                // Visual indication it's been used
                cardImage.color = new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }
    
    // Reset the card for a new round
    public void ResetForNewRound()
    {
        hasBeenUsedThisRound = false;
        cardImage.color = Color.white;
    }
} 