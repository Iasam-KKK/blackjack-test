using UnityEngine;
using UnityEngine.EventSystems;

public class TarotCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("The unique name for this tarot card (TheEye, Transform, or Discard)")]
    public string cardName;
    
    // Called when the mouse enters the card area
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show tooltip with card description
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowTooltip(cardName, transform.position);
        }
        
        // Optional: Add visual feedback (scaling, highlighting, etc.)
        transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
    }
    
    // Called when the mouse exits the card area
    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide tooltip
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
        
        // Reset visual feedback
        transform.localScale = Vector3.one;
    }
} 