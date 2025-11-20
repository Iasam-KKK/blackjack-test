using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Manages betting system where player bets their "soul" (health percentage)
/// Integrates with GameProgressionManager for balance and HealthBarManager for visuals
/// </summary>
public class BettingManager : MonoBehaviour
{
    public static BettingManager Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("Input field for manual bet entry")]
    public TMP_InputField betInputField;
    
    [Tooltip("Quick bet buttons")]
    public Button betButton5;
    public Button betButton10;
    public Button betButton25;
    public Button betButton50;
    public Button betButton100;
    
    [Tooltip("Confirm bet button")]
    public Button placeBetButton;
    
    [Tooltip("Display current balance (soul %)")]
    public TextMeshProUGUI balanceText;
    public Text balanceTextLegacy; // Fallback for legacy Text
    
    [Tooltip("Display current bet amount")]
    public TextMeshProUGUI currentBetText;
    public Text currentBetTextLegacy; // Fallback for legacy Text
    
    [Header("Betting Constraints")]
    [Tooltip("Minimum bet amount")]
    public float minBet = 1f;
    
    [Tooltip("Maximum bet amount (will be capped by player health)")]
    public float maxBet = 100f;
    
    [Header("References")]
    [Tooltip("Reference to Deck script to start game after bet")]
    public Deck deckScript;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // Current bet amount
    private float currentBetAmount = 0f;
    
    // Betting state
    private bool isBettingActive = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            Log("[BettingManager] Instance created");
        }
        else
        {
            LogWarning($"[BettingManager] Duplicate instance detected, destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Setup UI listeners
        SetupUIListeners();
        
        // Find Deck script if not assigned
        if (deckScript == null)
        {
            deckScript = FindObjectOfType<Deck>();
            if (deckScript != null)
            {
                Log("[BettingManager] Deck script found automatically");
            }
            else
            {
                LogWarning("[BettingManager] Deck script not found! Assign it in Inspector.");
            }
        }
        
        // Initial UI update
        UpdateBalanceDisplay();
        UpdateCurrentBetDisplay();
        UpdatePlaceBetButtonState();
    }
    
    /// <summary>
    /// Setup all UI button listeners
    /// </summary>
    private void SetupUIListeners()
    {
        // Quick bet buttons
        if (betButton5 != null)
        {
            betButton5.onClick.RemoveAllListeners();
            betButton5.onClick.AddListener(() => SetBetAmount(5f));
        }
        
        if (betButton10 != null)
        {
            betButton10.onClick.RemoveAllListeners();
            betButton10.onClick.AddListener(() => SetBetAmount(10f));
        }
        
        if (betButton25 != null)
        {
            betButton25.onClick.RemoveAllListeners();
            betButton25.onClick.AddListener(() => SetBetAmount(25f));
        }
        
        if (betButton50 != null)
        {
            betButton50.onClick.RemoveAllListeners();
            betButton50.onClick.AddListener(() => SetBetAmount(50f));
        }
        
        if (betButton100 != null)
        {
            betButton100.onClick.RemoveAllListeners();
            betButton100.onClick.AddListener(() => SetBetAmount(100f));
        }
        
        // Place bet button
        if (placeBetButton != null)
        {
            placeBetButton.onClick.RemoveAllListeners();
            placeBetButton.onClick.AddListener(PlaceBet);
        }
        
        // Input field
        if (betInputField != null)
        {
            betInputField.onValueChanged.RemoveAllListeners();
            betInputField.onValueChanged.AddListener(OnInputFieldChanged);
        }
        
        Log("[BettingManager] UI listeners set up");
    }
    
    /// <summary>
    /// Enable betting UI for a new round
    /// </summary>
    public void EnableBetting()
    {
        isBettingActive = true;
        
        // Reset bet amount
        currentBetAmount = 0f;
        UpdateCurrentBetDisplay();
        
        // Clear input field
        if (betInputField != null)
        {
            betInputField.text = "";
        }
        
        // Enable buttons
        SetQuickBetButtonsInteractable(true);
        
        if (placeBetButton != null)
        {
            placeBetButton.interactable = false; // Disabled until valid bet entered
        }
        
        if (betInputField != null)
        {
            betInputField.interactable = true;
        }
        
        UpdateBalanceDisplay();
        
        Log("[BettingManager] Betting enabled for new round");
    }
    
    /// <summary>
    /// Disable betting UI during gameplay
    /// </summary>
    public void DisableBetting()
    {
        isBettingActive = false;
        
        // Disable all betting UI
        SetQuickBetButtonsInteractable(false);
        
        if (placeBetButton != null)
        {
            placeBetButton.interactable = false;
        }
        
        if (betInputField != null)
        {
            betInputField.interactable = false;
        }
        
        Log("[BettingManager] Betting disabled");
    }
    
    /// <summary>
    /// Set bet amount from quick buttons
    /// </summary>
    public void SetBetAmount(float amount)
    {
        if (!isBettingActive)
        {
            LogWarning("[BettingManager] Cannot set bet: betting is not active");
            return;
        }
        
        // Validate bet
        if (ValidateBet(amount))
        {
            currentBetAmount = amount;
            
            // Update input field to reflect button choice
            if (betInputField != null)
            {
                betInputField.text = amount.ToString("F1");
            }
            
            UpdateCurrentBetDisplay();
            UpdatePlaceBetButtonState();
            
            Log($"[BettingManager] Bet amount set to {amount} via button");
        }
        else
        {
            LogWarning($"[BettingManager] Invalid bet amount: {amount}");
        }
    }
    
    /// <summary>
    /// Handle manual input field changes
    /// </summary>
    private void OnInputFieldChanged(string value)
    {
        if (!isBettingActive)
        {
            return;
        }
        
        // Try to parse input
        if (float.TryParse(value, out float betAmount))
        {
            currentBetAmount = betAmount;
            UpdateCurrentBetDisplay();
            UpdatePlaceBetButtonState();
            
            Log($"[BettingManager] Bet amount set to {betAmount} via input field");
        }
        else if (string.IsNullOrEmpty(value))
        {
            currentBetAmount = 0f;
            UpdateCurrentBetDisplay();
            UpdatePlaceBetButtonState();
        }
    }
    
    /// <summary>
    /// Place the bet and start the round
    /// </summary>
    public void PlaceBet()
    {
        if (!isBettingActive)
        {
            LogWarning("[BettingManager] Cannot place bet: betting is not active");
            return;
        }
        
        if (GameProgressionManager.Instance == null)
        {
            LogWarning("[BettingManager] GameProgressionManager.Instance is null!");
            return;
        }
        
        if (deckScript == null)
        {
            LogWarning("[BettingManager] Deck script is not assigned!");
            return;
        }
        
        // Validate bet one more time
        if (!ValidateBet(currentBetAmount))
        {
            LogWarning($"[BettingManager] Cannot place bet: Invalid amount {currentBetAmount}");
            return;
        }
        
        float currentHealth = GameProgressionManager.Instance.playerHealthPercentage;
        
        Log($"[BettingManager] Placing bet: {currentBetAmount} | Current health: {currentHealth:F1}");
        
        // Deduct bet from player health (bet amount is the same as percentage)
        GameProgressionManager.Instance.DamagePlayer(currentBetAmount);
        
        Log($"[BettingManager] Health after bet: {GameProgressionManager.Instance.playerHealthPercentage:F1}");
        
        // Disable betting UI
        DisableBetting();
        
        // Update balance display
        UpdateBalanceDisplay();
        
        // Notify Deck script to start the round with this bet amount
        deckScript.StartBettingRound(currentBetAmount);
        
        Log($"[BettingManager] Bet placed successfully: {currentBetAmount}");
    }
    
    /// <summary>
    /// Validate if a bet amount is acceptable
    /// </summary>
    private bool ValidateBet(float amount)
    {
        if (GameProgressionManager.Instance == null)
        {
            return false;
        }
        
        float currentHealth = GameProgressionManager.Instance.playerHealthPercentage;
        
        // Check minimum
        if (amount < minBet)
        {
            return false;
        }
        
        // Check maximum (can't bet more than current health)
        if (amount > currentHealth)
        {
            return false;
        }
        
        // Check against configured max bet
        if (amount > maxBet)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Get current balance (player health percentage)
    /// </summary>
    public float GetCurrentBalance()
    {
        if (GameProgressionManager.Instance != null)
        {
            return GameProgressionManager.Instance.playerHealthPercentage;
        }
        return 0f;
    }
    
    /// <summary>
    /// Update balance display (shows as simple number, not percentage)
    /// </summary>
    private void UpdateBalanceDisplay()
    {
        float balance = GetCurrentBalance();
        string balanceString = $"{Mathf.RoundToInt(balance)} Soul";
        
        if (balanceText != null)
        {
            balanceText.text = balanceString;
        }
        
        if (balanceTextLegacy != null)
        {
            balanceTextLegacy.text = balanceString;
        }
    }
    
    /// <summary>
    /// Update current bet display (shows as simple number, not percentage)
    /// </summary>
    private void UpdateCurrentBetDisplay()
    {
        string betString = currentBetAmount > 0 ? $"{currentBetAmount:F0}" : "0.00";
        
        if (currentBetText != null)
        {
            currentBetText.text = betString;
        }
        
        if (currentBetTextLegacy != null)
        {
            currentBetTextLegacy.text = betString;
        }
    }
    
    /// <summary>
    /// Update place bet button interactable state
    /// </summary>
    private void UpdatePlaceBetButtonState()
    {
        if (placeBetButton != null)
        {
            placeBetButton.interactable = ValidateBet(currentBetAmount);
        }
    }
    
    /// <summary>
    /// Set quick bet buttons interactable state
    /// </summary>
    private void SetQuickBetButtonsInteractable(bool interactable)
    {
        if (betButton5 != null) betButton5.interactable = interactable;
        if (betButton10 != null) betButton10.interactable = interactable;
        if (betButton25 != null) betButton25.interactable = interactable;
        if (betButton50 != null) betButton50.interactable = interactable;
        if (betButton100 != null) betButton100.interactable = interactable;
    }
    
    /// <summary>
    /// Get current bet amount
    /// </summary>
    public float GetCurrentBetAmount()
    {
        return currentBetAmount;
    }
    
    /// <summary>
    /// Set minimum bet
    /// </summary>
    public void SetMinBet(float min)
    {
        minBet = Mathf.Max(0f, min);
        Log($"[BettingManager] Min bet set to {minBet}");
    }
    
    /// <summary>
    /// Set maximum bet
    /// </summary>
    public void SetMaxBet(float max)
    {
        maxBet = Mathf.Max(minBet, max);
        Log($"[BettingManager] Max bet set to {maxBet}");
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

