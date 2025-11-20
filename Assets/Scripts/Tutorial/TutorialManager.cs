using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public class TutorialStep
{
    public string title;
    [TextArea(3, 6)]
    public string description;
    public GameObject highlightTarget; // UI element to highlight during this step
    public Vector2 panelOffset = Vector2.zero; // Offset for tutorial panel position
    public bool waitForUserInput = true; // Whether to wait for "Next" button or auto-advance
    public float autoAdvanceDelay = 3f; // Delay before auto-advance (if waitForUserInput is false)
    public TutorialTrigger triggerType = TutorialTrigger.Manual; // What triggers this step
    public string triggerData = ""; // Additional data for trigger (button name, etc.)
}

public enum TutorialTrigger
{
    Manual,          // Triggered by "Next" button
    ButtonClick,     // Triggered by clicking a specific button
    GameEvent,       // Triggered by specific game events
    AutoAdvance      // Auto-advance after delay
}

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Panel")]
    public GameObject tutorialPanel;
    public Text titleText;
    public Text descriptionText;
    public Button nextButton;
    public Button previousButton;
    public Button closeButton;
    
    [Header("Highlight System")]
    public TutorialHighlightOverlay highlightOverlay; // Overlay component with highlighting
    
    [Header("Tutorial Steps")]
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();
    
    [Header("Settings")]
    public bool enableTutorial = true;
    public bool showPreviousButton = true;
    public float highlightPadding = 20f;
    
    [Header("Testing (Editor Only)")]
    [Tooltip("Force show tutorial in editor even if already completed (ignores PlayerPrefs)")]
    public bool forceShowTutorialInEditor = false;
    
    // Animation constants
    private const float AnimationDuration = 0.5f;
    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
    private const string TUTORIAL_CURRENT_STEP_KEY = "TutorialCurrentStep";
    
    // State variables
    private int currentStepIndex = 0;
    private bool isTutorialActive = false;
    private bool isAnimating = false;
    private Vector3 originalPanelScale;
    private Vector3 originalPanelPosition;
    
    // References to game systems
    private Deck deckController;
    private GameHistoryManager historyManager;
    private GameMenuManager menuManager;
    
    private void Start()
    {
        // Get references to other game systems
        deckController = FindObjectOfType<Deck>();
        historyManager = FindObjectOfType<GameHistoryManager>();
        menuManager = FindObjectOfType<GameMenuManager>();
        
        // Set up UI elements
        SetupTutorialPanel();
        
        // Check if tutorial should be shown
        if (ShouldShowTutorial())
        {
            StartCoroutine(DelayedTutorialStart());
        }
    }
    
    private void SetupTutorialPanel()
    {
        if (tutorialPanel != null)
        {
            // Store original transform values
            originalPanelScale = tutorialPanel.transform.localScale;
            originalPanelPosition = tutorialPanel.transform.localPosition;
            
            // Hide panel initially
            tutorialPanel.SetActive(false);
        }
        
        // Set up button listeners
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStep);
        }
        
        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
            previousButton.onClick.AddListener(PreviousStep);
            // Hide previous button on first step, show on others
            previousButton.gameObject.SetActive(showPreviousButton && currentStepIndex > 0);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseTutorial);
        }
        
        // Set up highlight overlay
        if (highlightOverlay != null)
        {
            highlightOverlay.HideOverlay();
        }
    }
    
    private bool ShouldShowTutorial()
    {
        if (!enableTutorial) return false;
        
        // Force show tutorial in editor for testing (ignores PlayerPrefs)
        #if UNITY_EDITOR
        if (forceShowTutorialInEditor)
        {
            Debug.Log("Force showing tutorial in editor for testing");
            return true;
        }
        #endif
        
        // Check if tutorial is enabled for new players
        bool tutorialEnabledForNewPlayers = PlayerPrefs.GetInt("TutorialEnabledForNewPlayers", 1) == 1;
        if (!tutorialEnabledForNewPlayers)
        {
            Debug.Log("Tutorial disabled for new players in settings");
            return false;
        }
        
        // Check if tutorial was completed before
        bool tutorialCompleted = PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
        
        Debug.Log("Tutorial completed previously: " + tutorialCompleted + ", Tutorial enabled for new players: " + tutorialEnabledForNewPlayers);
        return !tutorialCompleted;
    }
    
    private IEnumerator DelayedTutorialStart()
    {
        // Wait a moment for the scene to fully load
        yield return new WaitForSeconds(1f);
        
        // Initialize tutorial steps with game-specific content
        InitializeTutorialSteps();
        
        // Start the tutorial
        StartTutorial();
    }
    
    private void InitializeTutorialSteps()
    {
        // Clear existing steps and create new ones based on current game state
        tutorialSteps.Clear();
        
        // Step 1: Welcome
        tutorialSteps.Add(new TutorialStep
        {
            title = "Welcome to Enhanced Blackjack!",
            description = "Welcome to this enhanced version of Blackjack! This isn't your ordinary card game - it features blind systems, tarot cards, special abilities, and much more. Let's learn everything step by step!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 2: Basic Blackjack Rules - Objective
        tutorialSteps.Add(new TutorialStep
        {
            title = "Blackjack Basics - The Goal",
            description = "The goal of Blackjack is simple: get your hand as close to 21 as possible without going over (called 'busting'), while having a higher total than the dealer.",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 3: Card Values
        tutorialSteps.Add(new TutorialStep
        {
            title = "Card Values",
            description = "• Number cards (2-10): Worth their face value\n• Face cards (Jack, Queen, King): Worth 10 points\n• Aces: Worth 1 OR 11 points (whichever is better for your hand)\n\nThe game automatically chooses the best value for Aces!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 4: How a Hand is Played
        tutorialSteps.Add(new TutorialStep
        {
            title = "How to Play a Hand",
            description = "1. Place your bet\n2. You and the dealer each get 2 cards\n3. One dealer card is face-down (hidden)\n4. Choose to 'Hit' (take another card) or 'Stand' (keep current total)\n5. Try to get close to 21 without going over!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 5: Dealer Rules
        tutorialSteps.Add(new TutorialStep
        {
            title = "Dealer Rules",
            description = "The dealer follows strict rules:\n• Must hit on 16 or less\n• Must stand on 17 or more\n• The face-down card is revealed after you finish your turn\n\nThis means the dealer has no choice - they must follow these rules!",
            highlightTarget = deckController?.dealer?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 6: Winning and Losing
        tutorialSteps.Add(new TutorialStep
        {
            title = "How to Win",
            description = "You WIN if:\n• Your hand is closer to 21 than dealer's\n• You get exactly 21 (Blackjack!)\n• Dealer busts (goes over 21)\n\nYou LOSE if:\n• You bust (go over 21)\n• Dealer's hand is closer to 21\n• Both have same total = Push (tie)",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 7: Your Balance
        tutorialSteps.Add(new TutorialStep
        {
            title = "Your Soul (Health)",
            description = "This shows your current soul/health. You start with 100 soul. Your health will increase when you win and decrease when you lose. Managing your health is crucial for survival!",
            highlightTarget = deckController?.bettingManager?.balanceText?.gameObject ?? deckController?.bettingManager?.balanceTextLegacy?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 8: Understanding Bets
        tutorialSteps.Add(new TutorialStep
        {
            title = "Understanding Bets",
            description = "Before each hand, you must bet your soul (health). This is the amount you're risking:\n• If you win, you heal back your bet PLUS additional health\n• If you lose, your bet is lost (health already deducted)\n• Bet wisely - don't risk more health than you can afford!",
            highlightTarget = deckController?.bettingManager?.currentBetText?.gameObject ?? deckController?.bettingManager?.currentBetTextLegacy?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 9: Placing Bets - Raise/Lower
        tutorialSteps.Add(new TutorialStep
        {
            title = "Adjusting Your Bet",
            description = "Use these buttons to set your bet amount:\n• Quick bet buttons: 5, 10, 25, 50, 100\n• Or enter a custom amount in the input field\n\nYou can't bet more than your current health. Choose your bet amount carefully!",
            highlightTarget = deckController?.bettingManager?.betButton10?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 10: Place Bet Button
        tutorialSteps.Add(new TutorialStep
        {
            title = "Confirming Your Bet",
            description = "After setting your bet amount, click 'Place Bet' to confirm and start the hand. Your health will be deducted immediately. Once you place a bet, the cards will be dealt and the hand begins. You can't change your bet during a hand!",
            highlightTarget = deckController?.bettingManager?.placeBetButton?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 11: Hit and Stand Buttons
        tutorialSteps.Add(new TutorialStep
        {
            title = "Hit and Stand Actions",
            description = "After cards are dealt:\n• 'HIT': Take another card (risk going over 21)\n• 'STAND': Keep your current total (end your turn)\n\nChoose wisely! Going over 21 means instant loss, but staying too low might lose to the dealer.",
            highlightTarget = deckController?.hitButton?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 12: Boss System Introduction
        tutorialSteps.Add(new TutorialStep
        {
            title = "Boss System - Overview",
            description = "This game features a boss-based progression system! Each boss has unique abilities and mechanics that affect gameplay. Defeat bosses to advance to the next level and unlock new challenges!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 13: Boss Health and Hands
        tutorialSteps.Add(new TutorialStep
        {
            title = "Boss Health and Hands",
            description = "Each boss has health points and a limited number of hands. Win hands to reduce the boss's health. When the boss's health reaches zero, you defeat them and advance to the next boss!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 14: Boss Mechanics
        tutorialSteps.Add(new TutorialStep
        {
            title = "Boss Mechanics",
            description = "Each boss has unique abilities that change how the game works. Some bosses might steal your cards, others might have special rules, and some might give you bonuses. Learn their patterns to succeed!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 15: Boss Progression
        tutorialSteps.Add(new TutorialStep
        {
            title = "Boss Progression",
            description = "Defeat bosses to unlock new ones! Each boss is more challenging than the last, with stronger abilities and higher stakes. Can you defeat them all and become the ultimate blackjack master?",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 16: Streak System
        tutorialSteps.Add(new TutorialStep
        {
            title = "Streak System - Bonus Multipliers",
            description = "Win consecutive hands to build a streak! Each streak level increases your winnings:\n• 1x streak: Normal winnings\n• 2x streak: 1.75x multiplier\n• 3x streak: 2.0x multiplier\n• And more!\n\nLosing breaks your streak, so be strategic!",
            highlightTarget = deckController?.streakPanel?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 17: Streak Flame Indicator
        tutorialSteps.Add(new TutorialStep
        {
            title = "Streak Flame Colors",
            description = "The flame color shows your current streak level:\n• Blue: 1x (no bonus)\n• Green: 2x streak\n• Yellow: 3x streak\n• Pink: 4x streak\n• Purple: 5x streak\n\nHigher streaks = bigger multipliers = more money!",
            highlightTarget = deckController?.streakPanel?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 18: Tarot Cards Introduction
        tutorialSteps.Add(new TutorialStep
        {
            title = "Tarot Cards - Special Powers",
            description = "This game features magical tarot cards that give you special abilities! These cards can help you win more hands and earn more money. You can buy them in the shop and use them strategically during gameplay.",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 19: Peek Ability
        tutorialSteps.Add(new TutorialStep
        {
            title = "Peek Ability - See Hidden Cards",
            description = "The Peek ability lets you temporarily see the dealer's face-down card! This gives you crucial information to make better decisions. Click the eye button when you have this ability available. Use it wisely - it's limited!",
            highlightTarget = deckController?.peekButton?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 20: Transform Ability
        tutorialSteps.Add(new TutorialStep
        {
            title = "Transform Ability - Change Cards",
            description = "Some tarot cards let you transform your cards into better ones! Select cards in your hand (they'll highlight) then click the transform button. This can turn a losing hand into a winning one!",
            highlightTarget = deckController?.transformButton?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 21: Discard Ability
        tutorialSteps.Add(new TutorialStep
        {
            title = "Discard Ability - Remove Unwanted Cards",
            description = "The discard ability lets you remove cards from your hand that you don't want. This is useful when you have too many cards or want to get rid of low-value cards. Select cards and click discard!",
            highlightTarget = deckController?.discardButton?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 22: Shop System Introduction
        tutorialSteps.Add(new TutorialStep
        {
            title = "The Shop - Buy Tarot Cards",
            description = "Between rounds, you can visit the shop to buy powerful tarot cards using your earnings. Different cards provide different abilities. Invest wisely in cards that match your playing style!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 23: Shop Navigation
        tutorialSteps.Add(new TutorialStep
        {
            title = "Shopping for Cards",
            description = "In the shop:\n• Browse available tarot cards\n• Read their descriptions and costs\n• Click to purchase cards you want\n• More powerful cards cost more money\n• Plan your purchases based on your strategy!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 24: Using Tarot Cards
        tutorialSteps.Add(new TutorialStep
        {
            title = "Using Your Tarot Cards",
            description = "Once you own tarot cards, they'll appear in your collection. During gameplay:\n• Cards activate automatically or via buttons\n• Some cards are one-time use, others are permanent\n• Read each card's effect carefully\n• Timing is everything!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 25: Discard Tokens
        tutorialSteps.Add(new TutorialStep
        {
            title = "Discard Tokens - Special Currency",
            description = "Some tarot cards give you 'discard tokens' when you win streaks. These tokens can be used to discard unwanted cards from your hand. They're valuable resources - use them strategically!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 26: Game History
        tutorialSteps.Add(new TutorialStep
        {
            title = "Game History - Track Your Progress",
            description = "Click this button to view your game history. See your past rounds, wins, losses, bets, and outcomes. This helps you analyze your performance and improve your strategy over time.",
            highlightTarget = historyManager?.showHistoryButton?.gameObject,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 27: Strategy Tips
        tutorialSteps.Add(new TutorialStep
        {
            title = "Strategy Tips",
            description = "Key strategies for success:\n• Start with small bets to learn\n• Build streaks for bonus multipliers\n• Use tarot abilities at the right moment\n• Manage your money across blind levels\n• Don't chase losses with big bets\n• Practice makes perfect!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 28: Advanced Tips
        tutorialSteps.Add(new TutorialStep
        {
            title = "Advanced Tips",
            description = "Pro tips:\n• Peek when dealer shows 10 or Ace\n• Transform when you have 12-16 (danger zone)\n• Buy defensive tarot cards early\n• Save powerful abilities for crucial moments\n• Watch your goal progress constantly\n• High streaks = higher bets for maximum profit!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        // Step 29: Ready to Play
        tutorialSteps.Add(new TutorialStep
        {
            title = "Ready to Become a Blackjack Master!",
            description = "You now know all the game mechanics! Remember:\n1. Start by placing a bet\n2. Play your hand strategically\n3. Use tarot abilities wisely\n4. Build streaks for bonuses\n5. Meet blind goals to advance\n\nGood luck and have fun!",
            highlightTarget = null,
            waitForUserInput = true,
            triggerType = TutorialTrigger.Manual
        });
        
        Debug.Log("Initialized " + tutorialSteps.Count + " comprehensive tutorial steps");
    }
    
    public void StartTutorial()
    {
        if (tutorialSteps.Count == 0)
        {
            Debug.LogWarning("No tutorial steps defined!");
            return;
        }
        
        isTutorialActive = true;
        currentStepIndex = 0;
        
        Debug.Log("Starting tutorial with " + tutorialSteps.Count + " steps");
        
        // Pause the game during tutorial
        if (deckController != null)
        {
            // Disable game interactions during tutorial
            DisableGameInteractions();
        }
        
        ShowStep(currentStepIndex);
    }
    
    private void ShowStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= tutorialSteps.Count)
        {
            Debug.LogError("Invalid tutorial step index: " + stepIndex);
            return;
        }
        
        TutorialStep step = tutorialSteps[stepIndex];
        
        // Update UI
        if (titleText != null)
            titleText.text = step.title;
        
        if (descriptionText != null)
            descriptionText.text = step.description;
        
        // Update button visibility and text
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(step.waitForUserInput);
            
            // Change button text for last step
            Text buttonText = nextButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = (stepIndex == tutorialSteps.Count - 1) ? "Finish" : "Next";
            }
        }
        
        // Update previous button visibility (hide on first step)
        if (previousButton != null)
        {
            previousButton.gameObject.SetActive(showPreviousButton && stepIndex > 0);
        }
        
        // Show tutorial panel with animation
        ShowTutorialPanel();
        
        // Handle highlighting
        if (step.highlightTarget != null)
        {
            HighlightElement(step.highlightTarget);
        }
        else
        {
            HideHighlight();
        }
        
        // Handle auto-advance
        if (!step.waitForUserInput)
        {
            StartCoroutine(AutoAdvanceStep(step.autoAdvanceDelay));
        }
        
        Debug.Log("Showing tutorial step " + (stepIndex + 1) + "/" + tutorialSteps.Count + ": " + step.title);
    }
    
    private void ShowTutorialPanel()
    {
        if (tutorialPanel == null || isAnimating) return;
        
        isAnimating = true;
        
        // Set initial state
        tutorialPanel.transform.localScale = Vector3.zero;
        tutorialPanel.SetActive(true);
        
        // Animate in
        tutorialPanel.transform.DOScale(originalPanelScale, AnimationDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => isAnimating = false);
    }
    
    private void HideTutorialPanel()
    {
        if (tutorialPanel == null || isAnimating) return;
        
        isAnimating = true;
        
        // Animate out
        tutorialPanel.transform.DOScale(Vector3.zero, AnimationDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                tutorialPanel.SetActive(false);
                isAnimating = false;
            });
    }
    
    private void HighlightElement(GameObject target)
    {
        if (highlightOverlay == null || target == null) return;
        
        // Show overlay and set highlight target
        highlightOverlay.ShowOverlay();
        highlightOverlay.SetHighlightTarget(target);
    }
    
    private void HideHighlight()
    {
        if (highlightOverlay != null)
        {
            highlightOverlay.HideOverlay();
        }
    }
    
    private IEnumerator AutoAdvanceStep(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (isTutorialActive)
        {
            NextStep();
        }
    }
    
    public void NextStep()
    {
        if (!isTutorialActive || isAnimating) return;
        
        currentStepIndex++;
        
        if (currentStepIndex >= tutorialSteps.Count)
        {
            // Tutorial completed
            CompleteTutorial();
        }
        else
        {
            ShowStep(currentStepIndex);
        }
    }
    
    public void PreviousStep()
    {
        if (!isTutorialActive || isAnimating || currentStepIndex <= 0) return;
        
        currentStepIndex--;
        ShowStep(currentStepIndex);
        
        Debug.Log("Going back to tutorial step " + (currentStepIndex + 1) + "/" + tutorialSteps.Count);
    }
    
    public void CloseTutorial()
    {
        if (!isTutorialActive) return;
        
        Debug.Log("Tutorial closed by user");
        CompleteTutorial();
    }
    
    private void CompleteTutorial()
    {
        isTutorialActive = false;
        
        // Hide UI elements
        HideTutorialPanel();
        HideHighlight();
        
        // Mark tutorial as completed
        PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
        PlayerPrefs.Save();
        
        // Re-enable game interactions
        EnableGameInteractions();
        
        Debug.Log("Tutorial completed!");
    }
    
    private void DisableGameInteractions()
    {
        // This prevents players from interacting with game elements during tutorial
        if (deckController != null)
        {
            // We don't disable the buttons here as the tutorial will guide through them
            // Instead, we could add a tutorial overlay that blocks interactions
        }
    }
    
    private void EnableGameInteractions()
    {
        // Re-enable all game interactions after tutorial
        if (deckController != null)
        {
            // Game interactions should work normally now
        }
    }
    
    // Public methods for external triggers
    public void TriggerTutorialStep(string triggerData)
    {
        if (!isTutorialActive) return;
        
        TutorialStep currentStep = tutorialSteps[currentStepIndex];
        if (currentStep.triggerType == TutorialTrigger.ButtonClick && 
            currentStep.triggerData == triggerData)
        {
            NextStep();
        }
    }
    
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_COMPLETED_KEY);
        PlayerPrefs.DeleteKey(TUTORIAL_CURRENT_STEP_KEY);
        PlayerPrefs.Save();
        Debug.Log("Tutorial progress reset");
    }
    
    // Public property to check if tutorial is active
    public bool IsTutorialActive
    {
        get { return isTutorialActive; }
    }
    
    // Method to restart tutorial (for settings menu)
    public void RestartTutorial()
    {
        ResetTutorial();
        
        if (tutorialSteps.Count == 0)
        {
            InitializeTutorialSteps();
        }
        
        StartTutorial();
    }
} 