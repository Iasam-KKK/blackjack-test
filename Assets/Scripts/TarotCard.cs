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
    private bool hasActivatedCursedHourglass = false;

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
                    case TarotCardType.Scavenger:
                        description = "The Scavenger: Removes all cards with value less than 7 from your hand. Can only be used once per round.";
                        break;
                    case TarotCardType.Gardener:
                        description = "The Gardener: Removes all club (clover) cards from both your hand and the dealer's hand. Can only be used once per round.";
                        break;
                    case TarotCardType.BetrayedCouple:
                        description = "The Betrayed Couple: Removes all heart cards from both your hand and the dealer's hand. Can only be used once per round.";
                        break;
                    case TarotCardType.Blacksmith:
                        description = "The Blacksmith: Removes all spade cards from both your hand and the dealer's hand. Can only be used once per round.";
                        break;
                    case TarotCardType.TaxCollector:
                        description = "The Tax Collector: Removes all diamond cards from both your hand and the dealer's hand. Can only be used once per round.";
                        break;
                    case TarotCardType.HouseKeeper:
                        description = "The House Keeper: Adds a +10 bonus for each Jack, Queen, or King card in your winning hand. Passive ability.";
                        break;
                    case TarotCardType.Spy:
                        description = "The Spy: Allows you to peek at the next card that would be dealt to the dealer. Can only be used once per round.";
                        break;
                    case TarotCardType.BlindSeer:
                        description = "The Blind Seer: Reveals all cards currently in the dealer's hand, including hidden cards. Can only be used once per round.";
                        break;
                    case TarotCardType.CorruptJudge:
                        description = "The Corrupt Judge: Peek at the next three cards in the deck and rearrange the first two if desired. Can only be used once per round.";
                        break;
                    case TarotCardType.Hitman:
                        description = "The Hitman: Peek at the next three cards in the deck and remove one from play permanently. Can only be used once per round.";
                        break;
                    case TarotCardType.FortuneTeller:
                        description = "The Fortune Teller: Take a peek at the next two cards that will be dealt from the deck. Can only be used once per round.";
                        break;
                    case TarotCardType.MadWriter:
                        description = "The Mad Writer: Look at the next card in the deck and choose to shuffle the entire deck if desired. Can only be used once per round.";
                        break;
                                    case TarotCardType.TheEscapist:
                    description = "The Escapist: Click to remove your last hit card and continue playing. Like an 'escape from jail' card - removes the problematic card but lets you keep playing. Destroys itself after use. Can only be used once per round.";
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
                case TarotCardType.CursedHourglass:
                    if (!hasBeenUsedThisRound)
                    {
                        // Deduct half of the current bet from the player's balance
                        int halfBet = Mathf.FloorToInt(deck._bet / 2f);
                        deck.Balance = (uint)Mathf.Max(0, (int)deck.Balance - halfBet);
                        deck.bet.text = halfBet.ToString();
                        Debug.Log($"[CursedHourglass] Deducted half of the bet: -{halfBet}");

                        // Activate the card's effect
                        Debug.Log("Cursed Hourglass triggered!");
                        deck.StartCoroutine(deck.ActivateCursedHourglassEffect());

                        effectApplied = true;
                    }
                    break;
                case TarotCardType.MakeupArtist:
                    if (!hasBeenUsedThisRound)
                    {
                        if (deck.selectedCardForMakeupArtist != null)
                        {
                            deck.StartCoroutine(deck.ReplaceCardWithMakeupArtist(deck.selectedCardForMakeupArtist));
                            deck.selectedCardForMakeupArtist = null;
                            effectApplied = true;
                        }
                        else
                        {
                            Debug.Log("Select a card in your hand first.");
                        }
                    }
                    break;
                case TarotCardType.WhisperOfThePast:
                    if (!hasBeenUsedThisRound)
                    {
                        // Deduct ¼ of the current bet
                        int quarterBet = Mathf.FloorToInt(deck._bet * 0.75f);
                        deck.Balance = (uint)Mathf.Max(0, (int)deck.Balance - quarterBet);
                        deck.bet.text = quarterBet.ToString();  // Optional: You can keep showing the full bet here if needed
                        Debug.Log($"[WhisperOfThePast] Deducted ¼ of the bet: -{quarterBet}");

                        // Activate the card's effect
                        Debug.Log("[WhisperOfThePast] Removing player cards and re-dealing...");
                        deck.StartCoroutine(deck.ActivateWhisperOfThePastEffect());

                        effectApplied = true;
                    }
                    break;
                case TarotCardType.Saboteur:
                    if (!hasBeenUsedThisRound)
                    {
                        // Deduct ¼ of the current bet
                        int quarterBet = Mathf.FloorToInt(deck._bet * 0.75f);
                        deck.Balance = (uint)Mathf.Max(0, (int)deck.Balance - quarterBet);
                        deck.bet.text = quarterBet.ToString();  // Optional: you might want to show the full bet instead
                        Debug.Log($"[Saboteur] Deducted ¼ of the bet: -{quarterBet}");

                        // Activate the dealer-hand clearing effect
                        Debug.Log("[Saboteur] Removing dealer cards...");
                        deck.StartCoroutine(deck.ActivateSaboteurEffect());

                        effectApplied = true;
                    }
                    break;
                case TarotCardType.Scammer:
                    if (!hasBeenUsedThisRound)
                    {
                        Debug.Log("Scammer card triggered!");

                        int halfBet = Mathf.FloorToInt(deck._bet * 0.5f);
                        deck.Balance = (uint)Mathf.Max(0, (int)deck.Balance - halfBet);
                        deck.bet.text = (deck._bet - halfBet).ToString();

                        deck.StartCoroutine(deck.ActivateScammerEffect());

                        effectApplied = true;
                    }
                    break;

                case TarotCardType.Scavenger:
                    CardHand playerHand = deck.player.GetComponent<CardHand>();
                    if (playerHand != null && playerHand.cards.Count > 0)
                    {
                        // Find all cards with value < 7
                        var cardsToRemove = new System.Collections.Generic.List<GameObject>();
                        foreach (GameObject card in playerHand.cards)
                        {
                            CardModel cardModel = card.GetComponent<CardModel>();
                            if (cardModel != null && cardModel.value < 7)
                            {
                                cardsToRemove.Add(card);
                            }
                        }
                        
                        // Animate and remove the cards with staggered timing
                        if (cardsToRemove.Count > 0)
                        {
                            Debug.Log("The Scavenger is removing " + cardsToRemove.Count + " cards with value < 7");
                            
                            // Keep track of completed animations
                            int animationsCompleted = 0;
                            int totalAnimations = cardsToRemove.Count;
                            
                            for (int i = 0; i < cardsToRemove.Count; i++)
                            {
                                GameObject cardToRemove = cardsToRemove[i];
                                
                                // Deselect the card if it's selected
                                CardModel cardModel = cardToRemove.GetComponent<CardModel>();
                                if (cardModel != null && cardModel.isSelected)
                                {
                                    cardModel.DeselectCard();
                                }
                                
                                // Calculate staggered delay for dramatic effect
                                float delay = i * 0.15f; // 150ms between each card animation
                                
                                // Create dramatic whoosh animation sequence
                                Sequence whooshSequence = DOTween.Sequence();
                                
                                // Small delay for staggered effect
                                whooshSequence.AppendInterval(delay);
                                
                                // Scale up slightly and rotate for dramatic effect
                                whooshSequence.Append(cardToRemove.transform.DOScale(cardToRemove.transform.localScale * 1.2f, 0.2f)
                                    .SetEase(Ease.OutQuad));
                                whooshSequence.Join(cardToRemove.transform.DORotate(new Vector3(0, 0, Random.Range(-30f, 30f)), 0.2f)
                                    .SetEase(Ease.OutQuad));
                                
                                // Whoosh the card down off-screen
                                Vector3 whooshTarget = new Vector3(
                                    cardToRemove.transform.localPosition.x + Random.Range(-200f, 200f), // Random horizontal spread
                                    cardToRemove.transform.localPosition.y - 1000f, // Move far down off-screen
                                    cardToRemove.transform.localPosition.z
                                );
                                
                                whooshSequence.Append(cardToRemove.transform.DOLocalMove(whooshTarget, 0.8f)
                                    .SetEase(Ease.InQuart)); // Fast acceleration downward
                                
                                // Fade out during the whoosh
                                SpriteRenderer spriteRenderer = cardToRemove.GetComponent<SpriteRenderer>();
                                if (spriteRenderer != null)
                                {
                                    whooshSequence.Join(spriteRenderer.DOFade(0f, 0.6f).SetDelay(0.2f));
                                }
                                
                                // Complete the animation
                                whooshSequence.OnComplete(() => {
                                    // Remove from the cards list and destroy
                                    if (playerHand.cards.Contains(cardToRemove))
                                    {
                                        playerHand.cards.Remove(cardToRemove);
                                    }
                                    Destroy(cardToRemove);
                                    
                                    animationsCompleted++;
                                    
                                    // When all animations are complete, update the hand
                                    if (animationsCompleted >= totalAnimations)
                                    {
                                        // Rearrange remaining cards and update points
                                        playerHand.ArrangeCardsInWindow();
                                        playerHand.UpdatePoints();
                                        
                                        // Update displays after all cards are removed
                                        deck.UpdateScoreDisplays();
                                        deck.UpdateDiscardButtonState();
                                        deck.UpdateTransformButtonState();
                                        
                                        Debug.Log("The Scavenger finished removing all low-value cards");
                                    }
                                });
                            }
                            
                            effectApplied = true;
                        }
                        else
                        {
                            Debug.Log("No cards with value < 7 found in hand");
                            effectApplied = true; // Still counts as used even if no cards were removed
                        }
                    }
                    else
                    {
                        Debug.Log("No cards in hand to scavenge");
                        effectApplied = true; // Still counts as used
                    }
                    break;
                case TarotCardType.Gardener:
                    RemoveCardsBySuitFromBothHands(CardSuit.Clubs, "The Gardener", "club");
                    effectApplied = true;
                    break;
                case TarotCardType.BetrayedCouple:
                    RemoveCardsBySuitFromBothHands(CardSuit.Hearts, "The Betrayed Couple", "heart");
                    effectApplied = true;
                    break;
                case TarotCardType.Blacksmith:
                    RemoveCardsBySuitFromBothHands(CardSuit.Spades, "The Blacksmith", "spade");
                    effectApplied = true;
                    break;
                case TarotCardType.TaxCollector:
                    RemoveCardsBySuitFromBothHands(CardSuit.Diamonds, "The Tax Collector", "diamond");
                    effectApplied = true;
                    break;
                case TarotCardType.HouseKeeper:
                    Debug.Log("The House Keeper card is active and will provide +10 bonus per Jack/Queen/King in winning hands");
                    // Don't mark as used - it's a passive effect
                    break;
                    
                // NEW PREVIEW CARDS
                case TarotCardType.Spy:
                    if (!deck._hasUsedSpyThisRound)
                    {
                        deck.UseSpyCard();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Spy ability already used this round");
                    }
                    break;
                    
                case TarotCardType.BlindSeer:
                    if (!deck._hasUsedBlindSeerThisRound)
                    {
                        deck.UseBlindSeerCard();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Blind Seer ability already used this round");
                    }
                    break;
                    
                case TarotCardType.CorruptJudge:
                    if (!deck._hasUsedCorruptJudgeThisRound)
                    {
                        deck.UseCorruptJudgeCard();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Corrupt Judge ability already used this round");
                    }
                    break;
                    
                case TarotCardType.Hitman:
                    if (!deck._hasUsedHitmanThisRound)
                    {
                        deck.UseHitmanCard();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Hitman ability already used this round");
                    }
                    break;
                    
                case TarotCardType.FortuneTeller:
                    if (!deck._hasUsedFortuneTellerThisRound)
                    {
                        deck.UseFortuneTellerCard();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Fortune Teller ability already used this round");
                    }
                    break;
                    
                case TarotCardType.MadWriter:
                    if (!deck._hasUsedMadWriterThisRound)
                    {
                        deck.UseMadWriterCard();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Mad Writer ability already used this round");
                    }
                    break;
                    
                case TarotCardType.TheEscapist:
                    if (!hasBeenUsedThisRound && deck._lastHitCard != null)
                    {
                        Debug.Log("The Escapist activated by player click!");
                        deck.StartCoroutine(deck.UseEscapistCard());
                        hasBeenUsedThisRound = true;
                        
                        // Remove from PlayerStats if it exists there
                        if (PlayerStats.instance != null && PlayerStats.instance.ownedCards != null && cardData != null)
                        {
                            PlayerStats.instance.ownedCards.Remove(cardData);
                            Debug.Log("Removed " + cardData.cardName + " from player's owned cards after use");
                        }
                        
                        // The Escapist has its own destruction animation in Deck.cs, so just destroy this instance
                        StartCoroutine(AnimateCardDestruction());
                        effectApplied = true;
                        return; // Exit early since we're handling destruction manually
                    }
                    else if (hasBeenUsedThisRound)
                    {
                        Debug.Log("The Escapist has already been used this round");
                    }
                    else if (deck._lastHitCard == null)
                    {
                        Debug.Log("The Escapist: No last hit card to remove - you need to hit a card first!");
                    }
                    break;
                    
                default:
                    Debug.LogWarning("Unknown card type: " + cardData.cardType);
                    return;
            }
            
            // Destroy the card after use (if effect was successfully applied and card is not reusable)
            if (effectApplied && !cardData.isReusable)
            {
                hasBeenUsedThisRound = true;
                
                // Remove from PlayerStats if it exists there
                if (PlayerStats.instance != null && PlayerStats.instance.ownedCards != null && cardData != null)
                {
                    PlayerStats.instance.ownedCards.Remove(cardData);
                    Debug.Log("Removed " + cardData.cardName + " from player's owned cards after use");
                }
                
                // Animate the card destruction
                StartCoroutine(AnimateCardDestruction());
            }
        }
    }
    
    // Helper method to remove cards of a specific suit from both player and dealer hands
    private void RemoveCardsBySuitFromBothHands(CardSuit suit, string cardName, string suitName)
    {
        Debug.Log($"{cardName} is active and will remove all {suitName} cards from both hands.");
        
        // Get player's hand
        CardHand playerHand = deck.player.GetComponent<CardHand>();
        if (playerHand == null)
        {
            Debug.LogError("Player's CardHand not found!");
            return;
        }

        // Get dealer's hand
        CardHand dealerHand = deck.dealer.GetComponent<CardHand>();
        if (dealerHand == null)
        {
            Debug.LogError("Dealer's CardHand not found!");
            return;
        }

        // Find all cards of the specified suit in both hands using deck's utility methods
        var cardsToRemove = new System.Collections.Generic.List<GameObject>();
        
        // Check player's hand
        foreach (GameObject card in playerHand.cards)
        {
            CardModel cardModel = card.GetComponent<CardModel>();
            if (cardModel != null)
            {
                // Use deck's method to get card info including suit
                CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
                if (cardInfo.suit == suit)
                {
                    cardsToRemove.Add(card);
                }
            }
        }
        
        // Check dealer's hand
        foreach (GameObject card in dealerHand.cards)
        {
            CardModel cardModel = card.GetComponent<CardModel>();
            if (cardModel != null)
            {
                // Use deck's method to get card info including suit
                CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
                if (cardInfo.suit == suit)
                {
                    cardsToRemove.Add(card);
                }
            }
        }

        if (cardsToRemove.Count > 0)
        {
            Debug.Log($"Removing {cardsToRemove.Count} {suitName} cards from both hands.");
            
            // Keep track of completed animations
            int animationsCompleted = 0;
            int totalAnimations = cardsToRemove.Count;
            
            for (int i = 0; i < cardsToRemove.Count; i++)
            {
                GameObject cardToRemove = cardsToRemove[i];
                
                // Deselect the card if it's selected
                CardModel cardModel = cardToRemove.GetComponent<CardModel>();
                if (cardModel != null && cardModel.isSelected)
                {
                    cardModel.DeselectCard();
                }
                
                // Calculate staggered delay for dramatic effect
                float delay = i * 0.15f; // 150ms between each card animation
                
                // Create dramatic whoosh animation sequence
                Sequence whooshSequence = DOTween.Sequence();
                
                // Small delay for staggered effect
                whooshSequence.AppendInterval(delay);
                
                // Scale up slightly and rotate for dramatic effect
                whooshSequence.Append(cardToRemove.transform.DOScale(cardToRemove.transform.localScale * 1.2f, 0.2f)
                    .SetEase(Ease.OutQuad));
                whooshSequence.Join(cardToRemove.transform.DORotate(new Vector3(0, 0, Random.Range(-30f, 30f)), 0.2f)
                    .SetEase(Ease.OutQuad));
                
                // Whoosh the card down off-screen
                Vector3 whooshTarget = new Vector3(
                    cardToRemove.transform.localPosition.x + Random.Range(-200f, 200f), // Random horizontal spread
                    cardToRemove.transform.localPosition.y - 1000f, // Move far down off-screen
                    cardToRemove.transform.localPosition.z
                );
                
                whooshSequence.Append(cardToRemove.transform.DOLocalMove(whooshTarget, 0.8f)
                    .SetEase(Ease.InQuart)); // Fast acceleration downward
                
                // Fade out during the whoosh
                SpriteRenderer spriteRenderer = cardToRemove.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    whooshSequence.Join(spriteRenderer.DOFade(0f, 0.6f).SetDelay(0.2f));
                }
                
                // Complete the animation
                whooshSequence.OnComplete(() => {
                    // Remove from the cards list and destroy
                    if (playerHand.cards.Contains(cardToRemove))
                    {
                        playerHand.cards.Remove(cardToRemove);
                    }
                    if (dealerHand.cards.Contains(cardToRemove))
                    {
                        dealerHand.cards.Remove(cardToRemove);
                    }
                    Destroy(cardToRemove);
                    
                    animationsCompleted++;
                    
                    // When all animations are complete, update the hands
                    if (animationsCompleted >= totalAnimations)
                    {
                        playerHand.ArrangeCardsInWindow();
                        playerHand.UpdatePoints();
                        dealerHand.ArrangeCardsInWindow();
                        dealerHand.UpdatePoints();
                        
                        // Update displays after all cards are removed
                        deck.UpdateScoreDisplays();
                        deck.UpdateDiscardButtonState();
                        deck.UpdateTransformButtonState();
                        
                        Debug.Log($"{cardName} finished removing all {suitName} cards.");
                    }
                });
            }
        }
        else
        {
            Debug.Log($"No {suitName} cards found in either hand.");
        }
    }
    
    // Reset the card for a new round
    public void ResetForNewRound()
    {
        hasBeenUsedThisRound = false;
        cardImage.color = Color.white;
    }
    
    // Animate the destruction of the tarot card after use
    private System.Collections.IEnumerator AnimateCardDestruction()
    {
        // Create a destruction animation
        Sequence destructionSequence = DOTween.Sequence();
        
        // Flash to indicate destruction
        if (cardImage != null)
        {
            Color originalColor = cardImage.color;
            destructionSequence.Append(cardImage.DOColor(Color.red, 0.15f));
            destructionSequence.Append(cardImage.DOColor(originalColor, 0.15f));
            destructionSequence.Append(cardImage.DOColor(Color.red, 0.15f));
        }
        
        // Shake and scale up before destruction
        destructionSequence.Append(transform.DOShakePosition(0.3f, 20f, 20, 90, false, true));
        destructionSequence.Join(transform.DOScale(transform.localScale * 1.2f, 0.3f));
        
        // Final destruction - fade out and shrink
        if (cardImage != null)
        {
            destructionSequence.Append(cardImage.DOFade(0f, 0.4f));
        }
        destructionSequence.Join(transform.DOScale(Vector3.zero, 0.4f)
            .SetEase(Ease.InQuart));
        
        yield return destructionSequence.WaitForCompletion();
        
        // Destroy the card object after animation completes
        Debug.Log("Destroying tarot card: " + (cardData != null ? cardData.cardName : "Unknown"));
        Destroy(gameObject);
    }
} 