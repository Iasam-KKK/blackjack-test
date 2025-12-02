using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

/// <summary>
/// UI Controller for the Deck Inspector Panel
/// Displays card counts, filters, and a grid of cards in the player's deck
/// </summary>
public class DeckInspectorPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject panelRoot;            // The main panel to show/hide
    public Button closeButton;              // Close button
    
    [Header("Filter Buttons")]
    public Button remainingButton;          // Filter: Remaining cards
    public Button fullDeckButton;           // Filter: Full deck
    public Button actionCardsButton;        // Filter: Action cards only
    public CanvasGroup remainingCanvasGroup;
    public CanvasGroup fullDeckCanvasGroup;
    public CanvasGroup actionCardsCanvasGroup;
    
    [Header("Base Cards - Container 1 (Type Counts)")]
    public TextMeshProUGUI acesCountText;              // Aces count
    public TextMeshProUGUI kqjCountText;               // KQJ (face cards) count  
    public TextMeshProUGUI numberedCardsCountText;     // Numbered cards count
    
    [Header("Base Cards - Container 2 (Suit Counts)")]
    public TextMeshProUGUI spadesCountText;            // Spades count
    public TextMeshProUGUI heartsCountText;            // Hearts count
    public TextMeshProUGUI clubsCountText;             // Clubs count
    public TextMeshProUGUI diamondsCountText;          // Diamonds count
    
    [Header("Individual Card Counts (NumberedCardsColumn)")]
    public TextMeshProUGUI aceIndividualText;          // A count (Var text)
    public TextMeshProUGUI kingIndividualText;         // K count
    public TextMeshProUGUI queenIndividualText;        // Q count
    public TextMeshProUGUI jackIndividualText;         // J count
    public TextMeshProUGUI tenIndividualText;          // 10 count
    public TextMeshProUGUI nineIndividualText;         // 9 count
    public TextMeshProUGUI eightIndividualText;        // 8 count
    public TextMeshProUGUI sevenIndividualText;        // 7 count
    public TextMeshProUGUI sixIndividualText;          // 6 count
    public TextMeshProUGUI fiveIndividualText;         // 5 count
    public TextMeshProUGUI fourIndividualText;         // 4 count
    public TextMeshProUGUI threeIndividualText;        // 3 count
    public TextMeshProUGUI twoIndividualText;          // 2 count
    
    [Header("Card Grid")]
    public Transform cardGridContent;       // Content transform of the scroll view
    public GameObject cardSlotPrefab;       // Prefab for individual card slots
    
    [Header("Action Card Description")]
    public TextMeshProUGUI actionCardDescriptionText;  // Text for action card description
    
    [Header("Configuration")]
    public float activeAlpha = 1.0f;        // Alpha for active filter button
    public float inactiveAlpha = 0.5f;      // Alpha for inactive filter buttons
    public float animationDuration = 0.3f;  // Animation duration for panel open/close
    
    // References
    private PlayerDeck playerDeck;
    private DeckFilterType currentFilter = DeckFilterType.Remaining;
    private List<DeckCardSlot> cardSlots = new List<DeckCardSlot>();
    
    private void Awake()
    {
        // Find PlayerDeck
        playerDeck = FindObjectOfType<PlayerDeck>();
        
        // Setup button listeners
        SetupButtonListeners();
        
        // Hide panel by default
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
    
    private void Start()
    {
        // Subscribe to deck changes
        if (playerDeck != null)
        {
            playerDeck.OnDeckChanged += RefreshUI;
        }
        
        // Set default filter
        SetFilter(DeckFilterType.Remaining);
        
        // Disable action cards button for now (as specified)
        if (actionCardsButton != null)
        {
            actionCardsButton.interactable = false;
            if (actionCardsCanvasGroup != null)
            {
                actionCardsCanvasGroup.alpha = 0.3f;
            }
        }
    }
    
    private void OnDestroy()
    {
        if (playerDeck != null)
        {
            playerDeck.OnDeckChanged -= RefreshUI;
        }
    }
    
    /// <summary>
    /// Setup button click listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
        
        if (remainingButton != null)
        {
            remainingButton.onClick.AddListener(() => SetFilter(DeckFilterType.Remaining));
        }
        
        if (fullDeckButton != null)
        {
            fullDeckButton.onClick.AddListener(() => SetFilter(DeckFilterType.FullDeck));
        }
        
        if (actionCardsButton != null)
        {
            actionCardsButton.onClick.AddListener(() => SetFilter(DeckFilterType.ActionCards));
        }
    }
    
    /// <summary>
    /// Open the deck inspector panel
    /// </summary>
    public void OpenPanel()
    {
        if (panelRoot == null) return;
        
        panelRoot.SetActive(true);
        
        // Animate panel appearance
        CanvasGroup canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animationDuration).SetEase(Ease.OutQuad);
        }
        
        // Scale animation
        panelRoot.transform.localScale = Vector3.one * 0.9f;
        panelRoot.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        
        // Refresh UI
        RefreshUI();
        
        Debug.Log("[DeckInspector] Panel opened");
    }
    
    /// <summary>
    /// Close the deck inspector panel
    /// </summary>
    public void ClosePanel()
    {
        if (panelRoot == null) return;
        
        // Animate panel disappearance
        CanvasGroup canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, animationDuration * 0.5f).SetEase(Ease.InQuad)
                .OnComplete(() => panelRoot.SetActive(false));
        }
        else
        {
            panelRoot.SetActive(false);
        }
        
        Debug.Log("[DeckInspector] Panel closed");
    }
    
    /// <summary>
    /// Toggle panel visibility
    /// </summary>
    public void TogglePanel()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }
    
    /// <summary>
    /// Set the current filter and update UI
    /// </summary>
    public void SetFilter(DeckFilterType filter)
    {
        currentFilter = filter;
        UpdateFilterButtonVisuals();
        RefreshUI();
    }
    
    /// <summary>
    /// Update filter button visuals based on current selection
    /// </summary>
    private void UpdateFilterButtonVisuals()
    {
        // Update Remaining button
        if (remainingCanvasGroup != null)
        {
            float targetAlpha = currentFilter == DeckFilterType.Remaining ? activeAlpha : inactiveAlpha;
            remainingCanvasGroup.DOFade(targetAlpha, 0.2f);
        }
        
        // Update Full Deck button
        if (fullDeckCanvasGroup != null)
        {
            float targetAlpha = currentFilter == DeckFilterType.FullDeck ? activeAlpha : inactiveAlpha;
            fullDeckCanvasGroup.DOFade(targetAlpha, 0.2f);
        }
        
        // Action Cards button stays at low alpha since it's disabled
        // (handled in Start())
    }
    
    /// <summary>
    /// Refresh all UI elements
    /// </summary>
    public void RefreshUI()
    {
        if (playerDeck == null)
        {
            playerDeck = FindObjectOfType<PlayerDeck>();
            if (playerDeck == null)
            {
                Debug.LogWarning("[DeckInspector] PlayerDeck not found!");
                return;
            }
        }
        
        bool remainingOnly = currentFilter == DeckFilterType.Remaining;
        
        // Update type counts (Container 1)
        UpdateTypeCounts(remainingOnly);
        
        // Update suit counts (Container 2)
        UpdateSuitCounts(remainingOnly);
        
        // Update individual card counts
        UpdateIndividualCounts(remainingOnly);
        
        // Update card grid
        UpdateCardGrid();
    }
    
    /// <summary>
    /// Update type counts (Aces, KQJ, Numbered)
    /// </summary>
    private void UpdateTypeCounts(bool remainingOnly)
    {
        if (acesCountText != null)
        {
            acesCountText.text = playerDeck.GetAcesCount(remainingOnly).ToString();
        }
        
        if (kqjCountText != null)
        {
            kqjCountText.text = playerDeck.GetFaceCardsCount(remainingOnly).ToString();
        }
        
        if (numberedCardsCountText != null)
        {
            numberedCardsCountText.text = playerDeck.GetNumberedCardsCount(remainingOnly).ToString();
        }
    }
    
    /// <summary>
    /// Update suit counts
    /// </summary>
    private void UpdateSuitCounts(bool remainingOnly)
    {
        if (spadesCountText != null)
        {
            spadesCountText.text = playerDeck.GetSuitCount(CardSuit.Spades, remainingOnly).ToString();
        }
        
        if (heartsCountText != null)
        {
            heartsCountText.text = playerDeck.GetSuitCount(CardSuit.Hearts, remainingOnly).ToString();
        }
        
        if (clubsCountText != null)
        {
            clubsCountText.text = playerDeck.GetSuitCount(CardSuit.Clubs, remainingOnly).ToString();
        }
        
        if (diamondsCountText != null)
        {
            diamondsCountText.text = playerDeck.GetSuitCount(CardSuit.Diamonds, remainingOnly).ToString();
        }
    }
    
    /// <summary>
    /// Update individual card counts (A, K, Q, J, 10, 9, 8, 7, 6, 5, 4, 3, 2)
    /// </summary>
    private void UpdateIndividualCounts(bool remainingOnly)
    {
        // Ace (value = 1)
        if (aceIndividualText != null)
        {
            aceIndividualText.text = playerDeck.GetRankCount(1, remainingOnly).ToString();
        }
        
        // King (value = 13)
        if (kingIndividualText != null)
        {
            kingIndividualText.text = playerDeck.GetRankCount(13, remainingOnly).ToString();
        }
        
        // Queen (value = 12)
        if (queenIndividualText != null)
        {
            queenIndividualText.text = playerDeck.GetRankCount(12, remainingOnly).ToString();
        }
        
        // Jack (value = 11)
        if (jackIndividualText != null)
        {
            jackIndividualText.text = playerDeck.GetRankCount(11, remainingOnly).ToString();
        }
        
        // 10
        if (tenIndividualText != null)
        {
            tenIndividualText.text = playerDeck.GetRankCount(10, remainingOnly).ToString();
        }
        
        // 9
        if (nineIndividualText != null)
        {
            nineIndividualText.text = playerDeck.GetRankCount(9, remainingOnly).ToString();
        }
        
        // 8
        if (eightIndividualText != null)
        {
            eightIndividualText.text = playerDeck.GetRankCount(8, remainingOnly).ToString();
        }
        
        // 7
        if (sevenIndividualText != null)
        {
            sevenIndividualText.text = playerDeck.GetRankCount(7, remainingOnly).ToString();
        }
        
        // 6
        if (sixIndividualText != null)
        {
            sixIndividualText.text = playerDeck.GetRankCount(6, remainingOnly).ToString();
        }
        
        // 5
        if (fiveIndividualText != null)
        {
            fiveIndividualText.text = playerDeck.GetRankCount(5, remainingOnly).ToString();
        }
        
        // 4
        if (fourIndividualText != null)
        {
            fourIndividualText.text = playerDeck.GetRankCount(4, remainingOnly).ToString();
        }
        
        // 3
        if (threeIndividualText != null)
        {
            threeIndividualText.text = playerDeck.GetRankCount(3, remainingOnly).ToString();
        }
        
        // 2
        if (twoIndividualText != null)
        {
            twoIndividualText.text = playerDeck.GetRankCount(2, remainingOnly).ToString();
        }
    }
    
    /// <summary>
    /// Update the card grid display
    /// </summary>
    private void UpdateCardGrid()
    {
        if (cardGridContent == null)
        {
            Debug.LogWarning("[DeckInspector] Card grid content not assigned!");
            return;
        }
        
        // Get cards to display based on current filter
        List<PlayerDeckCard> cardsToDisplay = playerDeck.GetCardsForDisplay(currentFilter);
        
        // Ensure we have enough card slots
        EnsureCardSlots(cardsToDisplay.Count);
        
        // Update each slot
        for (int i = 0; i < cardSlots.Count; i++)
        {
            if (i < cardsToDisplay.Count)
            {
                cardSlots[i].SetCard(cardsToDisplay[i]);
                cardSlots[i].gameObject.SetActive(true);
            }
            else
            {
                cardSlots[i].SetEmpty();
                cardSlots[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Ensure we have enough card slots in the grid
    /// </summary>
    private void EnsureCardSlots(int count)
    {
        // Create new slots if needed
        while (cardSlots.Count < count)
        {
            GameObject slotObj;
            
            if (cardSlotPrefab != null)
            {
                slotObj = Instantiate(cardSlotPrefab, cardGridContent);
            }
            else
            {
                // Create a basic slot if no prefab
                slotObj = new GameObject($"CardSlot_{cardSlots.Count}");
                slotObj.transform.SetParent(cardGridContent, false);
                
                // Add RectTransform
                RectTransform rect = slotObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(100, 140);
                
                // Add Image for card display
                Image img = slotObj.AddComponent<Image>();
                img.color = Color.white;
            }
            
            // Add or get DeckCardSlot component
            DeckCardSlot slot = slotObj.GetComponent<DeckCardSlot>();
            if (slot == null)
            {
                slot = slotObj.AddComponent<DeckCardSlot>();
            }
            
            cardSlots.Add(slot);
        }
    }
    
    /// <summary>
    /// Show description for an action card
    /// </summary>
    public void ShowActionCardDescription(string description)
    {
        if (actionCardDescriptionText != null)
        {
            actionCardDescriptionText.text = description;
        }
    }
    
    /// <summary>
    /// Clear action card description
    /// </summary>
    public void ClearActionCardDescription()
    {
        if (actionCardDescriptionText != null)
        {
            actionCardDescriptionText.text = "select an action card\nto view description";
        }
    }
}

