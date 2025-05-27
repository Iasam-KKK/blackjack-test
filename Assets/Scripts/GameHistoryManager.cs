using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameHistoryManager : MonoBehaviour
{
    [Header("History Panel")]
    public GameObject historyPanel;
    public Button showHistoryButton;
    public Button closeHistoryButton;
    public Button clearHistoryButton;
    
    [Header("History Entry Prefab")]
    public GameObject historyEntryPrefab;
    public Transform historyContainer;
    
    [Header("History Entry UI Elements")]
    public Text roundText;
    public Text blindLevelText;
    public Text playerScoreText;
    public Text dealerScoreText;
    public Text betText;
    public Text balanceText;
    public Text outcomeText;
    
    private List<GameHistoryEntry> gameHistory = new List<GameHistoryEntry>();
    private List<GameObject> historyEntryObjects = new List<GameObject>();
    
    // Animation constants
    private const float AnimationDuration = 0.4f;
    private const float ScaleMultiplier = 1.1f; // How much bigger the panel gets during animation
    
    // Animation state
    private bool isAnimating = false;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    
    private void Start()
    {
        // Set up button listeners
        if (showHistoryButton != null)
        {
            showHistoryButton.onClick.AddListener(ShowHistory);
        }
        
        if (closeHistoryButton != null)
        {
            closeHistoryButton.onClick.AddListener(CloseHistory);
        }
        
        if (clearHistoryButton != null)
        {
            clearHistoryButton.onClick.AddListener(ClearHistory);
        }
        
        // Hide history panel initially and store original transform values
        if (historyPanel != null)
        {
            // Store original scale and position for animations
            originalScale = historyPanel.transform.localScale;
            originalPosition = historyPanel.transform.localPosition;
            
            historyPanel.SetActive(false);
        }
    }
    
    public void AddHistoryEntry(GameHistoryEntry entry)
    {
        gameHistory.Add(entry);
        Debug.Log("Added history entry: Round " + entry.roundNumber + ", Outcome: " + entry.outcome);
        
        // If panel is currently open, refresh the display
        if (historyPanel != null && historyPanel.activeInHierarchy)
        {
            RefreshHistoryDisplay();
        }
    }
    
    public void ShowHistory()
    {
        if (historyPanel != null && !isAnimating)
        {
            isAnimating = true;
            
            // Set initial state for animation (off-screen to the left, small scale)
            historyPanel.transform.localPosition = new Vector3(originalPosition.x - Screen.width, originalPosition.y, originalPosition.z);
            historyPanel.transform.localScale = Vector3.zero;
            
            // Activate the panel
            historyPanel.SetActive(true);
            RefreshHistoryDisplay();
            
            // Create animation sequence - mirror the closing animation but in reverse
            Sequence showSequence = DOTween.Sequence();
            
            // Animate position from left side to center and scale up simultaneously
            showSequence.Append(historyPanel.transform.DOLocalMove(originalPosition, AnimationDuration)
                .SetEase(Ease.OutBack, 1.2f));
            showSequence.Join(historyPanel.transform.DOScale(originalScale * ScaleMultiplier, AnimationDuration)
                .SetEase(Ease.OutBack, 1.1f));
            
            // Then settle to normal scale
            showSequence.Append(historyPanel.transform.DOScale(originalScale, AnimationDuration * 0.3f)
                .SetEase(Ease.OutQuart));
            
            // Mark animation as complete
            showSequence.OnComplete(() => {
                isAnimating = false;
            });
        }
    }
    
    public void CloseHistory()
    {
        if (historyPanel != null && !isAnimating)
        {
            isAnimating = true;
            
            // Create animation sequence
            Sequence closeSequence = DOTween.Sequence();
            
            // First scale down slightly
            closeSequence.Append(historyPanel.transform.DOScale(originalScale * 0.9f, AnimationDuration * 0.3f)
                .SetEase(Ease.InQuart));
            
            // Then animate position to the right side and scale to zero
            closeSequence.Append(historyPanel.transform.DOLocalMove(
                new Vector3(originalPosition.x + Screen.width, originalPosition.y, originalPosition.z), 
                AnimationDuration * 0.7f)
                .SetEase(Ease.InBack, 1.2f));
            closeSequence.Join(historyPanel.transform.DOScale(Vector3.zero, AnimationDuration * 0.7f)
                .SetEase(Ease.InBack, 1.1f));
            
            // Deactivate panel and reset transform when animation completes
            closeSequence.OnComplete(() => {
                historyPanel.SetActive(false);
                
                // Reset transform for next time
                historyPanel.transform.localPosition = originalPosition;
                historyPanel.transform.localScale = originalScale;
                
                isAnimating = false;
            });
        }
    }
    
    public void ClearHistory()
    {
        gameHistory.Clear();
        RefreshHistoryDisplay();
        Debug.Log("Game history cleared");
    }
    
    private void RefreshHistoryDisplay()
    {
        // Clear existing UI entries
        ClearHistoryEntryObjects();
        
        // If we have a container and prefab, create entries dynamically
        if (historyContainer != null && historyEntryPrefab != null)
        {
            CreateDynamicHistoryEntries();
        }
        // Otherwise, use the static UI elements for the latest entry
        else
        {
            UpdateStaticHistoryDisplay();
        }
    }
    
    private void CreateDynamicHistoryEntries()
    {
        Debug.Log("Creating dynamic history entries. Count: " + gameHistory.Count);
        Debug.Log("History Container: " + (historyContainer != null ? historyContainer.name : "NULL"));
        Debug.Log("History Entry Prefab: " + (historyEntryPrefab != null ? historyEntryPrefab.name : "NULL"));
        
        foreach (GameHistoryEntry entry in gameHistory)
        {
            GameObject entryObject = Instantiate(historyEntryPrefab, historyContainer);
            historyEntryObjects.Add(entryObject);
            Debug.Log("Instantiated history entry object: " + entryObject.name);
            
            // Get UI components from the instantiated prefab
            GameHistoryEntryUI entryUI = entryObject.GetComponent<GameHistoryEntryUI>();
            if (entryUI != null)
            {
                Debug.Log("Found GameHistoryEntryUI component, setting up entry");
                entryUI.SetupEntry(entry);
            }
            else
            {
                Debug.Log("No GameHistoryEntryUI component found, using direct setup");
                // Fallback: try to find components directly
                SetupEntryDirect(entryObject, entry);
            }
        }
    }
    
    private void SetupEntryDirect(GameObject entryObject, GameHistoryEntry entry)
    {
        // Find UI components in the entry object and set their text
        Text[] texts = entryObject.GetComponentsInChildren<Text>();
        
        foreach (Text text in texts)
        {
            switch (text.name)
            {
                case "RoundText":
                    text.text = entry.GetRoundText();
                    break;
                case "BlindLevelText":
                    text.text = entry.GetBlindText();
                    break;
                case "PlayerScoreText":
                    text.text = entry.GetPlayerScoreText();
                    break;
                case "DealerScoreText":
                    text.text = entry.GetDealerScoreText();
                    break;
                case "BetText":
                    text.text = entry.GetBetText();
                    break;
                case "BalanceText":
                    text.text = entry.GetBalanceChangeText();
                    break;
                case "OutcomeText":
                    text.text = entry.outcome;
                    break;
            }
        }
    }
    
    private void UpdateStaticHistoryDisplay()
    {
        if (gameHistory.Count == 0)
        {
            // Clear all text if no history
            if (roundText != null) roundText.text = "";
            if (blindLevelText != null) blindLevelText.text = "";
            if (playerScoreText != null) playerScoreText.text = "";
            if (dealerScoreText != null) dealerScoreText.text = "";
            if (betText != null) betText.text = "";
            if (balanceText != null) balanceText.text = "";
            if (outcomeText != null) outcomeText.text = "";
            return;
        }
        
        // Show the latest entry
        GameHistoryEntry latestEntry = gameHistory[gameHistory.Count - 1];
        
        if (roundText != null) roundText.text = latestEntry.GetRoundText();
        if (blindLevelText != null) blindLevelText.text = latestEntry.GetBlindText();
        if (playerScoreText != null) playerScoreText.text = latestEntry.GetPlayerScoreText();
        if (dealerScoreText != null) dealerScoreText.text = latestEntry.GetDealerScoreText();
        if (betText != null) betText.text = latestEntry.GetBetText();
        if (balanceText != null) balanceText.text = latestEntry.GetBalanceChangeText();
        if (outcomeText != null) outcomeText.text = latestEntry.outcome;
    }
    
    private void ClearHistoryEntryObjects()
    {
        foreach (GameObject obj in historyEntryObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        historyEntryObjects.Clear();
    }
    
    public int GetHistoryCount()
    {
        return gameHistory.Count;
    }
    
    public List<GameHistoryEntry> GetHistory()
    {
        return new List<GameHistoryEntry>(gameHistory);
    }
} 