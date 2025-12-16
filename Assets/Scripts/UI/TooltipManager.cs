using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }
    
    [Header("Tooltip Components")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    
    [Header("Settings")]
    public float offset = 20f;
    public float fadeTime = 0.1f;
    
    // For smooth fade in/out
    private CanvasGroup canvasGroup;
    private Coroutine hideDelayCoroutine;
    
    [Header("Tooltip Descriptions")]
    [TextArea(3, 5)]
    public string peekDescription = "Eye of Providence: Peek at the dealer's hidden card for 2 seconds. Uses 1 token. Can only be used once per round.";
    [TextArea(3, 5)]
    public string transformDescription = "Transformation: Replace the first selected card with a duplicate of the second selected card. Uses 1 token. Can only be used once per round.";
    [TextArea(3, 5)]
    public string discardDescription = "Discard: Remove the selected card from your hand. Uses 1 token. Can be used multiple times if you have tokens.";
    
    private Dictionary<string, string> tooltipDescriptions = new Dictionary<string, string>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Get or add canvas group for fading
        canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        }
        
        // Prevent tooltip from blocking raycasts to avoid flickering
        canvasGroup.blocksRaycasts = false;
        
        // Hide tooltip initially
        HideTooltip();
        
        // Initialize descriptions
        tooltipDescriptions.Add("TheEye", peekDescription);
        tooltipDescriptions.Add("Transform", transformDescription);
        tooltipDescriptions.Add("Discard", discardDescription);
    }
    
    public void ShowTooltip(string title, string description, Vector3 position, bool isTarotCard = true)
    {
        // Only show tooltips for tarot cards, not shop items
        if (!isTarotCard)
        {
            return;
        }
        
        // Check for null references
        if (tooltipPanel == null || titleText == null || descriptionText == null)
        {
            Debug.LogWarning("TooltipManager has missing references! Check Inspector assignments.");
            return;
        }

        // Cancel any pending hide operation
        if (hideDelayCoroutine != null)
        {
            StopCoroutine(hideDelayCoroutine);
            hideDelayCoroutine = null;
        }

        // Set text
        titleText.text = title;
        descriptionText.text = description;
        
        // Show tooltip (position is manually set in Unity editor)
        tooltipPanel.SetActive(true);
        
        // Fade in
        canvasGroup.alpha = 0f;
        DOTween.Kill(tooltipPanel);
        canvasGroup.DOFade(1f, fadeTime);
    }
    
    // Overload for backward compatibility
    public void ShowTooltip(string title, Vector3 position)
    {
        ShowTooltip(title, "", position, true);
    }
    
    public void HideTooltip()
    {
        // Add a small delay to prevent flickering when mouse moves slightly
        if (hideDelayCoroutine != null)
        {
            StopCoroutine(hideDelayCoroutine);
        }
        hideDelayCoroutine = StartCoroutine(HideTooltipDelayed());
    }
    
    private IEnumerator HideTooltipDelayed()
    {
        // Small delay to prevent flickering
        yield return new WaitForSeconds(0.1f);
        
        // Fade out and hide
        DOTween.Kill(tooltipPanel);
        canvasGroup.DOFade(0f, fadeTime).OnComplete(() => {
            tooltipPanel.SetActive(false);
        });
        
        hideDelayCoroutine = null;
    }
    

} 