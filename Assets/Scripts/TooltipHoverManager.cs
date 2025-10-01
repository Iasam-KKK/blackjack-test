using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TooltipHoverManager : MonoBehaviour
{
    [Header("Tooltip UI References")]
    public Text tipText; // Reference to your existing tooltip text component
    public RectTransform tipWindow; // Reference to your existing tooltip panel
    
    [Header("Settings")]
    public float offset = 20f;
    public float fadeTime = 0.1f;
    
    // Static events for other scripts to use
    public static Action<string, Vector2> OnMouseHover;
    public static Action OnMouseLoseFocus;
    
    private CanvasGroup canvasGroup;
    
    private void OnEnable()
    {
        OnMouseHover += ShowTip;
        OnMouseLoseFocus += HideTip;
    }
    
    private void OnDisable()
    {
        OnMouseHover -= ShowTip;
        OnMouseLoseFocus -= HideTip;
    }
    
    void Start()
    {
        // Get or add canvas group for fading
        canvasGroup = tipWindow.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = tipWindow.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Hide tooltip initially
        HideTip();
    }
    
    private void ShowTip(string tip, Vector2 mousePos)
    {
        if (tipText == null || tipWindow == null) return;
        
        // Set the tooltip text
        tipText.text = tip;
        
        // Position tooltip above the mouse cursor
        tipWindow.position = new Vector2(mousePos.x, mousePos.y + offset);
        
        // Show tooltip
        tipWindow.gameObject.SetActive(true);
        
        // Fade in
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeTime);
    }
    
    private void HideTip()
    {
        if (tipWindow == null) return;
        
        // Fade out and hide
        canvasGroup.DOFade(0f, fadeTime).OnComplete(() => {
            tipWindow.gameObject.SetActive(false);
        });
    }
}
