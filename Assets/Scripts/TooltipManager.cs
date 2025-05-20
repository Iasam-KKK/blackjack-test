using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }
    
    [Header("Tooltip Components")]
    public GameObject tooltipPanel;
    public Text titleText;
    public Text descriptionText;
    
    [Header("Settings")]
    public float offset = 20f;
    public float fadeTime = 0.1f;
    
    // For smooth fade in/out
    private CanvasGroup canvasGroup;
    
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
        
        // Hide tooltip initially
        HideTooltip();
        
        // Initialize descriptions
        tooltipDescriptions.Add("TheEye", peekDescription);
        tooltipDescriptions.Add("Transform", transformDescription);
        tooltipDescriptions.Add("Discard", discardDescription);
    }
    
    public void ShowTooltip(string title, string description, Vector3 position)
    {
        // Check for null references
        if (tooltipPanel == null || titleText == null || descriptionText == null)
        {
            Debug.LogWarning("TooltipManager has missing references! Check Inspector assignments.");
            return;
        }

        // Set text
        titleText.text = title;
        descriptionText.text = description;
        
        // Position tooltip
        PositionTooltip(position);
        
        // Show tooltip
        tooltipPanel.SetActive(true);
        
        // Fade in
        canvasGroup.alpha = 0f;
        DOTween.Kill(tooltipPanel);
        canvasGroup.DOFade(1f, fadeTime);
    }
    
    // Overload for backward compatibility
    public void ShowTooltip(string title, Vector3 position)
    {
        ShowTooltip(title, "", position);
    }
    
    public void HideTooltip()
    {
        // Fade out and hide
        DOTween.Kill(tooltipPanel);
        canvasGroup.DOFade(0f, fadeTime).OnComplete(() => {
            tooltipPanel.SetActive(false);
        });
    }
    
    private void PositionTooltip(Vector3 targetPosition)
    {
        // Get canvas for screen space positioning
        Canvas canvas = GetComponentInParent<Canvas>();
        
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // For screen space overlay
            Vector2 screenPoint = targetPosition;
            
            // Get tooltip size
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            
            // Position tooltip to avoid going off-screen
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            // Adjust X position to keep tooltip on screen
            if (screenPoint.x + tooltipSize.x + offset > screenWidth)
            {
                screenPoint.x = screenPoint.x - tooltipSize.x - offset;
            }
            else
            {
                screenPoint.x += offset;
            }
            
            // Adjust Y position to keep tooltip on screen
            if (screenPoint.y + tooltipSize.y + offset > screenHeight)
            {
                screenPoint.y = screenPoint.y - tooltipSize.y - offset;
            }
            else
            {
                screenPoint.y += offset;
            }
            
            tooltipPanel.transform.position = screenPoint;
        }
        else
        {
            // For world space or camera space canvas
            tooltipPanel.transform.position = targetPosition + new Vector3(offset, offset, 0);
        }
    }
} 