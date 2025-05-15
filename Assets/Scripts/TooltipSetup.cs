using UnityEngine;
using UnityEngine.UI;

// This is a helper script to create a tooltip UI if one doesn't exist
// You can run this once in the editor to create the tooltip UI
public class TooltipSetup : MonoBehaviour
{
    public static GameObject CreateTooltipUI()
    {
        // Check if canvas exists, create one if not
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create tooltip panel
        GameObject tooltipPanel = new GameObject("TooltipPanel");
        tooltipPanel.transform.SetParent(canvas.transform, false);
        
        // Add panel components
        RectTransform rectTransform = tooltipPanel.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 150);
        
        Image panelImage = tooltipPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        // Create tooltip text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipPanel.transform, false);
        
        RectTransform textRectTransform = textObj.AddComponent<RectTransform>();
        textRectTransform.anchorMin = new Vector2(0, 0);
        textRectTransform.anchorMax = new Vector2(1, 1);
        textRectTransform.offsetMin = new Vector2(10, 10);
        textRectTransform.offsetMax = new Vector2(-10, -10);
        
        Text tooltipText = textObj.AddComponent<Text>();
        tooltipText.alignment = TextAnchor.MiddleCenter;
        tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        tooltipText.fontSize = 16;
        tooltipText.color = Color.white;
        tooltipText.supportRichText = true;
        
        // Add TooltipManager to canvas
        TooltipManager tooltipManager = canvas.gameObject.AddComponent<TooltipManager>();
        tooltipManager.tooltipPanel = tooltipPanel;
        tooltipManager.tooltipText = tooltipText;
        
        // Hide initially
        tooltipPanel.SetActive(false);
        
        Debug.Log("Tooltip UI created successfully!");
        return tooltipPanel;
    }
    
    [ContextMenu("Create Tooltip UI")]
    public void SetupTooltip()
    {
        CreateTooltipUI();
    }
} 