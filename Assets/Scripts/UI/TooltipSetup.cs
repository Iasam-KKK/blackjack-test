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
        
        // Create tooltip title
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(tooltipPanel.transform, false);
        
        RectTransform titleRectTransform = titleObj.AddComponent<RectTransform>();
        titleRectTransform.anchorMin = new Vector2(0, 0.75f);
        titleRectTransform.anchorMax = new Vector2(1, 1);
        titleRectTransform.offsetMin = new Vector2(10, 5);
        titleRectTransform.offsetMax = new Vector2(-10, -5);
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.supportRichText = true;
        
        // Create tooltip description
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(tooltipPanel.transform, false);
        
        RectTransform descRectTransform = descObj.AddComponent<RectTransform>();
        descRectTransform.anchorMin = new Vector2(0, 0);
        descRectTransform.anchorMax = new Vector2(1, 0.75f);
        descRectTransform.offsetMin = new Vector2(10, 10);
        descRectTransform.offsetMax = new Vector2(-10, -5);
        
        Text descriptionText = descObj.AddComponent<Text>();
        descriptionText.alignment = TextAnchor.UpperLeft;
        descriptionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        descriptionText.fontSize = 16;
        descriptionText.color = Color.white;
        descriptionText.supportRichText = true;
        
        // Add TooltipManager to canvas
        TooltipManager tooltipManager = canvas.gameObject.AddComponent<TooltipManager>();
        tooltipManager.tooltipPanel = tooltipPanel;
        tooltipManager.titleText = titleText;
        tooltipManager.descriptionText = descriptionText;
        
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