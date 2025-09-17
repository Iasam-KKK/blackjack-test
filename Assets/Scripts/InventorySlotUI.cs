using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image slotBackground;
    public Image cardImage;
    public Image materialBackground;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI durabilityText;
    public GameObject emptySlotIndicator;
    
    [Header("Visual Settings")]
    public Color equipmentSlotColor = new Color(0.2f, 0.6f, 1f, 0.3f);
    public Color storageSlotColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    public Color occupiedSlotColor = new Color(1f, 1f, 1f, 0.8f);
    public Color hoverColor = new Color(1f, 1f, 0.5f, 0.5f);
    
    [Header("State")]
    public int slotIndex;
    public bool isEquipmentSlot;
    public InventorySlotData slotData;
    
    private InventoryPanelUI parentPanel;
    private Color originalBackgroundColor;
    private bool isHovered = false;
    
    private void Start()
    {
        originalBackgroundColor = slotBackground.color;
        UpdateSlotDisplay();
    }
    
    public void Initialize(int index, bool isEquipment, InventorySlotData data, InventoryPanelUI parent)
    {
        slotIndex = index;
        isEquipmentSlot = isEquipment;
        slotData = data;
        parentPanel = parent;
        
        // Set slot background color based on type
        originalBackgroundColor = isEquipmentSlot ? equipmentSlotColor : storageSlotColor;
        slotBackground.color = originalBackgroundColor;
        
        UpdateSlotDisplay();
    }
    
    public void UpdateSlotDisplay()
    {
        if (slotData == null) return;
        
        bool hasCard = slotData.isOccupied && slotData.storedCard != null;
        
        // Show/hide components based on whether slot has a card
        if (cardImage != null) cardImage.gameObject.SetActive(hasCard);
        if (materialBackground != null) materialBackground.gameObject.SetActive(hasCard);
        if (cardNameText != null) cardNameText.gameObject.SetActive(hasCard);
        if (durabilityText != null) durabilityText.gameObject.SetActive(hasCard);
        if (emptySlotIndicator != null) emptySlotIndicator.SetActive(!hasCard);
        
        if (hasCard)
        {
            var card = slotData.storedCard;
            
            // Update card image
            if (cardImage != null && card.cardImage != null)
            {
                cardImage.sprite = card.cardImage;
                cardImage.color = Color.white;
            }
            
            // Update material background
            if (materialBackground != null && card.assignedMaterial != null)
            {
                if (card.assignedMaterial.backgroundSprite != null)
                {
                    materialBackground.sprite = card.assignedMaterial.backgroundSprite;
                    materialBackground.color = Color.white;
                }
                else
                {
                    materialBackground.sprite = null;
                    materialBackground.color = card.GetMaterialColor();
                }
            }
            
            // Update card name
            if (cardNameText != null)
            {
                cardNameText.text = card.cardName;
            }
            
            // Update durability display
            if (durabilityText != null)
            {
                int remainingUses = card.GetRemainingUses();
                if (remainingUses == -1)
                {
                    durabilityText.text = "∞";
                    durabilityText.color = Color.cyan;
                }
                else if (remainingUses > 0)
                {
                    durabilityText.text = remainingUses.ToString();
                    durabilityText.color = remainingUses > 2 ? Color.green : 
                                          remainingUses > 1 ? Color.yellow : Color.red;
                }
                else
                {
                    durabilityText.text = "0";
                    durabilityText.color = Color.red;
                }
            }
            
            // Update slot background to show it's occupied
            slotBackground.color = occupiedSlotColor;
        }
        else
        {
            // Reset to original background color
            slotBackground.color = originalBackgroundColor;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentPanel == null) return;
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Left click - select/interact with slot
            parentPanel.OnSlotClicked(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right click - context menu or quick action
            parentPanel.OnSlotRightClicked(this);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        
        // Hover visual feedback
        transform.DOScale(1.05f, 0.1f).SetEase(Ease.OutQuad);
        
        // Show tooltip if slot has a card
        if (slotData != null && slotData.isOccupied && slotData.storedCard != null)
        {
            ShowTooltip();
        }
        
        // Highlight slot
        if (slotBackground != null)
        {
            slotBackground.DOColor(hoverColor, 0.1f);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        
        // Reset visual feedback
        transform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
        
        // Hide tooltip
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
        
        // Reset slot color
        if (slotBackground != null)
        {
            Color targetColor = slotData != null && slotData.isOccupied ? occupiedSlotColor : originalBackgroundColor;
            slotBackground.DOColor(targetColor, 0.1f);
        }
    }
    
    private void ShowTooltip()
    {
        if (TooltipManager.Instance == null || slotData?.storedCard == null) return;
        
        var card = slotData.storedCard;
        string tooltipText = $"<b>{card.cardName}</b>\n\n";
        tooltipText += $"<i>{card.description}</i>\n\n";
        tooltipText += $"Material: {card.GetMaterialDisplayName()}\n";
        
        int remainingUses = card.GetRemainingUses();
        if (remainingUses == -1)
        {
            tooltipText += "Uses: Unlimited";
        }
        else
        {
            tooltipText += $"Uses: {remainingUses}/{card.maxUses}";
        }
        
        if (isEquipmentSlot)
        {
            tooltipText += "\n<color=yellow>Equipped</color>";
        }
        
        TooltipManager.Instance.ShowTooltip(tooltipText, transform.position);
    }
    
    // Visual feedback for valid drop target
    public void ShowAsValidDropTarget(bool isValid)
    {
        if (slotBackground == null) return;
        
        Color targetColor = isValid ? Color.green : Color.red;
        targetColor.a = 0.5f;
        slotBackground.DOColor(targetColor, 0.2f);
    }
    
    // Reset visual state
    public void ResetVisualState()
    {
        if (slotBackground == null) return;
        
        Color targetColor = slotData != null && slotData.isOccupied ? occupiedSlotColor : originalBackgroundColor;
        slotBackground.DOColor(targetColor, 0.2f);
        transform.DOScale(1f, 0.2f);
    }
}
