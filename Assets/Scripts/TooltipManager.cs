using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }
    
    [Header("Tooltip UI")]
    public GameObject tooltipPanel;
    public Text tooltipText;
    
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
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Hide tooltip panel initially
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
        
        // Initialize descriptions
        tooltipDescriptions.Add("TheEye", peekDescription);
        tooltipDescriptions.Add("Transform", transformDescription);
        tooltipDescriptions.Add("Discard", discardDescription);
    }
    
    public void ShowTooltip(string cardName, Vector3 position)
    {
        if (tooltipPanel == null || tooltipText == null)
        {
            Debug.LogWarning("Tooltip UI references are missing!");
            return;
        }
        
        if (tooltipDescriptions.TryGetValue(cardName, out string description))
        {
            tooltipText.text = description;
            tooltipPanel.SetActive(true);
            
            // Position tooltip near the card but ensure it stays on screen
            RectTransform rectTransform = tooltipPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Convert world position to screen position
                Vector3 screenPos = Camera.main.WorldToScreenPoint(position);
                
                // Adjust position to ensure visibility
                screenPos.y += 50; // Offset up a bit
                
                // Constrain to screen edges
                float width = rectTransform.rect.width;
                float height = rectTransform.rect.height;
                
                screenPos.x = Mathf.Clamp(screenPos.x, width/2, Screen.width - width/2);
                screenPos.y = Mathf.Clamp(screenPos.y, height/2, Screen.height - height/2);
                
                rectTransform.position = screenPos;
            }
        }
        else
        {
            Debug.LogWarning("No tooltip description found for " + cardName);
        }
    }
    
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
} 