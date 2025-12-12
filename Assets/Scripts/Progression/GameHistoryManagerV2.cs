using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameHistoryManagerV2 : MonoBehaviour
{
    public static GameHistoryManagerV2 Instance { get; private set; }
    
    [Header("History Panel")]
    public GameObject historyPanel;
    public Button showHistoryButton;    // Toggle button - shows/hides history
    public Button closeHistoryButton;   // Dedicated close button inside panel
    public Button clearHistoryButton;
    
    [Header("History Slot Prefab")]
    public GameObject historySlotPrefab;
    public Transform historyContainer;
    
    private List<GameHistoryEntryV2> gameHistory = new List<GameHistoryEntryV2>();
    private List<GameObject> historySlotObjects = new List<GameObject>();
    
    // Animation constants
    private const float AnimationDuration = 0.4f;
    private const float ScaleMultiplier = 1.1f; // How much bigger the panel gets during animation
    
    // Animation state
    private bool isAnimating = false;
    private bool isHistoryOpen = false;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    
    private void Awake()
    {
        // Singleton pattern - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameHistoryManagerV2] Instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log($"[GameHistoryManagerV2] Duplicate instance detected, transferring UI references before destroying {gameObject.name}");
            
            // CRITICAL: Transfer UI references from this (new scene's) manager to the singleton
            // This ensures the singleton always has fresh, valid UI references for the current scene
            TransferReferencesToSingleton();
            
            Destroy(gameObject);
            return;
        }
        
        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    /// <summary>
    /// Transfer UI references from this instance to the singleton instance
    /// Called when a duplicate is detected, before this instance is destroyed
    /// </summary>
    private void TransferReferencesToSingleton()
    {
        if (Instance == null) return;
        
        // Transfer UI references if this instance has them
        if (historyPanel != null)
        {
            Instance.historyPanel = historyPanel;
            Debug.Log("[GameHistoryManagerV2] Transferred historyPanel to singleton");
        }
        if (showHistoryButton != null)
        {
            Instance.showHistoryButton = showHistoryButton;
            Debug.Log("[GameHistoryManagerV2] Transferred showHistoryButton to singleton");
        }
        if (closeHistoryButton != null)
        {
            Instance.closeHistoryButton = closeHistoryButton;
            Debug.Log("[GameHistoryManagerV2] Transferred closeHistoryButton to singleton");
        }
        if (clearHistoryButton != null)
        {
            Instance.clearHistoryButton = clearHistoryButton;
            Debug.Log("[GameHistoryManagerV2] Transferred clearHistoryButton to singleton");
        }
        if (historySlotPrefab != null)
        {
            Instance.historySlotPrefab = historySlotPrefab;
            Debug.Log("[GameHistoryManagerV2] Transferred historySlotPrefab to singleton");
        }
        if (historyContainer != null)
        {
            Instance.historyContainer = historyContainer;
            Debug.Log("[GameHistoryManagerV2] Transferred historyContainer to singleton");
        }
        
        // Re-setup button listeners with new references
        Instance.SetupButtonListeners();
        
        // Store transform values if panel exists
        if (Instance.historyPanel != null)
        {
            Instance.originalScale = Instance.historyPanel.transform.localScale;
            Instance.originalPosition = Instance.historyPanel.transform.localPosition;
        }
        
        Debug.Log("[GameHistoryManagerV2] UI references transferred to singleton");
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameHistoryManagerV2] Scene loaded: {scene.name}");
        
        // Find UI references in the new scene if they're not set
        if (scene.name == "Blackjack")
        {
            FindUIReferencesInScene();
        }
    }
    
    /// <summary>
    /// Find UI references in the current scene if they're missing
    /// </summary>
    private void FindUIReferencesInScene()
    {
        // Try to find history panel if not set
        if (historyPanel == null)
        {
            GameObject found = GameObject.Find("GameHistoryWindow");
            if (found != null)
            {
                historyPanel = found;
                Debug.Log("[GameHistoryManagerV2] Found historyPanel in scene");
            }
        }
        
        // Try to find container if not set
        if (historyContainer == null)
        {
            Transform found = historyPanel?.transform.Find("gameHistory");
            if (found != null)
            {
                historyContainer = found;
                Debug.Log("[GameHistoryManagerV2] Found historyContainer in scene");
            }
        }
        
        // Store transform values if panel exists
        if (historyPanel != null)
        {
            originalScale = historyPanel.transform.localScale;
            originalPosition = historyPanel.transform.localPosition;
        }
    }
    
    private void SetupButtonListeners()
    {
        // Remove old listeners first
        if (showHistoryButton != null)
        {
            showHistoryButton.onClick.RemoveAllListeners();
            showHistoryButton.onClick.AddListener(ToggleHistory);
        }
        
        if (closeHistoryButton != null)
        {
            closeHistoryButton.onClick.RemoveAllListeners();
            closeHistoryButton.onClick.AddListener(CloseHistory);
        }
        
        if (clearHistoryButton != null)
        {
            clearHistoryButton.onClick.RemoveAllListeners();
            clearHistoryButton.onClick.AddListener(ClearHistory);
        }
    }
    
    private void Start()
    {
        // Set up button listeners
        SetupButtonListeners();
        
        // Find UI references if not set
        FindUIReferencesInScene();
        
        // Hide history panel initially and store original transform values
        if (historyPanel != null)
        {
            // Store original scale and position for animations
            originalScale = historyPanel.transform.localScale;
            originalPosition = historyPanel.transform.localPosition;
            
            historyPanel.SetActive(false);
            isHistoryOpen = false;
        }
        
        Debug.Log($"[GameHistoryManagerV2] Start complete. History count: {gameHistory.Count}");
    }
    
    public void AddHistoryEntry(GameHistoryEntryV2 entry)
    {
        gameHistory.Add(entry);
        Debug.Log($"Added history entry V2: Boss={entry.GetBossName()}, Outcome: {entry.outcome}, Amount: {entry.winLossAmount:F0} SOL");
        
        // If panel is currently open, refresh the display
        if (historyPanel != null && historyPanel.activeInHierarchy)
        {
            RefreshHistoryDisplay();
        }
    }
    
    /// <summary>
    /// Toggle history panel - called when show history button is clicked
    /// </summary>
    public void ToggleHistory()
    {
        if (isHistoryOpen)
        {
            CloseHistory();
        }
        else
        {
            ShowHistory();
        }
    }
    
    /// <summary>
    /// Show history panel
    /// </summary>
    public void ShowHistory()
    {
        if (historyPanel != null && !isAnimating && !isHistoryOpen)
        {
            isAnimating = true;
            isHistoryOpen = true;
            
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
    
    /// <summary>
    /// Close history panel - called by both toggle button and close button
    /// </summary>
    public void CloseHistory()
    {
        if (historyPanel != null && !isAnimating && isHistoryOpen)
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
                isHistoryOpen = false;
                
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
        Debug.Log("Game history V2 cleared");
    }
    
    private void RefreshHistoryDisplay()
    {
        // Clear existing UI entries
        ClearHistorySlotObjects();
        
        // If we have a container and prefab, create slots dynamically
        if (historyContainer != null && historySlotPrefab != null)
        {
            CreateHistorySlots();
        }
        else
        {
            Debug.LogWarning("GameHistoryManagerV2: historyContainer or historySlotPrefab is not assigned!");
        }
    }
    
    private void CreateHistorySlots()
    {
        Debug.Log($"Creating history slots. Count: {gameHistory.Count}");
        Debug.Log($"History Container: {(historyContainer != null ? historyContainer.name : "NULL")}");
        Debug.Log($"History Slot Prefab: {(historySlotPrefab != null ? historySlotPrefab.name : "NULL")}");
        
        // Display newest first (reverse order)
        for (int i = gameHistory.Count - 1; i >= 0; i--)
        {
            GameHistoryEntryV2 entry = gameHistory[i];
            GameObject slotObject = Instantiate(historySlotPrefab, historyContainer);
            historySlotObjects.Add(slotObject);
            Debug.Log($"Instantiated history slot object: {slotObject.name}");
            
            // Get UI component from the instantiated prefab
            GameHistorySlotUI slotUI = slotObject.GetComponent<GameHistorySlotUI>();
            if (slotUI != null)
            {
                Debug.Log("Found GameHistorySlotUI component, setting up entry");
                slotUI.SetupEntry(entry);
            }
            else
            {
                Debug.LogWarning($"No GameHistorySlotUI component found on {slotObject.name}. Make sure the prefab has this component attached.");
            }
        }
    }
    
    private void ClearHistorySlotObjects()
    {
        foreach (GameObject obj in historySlotObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        historySlotObjects.Clear();
    }
    
    public int GetHistoryCount()
    {
        return gameHistory.Count;
    }
    
    public List<GameHistoryEntryV2> GetHistory()
    {
        return new List<GameHistoryEntryV2>(gameHistory);
    }
}

