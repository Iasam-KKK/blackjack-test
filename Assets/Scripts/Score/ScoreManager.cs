using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages score calculation and display for player and dealer
/// Shows score as text and visual fill slider (progress to 21)
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    
    [Header("Player Score UI")]
    [Tooltip("Text component for player score")]
    public TextMeshProUGUI playerScoreText;
    public Text playerScoreTextLegacy;
    
    [Tooltip("Fill image for player score progress to 21")]
    public Image playerScoreFill;
    
    [Header("Dealer Score UI")]
    [Tooltip("Text component for dealer score")]
    public TextMeshProUGUI dealerScoreText;
    public Text dealerScoreTextLegacy;
    
    [Tooltip("Fill image for dealer score progress to 21")]
    public Image dealerScoreFill;
    
    [Header("Card Hand References")]
    [Tooltip("Player's card hand")]
    public GameObject playerHandObject;
    
    [Tooltip("Dealer's card hand")]
    public GameObject dealerHandObject;
    
    [Header("Settings")]
    [Tooltip("Target score (21 in blackjack)")]
    public int targetScore = 21;
    
    [Tooltip("Cap fill amount at 100% even if over 21")]
    public bool capFillAtMax = true;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private void Awake()
    {
        // Singleton pattern (scene-specific, not persistent)
        if (Instance == null)
        {
            Instance = this;
            Log("[ScoreManager] Instance created");
        }
        else
        {
            LogWarning($"[ScoreManager] Duplicate instance detected, destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Validate references
        ValidateReferences();
        
        // Initial score display
        UpdateAllScores();
    }
    
    /// <summary>
    /// Validate that required references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (playerHandObject == null)
        {
            LogWarning("[ScoreManager] Player hand object not assigned! Assign it in Inspector.");
        }
        else
        {
            CardHand playerHand = playerHandObject.GetComponent<CardHand>();
            if (playerHand == null)
            {
                LogWarning($"[ScoreManager] Player hand object '{playerHandObject.name}' does not have CardHand component!");
            }
            else
            {
                Log($"[ScoreManager] Player hand found on '{playerHandObject.name}'");
            }
        }
        
        if (dealerHandObject == null)
        {
            LogWarning("[ScoreManager] Dealer hand object not assigned! Assign it in Inspector.");
        }
        else
        {
            CardHand dealerHand = dealerHandObject.GetComponent<CardHand>();
            if (dealerHand == null)
            {
                LogWarning($"[ScoreManager] Dealer hand object '{dealerHandObject.name}' does not have CardHand component!");
            }
            else
            {
                Log($"[ScoreManager] Dealer hand found on '{dealerHandObject.name}'");
            }
        }
        
        if (playerScoreFill == null)
        {
            LogWarning("[ScoreManager] Player score fill image not assigned!");
        }
        
        if (dealerScoreFill == null)
        {
            LogWarning("[ScoreManager] Dealer score fill image not assigned!");
        }
        
        if (playerScoreText == null && playerScoreTextLegacy == null)
        {
            LogWarning("[ScoreManager] Player score text not assigned!");
        }
        
        if (dealerScoreText == null && dealerScoreTextLegacy == null)
        {
            LogWarning("[ScoreManager] Dealer score text not assigned!");
        }
    }
    
    /// <summary>
    /// Calculate player's visible score
    /// </summary>
    public int CalculatePlayerScore()
    {
        return CalculateVisibleScore(playerHandObject, true);
    }
    
    /// <summary>
    /// Calculate dealer's visible score (excludes face-down cards)
    /// </summary>
    public int CalculateDealerScore()
    {
        return CalculateVisibleScore(dealerHandObject, false);
    }
    
    /// <summary>
    /// Calculate visible score for a hand (handles face-down dealer card)
    /// Based on Deck.GetVisibleScore() logic
    /// </summary>
    private int CalculateVisibleScore(GameObject handOwner, bool isPlayer)
    {
        if (handOwner == null)
        {
            LogWarning($"[ScoreManager] Hand owner is null for {(isPlayer ? "Player" : "Dealer")}");
            return 0;
        }
        
        CardHand hand = handOwner.GetComponent<CardHand>();
        if (hand == null)
        {
            LogWarning($"[ScoreManager] CardHand component not found on {handOwner.name}. Make sure player/dealer references have CardHand component!");
            return 0;
        }
        
        Log($"[ScoreManager] Calculating score for {(isPlayer ? "Player" : "Dealer")}, cards in hand: {hand.cards.Count}");
        
        int visibleScore = 0;
        int aces = 0;
        
        foreach (GameObject cardGO in hand.cards)
        {
            CardModel cardModel = cardGO.GetComponent<CardModel>();
            Image cardImage = cardGO.GetComponent<Image>();
            
            if (cardImage == null || cardModel == null)
                continue;
            
            // Only count face-up cards (showing cardFront)
            if (cardImage.sprite == cardModel.cardFront)
            {
                if (cardModel.value == 1) // Ace
                {
                    aces++;
                    visibleScore += Constants.SoftAce; // Add as 11 for now
                }
                else
                {
                    visibleScore += cardModel.value;
                }
            }
        }
        
        // Adjust for Aces: if score > 21 and there are Aces, convert Aces from 11 to 1
        while (visibleScore > Constants.Blackjack && aces > 0)
        {
            visibleScore -= (Constants.SoftAce - 1); // Subtract 10 (11 - 1)
            aces--;
        }
        
        return visibleScore;
    }
    
    /// <summary>
    /// Calculate actual score of all cards regardless of visibility
    /// Useful for blackjack detection at game start
    /// </summary>
    public int CalculateActualScore(GameObject handOwner)
    {
        if (handOwner == null)
        {
            LogWarning("[ScoreManager] Hand owner is null");
            return 0;
        }
        
        CardHand hand = handOwner.GetComponent<CardHand>();
        if (hand == null)
        {
            LogWarning("[ScoreManager] CardHand component not found");
            return 0;
        }
        
        int actualScore = 0;
        int aces = 0;
        
        foreach (GameObject cardGO in hand.cards)
        {
            CardModel cardModel = cardGO.GetComponent<CardModel>();
            
            if (cardModel == null)
                continue;
            
            if (cardModel.value == 1) // Ace
            {
                aces++;
                actualScore += Constants.SoftAce; // Add as 11 for now
            }
            else
            {
                actualScore += cardModel.value;
            }
        }
        
        // Adjust for Aces: if score > 21 and there are Aces, convert Aces from 11 to 1
        while (actualScore > Constants.Blackjack && aces > 0)
        {
            actualScore -= (Constants.SoftAce - 1); // Subtract 10 (11 - 1)
            aces--;
        }
        
        return actualScore;
    }
    
    /// <summary>
    /// Update all score displays (player and dealer)
    /// </summary>
    public void UpdateAllScores()
    {
        UpdatePlayerScoreDisplay();
        UpdateDealerScoreDisplay();
    }
    
    /// <summary>
    /// Update player score display (text and fill slider)
    /// </summary>
    public void UpdatePlayerScoreDisplay()
    {
        int score = CalculatePlayerScore();
        
        // Update text (just the number, no "Score:" prefix)
        string scoreString = $"{score}";
        if (playerScoreText != null)
        {
            playerScoreText.text = scoreString;
        }
        else if (playerScoreTextLegacy != null)
        {
            playerScoreTextLegacy.text = scoreString;
        }
        
        // Update fill slider
        if (playerScoreFill != null)
        {
            float fillAmount = (float)score / targetScore;
            
            // Optionally cap at 100%
            if (capFillAtMax)
            {
                fillAmount = Mathf.Min(fillAmount, 1f);
            }
            
            playerScoreFill.fillAmount = fillAmount;
            Log($"[ScoreManager] Player score: {score} (fill: {fillAmount:F2})");
        }
    }
    
    /// <summary>
    /// Update dealer score display (text and fill slider)
    /// </summary>
    public void UpdateDealerScoreDisplay()
    {
        int score = CalculateDealerScore();
        
        // Update text (just the number, no "Score:" prefix)
        string scoreString = $"{score}";
        if (dealerScoreText != null)
        {
            dealerScoreText.text = scoreString;
        }
        else if (dealerScoreTextLegacy != null)
        {
            dealerScoreTextLegacy.text = scoreString;
        }
        
        // Update fill slider
        if (dealerScoreFill != null)
        {
            float fillAmount = (float)score / targetScore;
            
            // Optionally cap at 100%
            if (capFillAtMax)
            {
                fillAmount = Mathf.Min(fillAmount, 1f);
            }
            
            dealerScoreFill.fillAmount = fillAmount;
            Log($"[ScoreManager] Dealer score: {score} (fill: {fillAmount:F2})");
        }
    }
    
    /// <summary>
    /// Manually set hand references (for runtime setup)
    /// </summary>
    public void SetHandReferences(GameObject player, GameObject dealer)
    {
        playerHandObject = player;
        dealerHandObject = dealer;
        Log("[ScoreManager] Hand references updated");
        UpdateAllScores();
    }
    
    /// <summary>
    /// Check if player has blackjack (visible cards)
    /// </summary>
    public bool PlayerHasBlackjack()
    {
        return CalculatePlayerScore() == Constants.Blackjack;
    }
    
    /// <summary>
    /// Check if dealer has blackjack (visible cards)
    /// </summary>
    public bool DealerHasBlackjack()
    {
        return CalculateDealerScore() == Constants.Blackjack;
    }
    
    /// <summary>
    /// Check if player has bust
    /// </summary>
    public bool PlayerHasBust()
    {
        return CalculatePlayerScore() > Constants.Blackjack;
    }
    
    /// <summary>
    /// Check if dealer has bust
    /// </summary>
    public bool DealerHasBust()
    {
        return CalculateDealerScore() > Constants.Blackjack;
    }
    
    // Debug logging helpers
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    private void LogWarning(string message)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning(message);
        }
    }
}

