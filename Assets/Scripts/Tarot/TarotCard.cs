using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;
using System.Collections;

public class TarotCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Card Data")]
    public TarotCardData cardData;
    
    [Header("UI Components")]
    public Image cardImage;
    public Image materialBackground; // Background image for material
    public Text cardNameText;
    public Text priceText;
    public TextMeshProUGUI durabilityText; // Display remaining uses
    
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
        
        // Find material background if not assigned
        if (materialBackground == null)
        {
            materialBackground = GetComponentInChildren<Image>();
            if (materialBackground != null && materialBackground.name != "MaterialBackground")
            {
                // Look specifically for MaterialBackground child
                Transform bgTransform = transform.Find("MaterialBackground");
                if (bgTransform != null)
                {
                    materialBackground = bgTransform.GetComponent<Image>();
                }
            }
        }
        
        // Just update display - don't reset material durability
        UpdateCardDisplay();
    }
    
    // Update card visuals based on data
    public void UpdateCardDisplay()
    {
        Debug.Log($"[TarotCard] UpdateCardDisplay called. CardData: {(cardData != null ? cardData.cardName : "null")}");
        
        if (cardData != null)
        {
            // Update image and texts
            if (cardImage != null && cardData.cardImage != null)
            {
                cardImage.sprite = cardData.cardImage;
                Debug.Log($"[TarotCard] Updated card image for {cardData.cardName}");
            }
            
            // Update material background
            if (materialBackground != null)
            {
                if (cardData.assignedMaterial != null)
                {
                    // Prioritize background sprite over color tint
                    Sprite materialSprite = cardData.GetMaterialBackgroundSprite();
                    if (materialSprite != null)
                    {
                        // Use the material background image
                        materialBackground.sprite = materialSprite;
                        materialBackground.color = Color.white; // Use sprite at full opacity
                        materialBackground.type = Image.Type.Simple; // Ensure proper display
                        materialBackground.preserveAspect = false; // Stretch to fit card
                    }
                    else
                    {
                        // Fallback to color tint if no sprite is available
                        materialBackground.sprite = null;
                        materialBackground.color = cardData.GetMaterialColor();
                    }
                    materialBackground.gameObject.SetActive(true);
                    
                    // Debug log to help with troubleshooting
                    string materialInfo = materialSprite != null ? 
                        $"Using sprite: {materialSprite.name}" : 
                        $"Using color tint: {cardData.GetMaterialColor()}";
                    Debug.Log($"Material background for {cardData.cardName}: {materialInfo}");
                }
                else
                {
                    materialBackground.gameObject.SetActive(false);
                }
            }
            
            if (cardNameText != null)
            {
                cardNameText.text = cardData.cardName;
                Debug.Log($"[TarotCard] Updated card name text to: {cardData.cardName}");
            }
            
            if (priceText != null)
            {
                if (isInShop)
                {
                    /*priceText.text = cardData.price + "$";
                    priceText.gameObject.SetActive(true);*/
                    priceText.text = GetFinalPrice() + "$";
                    priceText.gameObject.SetActive(true);
                }
                else
                {
                    priceText.gameObject.SetActive(false);
                }
            }
            
            // Update durability display
            if (durabilityText != null)
            {
                int remainingUses = cardData.GetRemainingUses();
                if (remainingUses == -1)
                {
                    durabilityText.text = "∞"; // Unlimited uses
                }
                else
                {
                    durabilityText.text = remainingUses.ToString();
                }
                
                // Show durability only when not in shop
                if (isInShop)
                {
                   // durabilityText.gameObject.SetActive(false);
                }
                else
                {
                    durabilityText.gameObject.SetActive(true);
                    
                    // Color code durability: red when low, yellow when medium, green when high
                    if (remainingUses == -1)
                    {
                        durabilityText.color = Color.cyan; // Special color for unlimited
                    }
                    else if (remainingUses <= 1)
                    {
                        durabilityText.color = Color.red;
                    }
                    else if (remainingUses <= 3)
                    {
                        durabilityText.color = Color.yellow;
                    }
                    else
                    {
                        durabilityText.color = Color.green;
                    }
                }
            }
            
            // Visual state for used cards or cards that can't be used
            bool canBeUsed = cardData.CanBeUsed() && (!hasBeenUsedThisRound || cardData.isReusable);
            bool isActivatedThisRound = IsCardActivatedThisRound();
            
            if (!canBeUsed || isActivatedThisRound)
            {
                cardImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Grayed out
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
        // Visual feedback
        transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad);
    }
    
    // Called when the mouse exits the card area
    public void OnPointerExit(PointerEventData eventData)
    {
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
        else if (cardData.CanBeUsed() && (!hasBeenUsedThisRound || cardData.isReusable) && !IsCardActivatedThisRound())
        {
            TryUseCard();
        }
        else if (!cardData.CanBeUsed())
        {
            // Card is out of durability
            transform.DOShakePosition(0.3f, 5f, 10, 90, false, true);
            Debug.Log("Card has no remaining uses! Material: " + cardData.GetMaterialDisplayName() + 
                     ", Uses: " + cardData.currentUses + "/" + cardData.maxUses);
        }
    }
    
    // BETTING SYSTEM 2.0: Try to purchase the card from the shop (using health as currency)
    public void TryPurchaseCard()
    {
        uint cost = GetFinalPrice();
        // Convert cost to health (every $10 = 1 health point)
        float healthCost = cost / 10f;
        
        if (deck != null && GameProgressionManager.Instance != null)
        {
            float currentHealth = GameProgressionManager.Instance.playerHealthPercentage;
            if (currentHealth >= healthCost)
            {
                // Start purchase animation sequence
                StartCoroutine(AnimatedPurchaseSequence(cost, healthCost));
            }
            else
            {
                // Not enough health - show feedback
                transform.DOShakePosition(0.5f, 10, 10, 90, false, true);
                Debug.Log($"Not enough health to purchase card. Health: {currentHealth:F0}, Cost: {healthCost:F0}");
            }
        }
    }
    
    private IEnumerator AnimatedPurchaseSequence(uint cost, float healthCost)
    {
        // BETTING SYSTEM 2.0: Deduct health immediately
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.DamagePlayer(healthCost);
        }
        deck.OnCardPurchased(cost);
        
        // Ensure shopManager reference
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
        }
        if (InventoryManagerV3.Instance != null && InventoryManagerV3.Instance.shopManager == null)
        {
            InventoryManagerV3.Instance.shopManager = shopManager;
        }
        
        // Add to inventory
        bool addedToInventory = false;
        int storageSlotIndex = -1;
        int equipmentSlotIndex = -1;
        
        if (InventoryManagerV3.Instance != null && InventoryManagerV3.Instance.inventoryData != null && cardData != null)
        {
            addedToInventory = InventoryManagerV3.Instance.AddPurchasedCard(cardData);
            
            if (addedToInventory)
            {
                // Find which storage slot it went to
                var storageSlots = InventoryManagerV3.Instance.inventoryData.storageSlots;
                for (int i = 0; i < storageSlots.Count; i++)
                {
                    if (storageSlots[i].isOccupied && storageSlots[i].storedCard == cardData)
                    {
                        storageSlotIndex = i;
                        break;
                    }
                }
                
                // Check for available equipment slot
                var equipmentSlots = InventoryManagerV3.Instance.inventoryData.equipmentSlots;
                for (int i = 0; i < equipmentSlots.Count; i++)
                {
                    if (!equipmentSlots[i].isOccupied)
                    {
                        equipmentSlotIndex = i;
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("Failed to add card to inventory - inventory might be full!");
            }
        }
        
        // Add to PlayerStats for compatibility
        if (PlayerStats.instance != null && cardData != null)
        {
            PlayerStats.instance.ownedCards.Add(cardData);
        }
        
        // Notify shop manager
        if (shopManager != null)
        {
            shopManager.OnCardPurchased(this);
        }
        
        // Destroy the shop card (always destroy after purchase)
        Debug.Log($"Purchase complete - {(addedToInventory ? "card added to inventory" : "inventory full")} - destroying shop card");
        Destroy(gameObject);
        
        yield return null; // End coroutine
    }
    // Get final price depending on material
    // SOUL CURRENCY: Adjusted prices for health-based economy
    public uint GetFinalPrice()
    {
        int finalPrice = 30; // Reduced base price for soul currency (was 100)

        if (cardData.assignedMaterial != null)
        {
            // Adjusted multipliers for soul currency - more affordable
            switch (cardData.assignedMaterial.materialType)
            {
                case TarotMaterialType.Paper:
                    finalPrice = 30; // 3 health - Common, cheap
                    break;
                case TarotMaterialType.Cardboard:
                    finalPrice = 50; // 5 health - Uncommon
                    break;
                case TarotMaterialType.Wood:
                    finalPrice = 70; // 7 health - Rare
                    break;
                case TarotMaterialType.Copper:
                    finalPrice = 100; // 10 health - Very Rare
                    break;
                case TarotMaterialType.Silver:
                    finalPrice = 150; // 15 health - Epic
                    break;
                case TarotMaterialType.Gold:
                    finalPrice = 200; // 20 health - Legendary
                    break;
                case TarotMaterialType.Platinum:
                    finalPrice = 250; // 25 health - Mythic
                    break;
                case TarotMaterialType.Diamond:
                    finalPrice = 300; // 30 health - Ultimate (unlimited uses)
                    break;
            }
        }

        return (uint)finalPrice;
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
        
        // Check if passive card is already activated this round
        if (cardData.IsPassiveCard() && IsCardActivatedThisRound())
        {
            Debug.Log("Card has already been activated this round");
            return;
        }
        
        // Check material durability
        if (!cardData.CanBeUsed())
        {
            Debug.Log("Card has no remaining uses! Material durability exhausted.");
            return;
        }
        
        // Check tarot usage limit per hand
        if (deck != null && !deck.CanUseTarot())
        {
            Debug.Log($"Cannot use tarot: Limit reached this hand");
            return;
        }
        
        // ✅ NEW: Allow card-removal cards to be used as "rescue" even after round ends
        bool isRescueCard = cardData.cardType == TarotCardType.Scavenger ||
                           cardData.cardType == TarotCardType.Gardener ||
                           cardData.cardType == TarotCardType.BetrayedCouple ||
                           cardData.cardType == TarotCardType.Blacksmith ||
                           cardData.cardType == TarotCardType.TaxCollector;
        
        // If it's a rescue card and game is over but player lost, allow usage
        if (isRescueCard && deck != null)
        {
            bool gameOver = !deck.hitButton.interactable && !deck.stickButton.interactable;
            int playerPoints = deck.GetPlayerPoints();
            int dealerPoints = deck.GetDealerPoints();
            
            if (gameOver && playerPoints > 21)
            {
                Debug.Log($"Rescue card {cardData.cardName} can be used - player is busted!");
            }
            else if (gameOver && playerPoints <= dealerPoints && dealerPoints <= 21)
            {
                Debug.Log($"Rescue card {cardData.cardName} can be used - dealer is winning!");
            }
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
                    if (!deck._hasActivatedWitchDoctorThisRound)
                    {
                        deck._hasActivatedWitchDoctorThisRound = true;
                        Debug.Log("Witch Doctor activated! Will provide 10% refund on losses for this round");
                        PlaySimpleActivationEffect();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Witch Doctor already activated this round");
                    }
                    break;
                case TarotCardType.Artificer:
                    if (!deck._hasActivatedArtificerThisRound)
                    {
                        deck._hasActivatedArtificerThisRound = true;
                        Debug.Log("Artificer activated! Will boost win multiplier by 10% when you have a streak for this round");
                        PlaySimpleActivationEffect();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("Artificer already activated this round");
                    }
                    break;
                case TarotCardType.Botanist:
                    if (!deck._hasActivatedBotanistThisRound)
                    {
                        deck._hasActivatedBotanistThisRound = true;
                        Debug.Log("The Botanist activated! Will provide +50 bonus per club in winning hands for this round");
                        PlaySimpleActivationEffect();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("The Botanist already activated this round");
                    }
                    break;
                case TarotCardType.Assassin:
                    if (!deck._hasActivatedAssassinThisRound)
                    {
                        deck._hasActivatedAssassinThisRound = true;
                        Debug.Log("The Assassin activated! Will provide +50 bonus per spade in winning hands for this round");
                        PlaySimpleActivationEffect();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("The Assassin already activated this round");
                    }
                    break;
                case TarotCardType.SecretLover:
                    if (!deck._hasActivatedSecretLoverThisRound)
                    {
                        deck._hasActivatedSecretLoverThisRound = true;
                        Debug.Log("The Secret Lover activated! Will provide +50 bonus per heart in winning hands for this round");
                        PlaySimpleActivationEffect();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("The Secret Lover already activated this round");
                    }
                    break;
                case TarotCardType.Jeweler:
                    if (!deck._hasActivatedJewelerThisRound)
                    {
                        deck._hasActivatedJewelerThisRound = true;
                        Debug.Log("The Jeweler activated! Will provide +50 bonus per diamond in winning hands for this round");
                        PlaySimpleActivationEffect();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("The Jeweler already activated this round");
                    }
                    break;
                case TarotCardType.CursedHourglass:
                    if (!hasBeenUsedThisRound)
                    {
                        // Check if bet has been placed
                        if (!deck._isBetPlaced)
                        {
                            Debug.Log("[CursedHourglass] Cannot use - no bet placed");
                            break;
                        }
                        
                        // BETTING SYSTEM 2.0: Deduct half of the current bet from the player's health
                        float halfBet = deck.CurrentBetAmount / 2f;
                        if (GameProgressionManager.Instance != null)
                        {
                            GameProgressionManager.Instance.DamagePlayer(halfBet);
                        }
                        
                        // Update bet to remaining half
                        deck.CurrentBetAmount /= 2;
                        Debug.Log($"[CursedHourglass] Deducted half of the bet: -{halfBet:F0}");

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
                        // BETTING SYSTEM 2.0: Deduct ¼ of the current bet from health
                        float quarterBet = deck.CurrentBetAmount * 0.25f;
                        if (GameProgressionManager.Instance != null)
                        {
                            GameProgressionManager.Instance.DamagePlayer(quarterBet);
                        }
                        Debug.Log($"[WhisperOfThePast] Deducted ¼ of the bet: -{quarterBet:F0}");

                        // Activate the card's effect
                        Debug.Log("[WhisperOfThePast] Removing player cards and re-dealing...");
                        deck.StartCoroutine(deck.ActivateWhisperOfThePastEffect());

                        effectApplied = true;
                    }
                    break;
                case TarotCardType.Saboteur:
                    if (!hasBeenUsedThisRound)
                    {
                        // BETTING SYSTEM 2.0: Deduct ¼ of the current bet from health
                        float quarterBet = deck.CurrentBetAmount * 0.25f;
                        if (GameProgressionManager.Instance != null)
                        {
                            GameProgressionManager.Instance.DamagePlayer(quarterBet);
                        }
                        Debug.Log($"[Saboteur] Deducted ¼ of the bet: -{quarterBet:F0}");

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

                        // BETTING SYSTEM 2.0: Deduct half of the bet from health
                        float halfBet = deck.CurrentBetAmount * 0.5f;
                        if (GameProgressionManager.Instance != null)
                        {
                            GameProgressionManager.Instance.DamagePlayer(halfBet);
                        }

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
                                        Debug.Log("========== The Scavenger ALL ANIMATIONS COMPLETE ==========");
                                        
                                        // Rearrange remaining cards and update points
                                        playerHand.ArrangeCardsInWindow();
                                        playerHand.UpdatePoints();
                                        
                                        // Update displays after all cards are removed
                                        deck.UpdateScoreDisplays();
                                        deck.UpdateDiscardButtonState();
                                        deck.UpdateTransformButtonState();
                                        
                                        // ✅ FIX: Re-evaluate game state and re-enable controls FOR RESCUE
                                        int playerPoints = deck.GetPlayerPoints();
                                        int dealerPoints = deck.GetDealerPoints();
                                        
                                        Debug.Log("========== The Scavenger RESCUE CHECK ==========");
                                        Debug.Log($"Player Points: {playerPoints}");
                                        Debug.Log($"Dealer Points: {dealerPoints}");
                                        Debug.Log($"Game In Progress: {deck._gameInProgress}");
                                        Debug.Log($"Player Stood: {deck._playerStood}");
                                        Debug.Log($"Dealer Stood: {deck._dealerStood}");
                                        Debug.Log($"Hit Button Interactable: {deck.hitButton.interactable}");
                                        Debug.Log($"Stick Button Interactable: {deck.stickButton.interactable}");
                                        
                                        // Always re-enable controls if player has valid hand
                                        if (playerPoints > 0 && playerPoints <= 21)
                                        {
                                            Debug.Log("========== The Scavenger RESCUE ACTIVATED! ==========");
                                            Debug.Log("Player has valid hand - forcing game to continue");
                                            
                                            // Force game state back to active
                                            deck._gameInProgress = true;
                                            deck._playerStood = false;
                                            deck._dealerStood = false;
                                            deck._currentTurn = Deck.GameTurn.Player;
                                            
                                            Debug.Log("Re-enabling all player controls...");
                                            
                                            // Re-enable all player action buttons
                                            deck.hitButton.interactable = true;
                                            deck.stickButton.interactable = true;
                                            deck.UpdateDiscardButtonState();
                                            deck.UpdatePeekButtonState();
                                            deck.UpdateTransformButtonState();
                                            
                                            // Disable play again button (was enabled after loss)
                                            if (deck.playAgainButton != null)
                                            {
                                                deck.playAgainButton.interactable = false;
                                                Debug.Log("Disabled Play Again button");
                                            }
                                            
                                            // Show rescue message
                                            if (deck.finalMessage != null)
                                            {
                                                deck.finalMessage.text = "⚡ The Scavenger rescued you! ⚡\nContinue playing...";
                                                deck.finalMessage.gameObject.SetActive(true);
                                            }
                                            
                                            Debug.Log("========== The Scavenger RESCUE COMPLETE - GAME RESUMED! ==========");
                                        }
                                        else
                                        {
                                            Debug.Log("========== The Scavenger CANNOT RESCUE ==========");
                                            Debug.Log($"Player points {playerPoints} is not valid (must be 1-21)");
                                        }
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
                    if (!deck._hasActivatedHouseKeeperThisRound)
                    {
                        deck._hasActivatedHouseKeeperThisRound = true;
                        Debug.Log("The House Keeper activated! Will provide +10 bonus per Jack/Queen/King in winning hands for this round");
                        PlaySimpleActivationEffect();
                        effectApplied = true;
                    }
                    else
                    {
                        Debug.Log("The House Keeper already activated this round");
                    }
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
            
            // Handle card usage and durability
            if (effectApplied)
            {
                // Mark as used this round if not reusable and not a passive card
                if (!cardData.isReusable && !cardData.IsPassiveCard())
                {
                    hasBeenUsedThisRound = true;
                }
                
            // Update durability in the central inventory system
            if (InventoryManagerV3.Instance != null)
            {
                InventoryManagerV3.Instance.UseEquippedCard(cardData);
            }
            else
            {
                // Fallback if no inventory manager
                cardData.UseCard();
            }
            
            UpdateCardDisplay();
                
                // Check if card is completely used up (no durability left)
                if (!cardData.CanBeUsed())
                {
                    Debug.Log("Card " + cardData.cardName + " has been completely used up! Material: " + 
                             cardData.GetMaterialDisplayName());
                    
                    // Remove from PlayerStats (for compatibility)
                    if (PlayerStats.instance != null && PlayerStats.instance.ownedCards != null && cardData != null)
                    {
                        PlayerStats.instance.ownedCards.Remove(cardData);
                        Debug.Log("Removed " + cardData.cardName + " from player's owned cards - durability exhausted");
                    }
                    
                    // Remove from Inventory System
                    if (InventoryManagerV3.Instance != null && cardData != null)
                    {
                        InventoryManagerV3.Instance.RemoveUsedUpCard(cardData);
                    }
                    
                    // Animate the card destruction
                    StartCoroutine(AnimateCardDestruction());
                }
                else
                {
                    Debug.Log("Card " + cardData.cardName + " used. Remaining uses: " + 
                             cardData.GetRemainingUses() + " (" + cardData.GetMaterialDisplayName() + ")");
                }
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
                        Debug.Log($"========== {cardName} ALL ANIMATIONS COMPLETE ==========");
                        
                        playerHand.ArrangeCardsInWindow();
                        playerHand.UpdatePoints();
                        dealerHand.ArrangeCardsInWindow();
                        dealerHand.UpdatePoints();
                        
                        // Update displays after all cards are removed
                        deck.UpdateScoreDisplays();
                        deck.UpdateDiscardButtonState();
                        deck.UpdateTransformButtonState();
                        
                        // ✅ FIX: Re-evaluate game state and re-enable controls FOR RESCUE
                        int playerPoints = deck.GetPlayerPoints();
                        int dealerPoints = deck.GetDealerPoints();
                        
                        Debug.Log($"========== {cardName} RESCUE CHECK ==========");
                        Debug.Log($"Player Points: {playerPoints}");
                        Debug.Log($"Dealer Points: {dealerPoints}");
                        Debug.Log($"Game In Progress: {deck._gameInProgress}");
                        Debug.Log($"Player Stood: {deck._playerStood}");
                        Debug.Log($"Dealer Stood: {deck._dealerStood}");
                        Debug.Log($"Hit Button Interactable: {deck.hitButton.interactable}");
                        Debug.Log($"Stick Button Interactable: {deck.stickButton.interactable}");
                        
                        // Always re-enable controls if player has valid hand
                        if (playerPoints > 0 && playerPoints <= 21)
                        {
                            Debug.Log($"========== {cardName} RESCUE ACTIVATED! ==========");
                            Debug.Log("Player has valid hand - forcing game to continue");
                            
                            // Force game state back to active
                            deck._gameInProgress = true;
                            deck._playerStood = false;
                            deck._dealerStood = false;
                            deck._currentTurn = Deck.GameTurn.Player;
                            
                            Debug.Log("Re-enabling all player controls...");
                            
                            // Re-enable all player action buttons
                            deck.hitButton.interactable = true;
                            deck.stickButton.interactable = true;
                            deck.UpdateDiscardButtonState();
                            deck.UpdatePeekButtonState();
                            deck.UpdateTransformButtonState();
                            
                            // Disable play again button (was enabled after loss)
                            if (deck.playAgainButton != null)
                            {
                                deck.playAgainButton.interactable = false;
                                Debug.Log("Disabled Play Again button");
                            }
                            
                            // Show rescue message
                            if (deck.finalMessage != null)
                            {
                                deck.finalMessage.text = $"⚡ {cardName} rescued you! ⚡\nContinue playing...";
                                deck.finalMessage.gameObject.SetActive(true);
                            }
                            
                            Debug.Log($"========== {cardName} RESCUE COMPLETE - GAME RESUMED! ==========");
                        }
                        else
                        {
                            Debug.Log($"========== {cardName} CANNOT RESCUE ==========");
                            Debug.Log($"Player points {playerPoints} is not valid (must be 1-21)");
                        }
                    }
                });
            }
        }
        else
        {
            Debug.Log($"No {suitName} cards found in either hand.");
        }
    }
    
    // Check if this passive card has been activated this round
    private bool IsCardActivatedThisRound()
    {
        if (deck == null || cardData == null) return false;
        
        switch (cardData.cardType)
        {
            case TarotCardType.Botanist:
                return deck._hasActivatedBotanistThisRound;
            case TarotCardType.Assassin:
                return deck._hasActivatedAssassinThisRound;
            case TarotCardType.SecretLover:
                return deck._hasActivatedSecretLoverThisRound;
            case TarotCardType.Jeweler:
                return deck._hasActivatedJewelerThisRound;
            case TarotCardType.HouseKeeper:
                return deck._hasActivatedHouseKeeperThisRound;
            case TarotCardType.WitchDoctor:
                return deck._hasActivatedWitchDoctorThisRound;
            case TarotCardType.Artificer:
                return deck._hasActivatedArtificerThisRound;
            default:
                return false;
        }
    }
    
    // Reset the card for a new round
    public void ResetForNewRound()
    {
        hasBeenUsedThisRound = false;
        cardImage.color = Color.white;
    }
    
    // Play simple activation effect for passive cards
    private void PlaySimpleActivationEffect()
    {
        // Simple immediate feedback - flash green then gray out
        if (cardImage != null)
        {
            // Quick green flash using DOTween
            cardImage.DOColor(Color.green, 0.1f).OnComplete(() => {
                cardImage.DOColor(new Color(0.5f, 0.5f, 0.5f, 0.8f), 0.2f);
            });
            
            // Quick scale bounce
            transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 1, 0.5f);
        }
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