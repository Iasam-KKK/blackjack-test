using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUIV3 : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Components")]
    public Image slotBackgroundImage;
    public Image cardImage;
    public Image materialBackgroundImage;
    public TextMeshProUGUI cardNameTxt;
    public TextMeshProUGUI durabilityTxt;
    public GameObject emptySlotIndicator;
    public GameObject selectionHighlight;
    
    [Header("Visual Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color hoverColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    public float selectedScale = 1.1f; // Scale when selected
    public bool keepSelectionVisible = true; // Keep selection persistent
    
    [Header("Slot Data")]
    public int slotIndex;
    public InventorySlotData slotData;
    public bool isSelected;
    
    private InventoryPanelUIV3 parentPanel;
    private bool isHovered;
    
    public void Initialize(int index, InventorySlotData data, InventoryPanelUIV3 parent)
    {
        slotIndex = index;
        slotData = data;
        parentPanel = parent;
        isSelected = false;
        
        UpdateDisplay();
    }
    
    public void UpdateDisplay()
    {
        if (slotData == null)
        {
            ShowEmpty();
            return;
        }
        
        bool hasCard = slotData.isOccupied && slotData.storedCard != null;
        
        if (hasCard)
        {
            ShowCard(slotData.storedCard);
        }
        else
        {
            ShowEmpty();
        }
        
        // Update selection highlight
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isSelected);
        }
    }
    
    private void ShowCard(TarotCardData card)
    {
        // Show card components
        if (cardImage != null)
        {
            cardImage.gameObject.SetActive(true);
            if (card.cardImage != null)
            {
                cardImage.sprite = card.cardImage;
                cardImage.color = Color.white;
            }
            else
            {
                cardImage.sprite = null;
                cardImage.color = Color.gray;
            }
        }
        
        if (materialBackgroundImage != null)
        {
            materialBackgroundImage.gameObject.SetActive(true);
            if (card.assignedMaterial != null)
            {
                if (card.assignedMaterial.backgroundSprite != null)
                {
                    materialBackgroundImage.sprite = card.assignedMaterial.backgroundSprite;
                    materialBackgroundImage.color = Color.white;
                }
                else
                {
                    materialBackgroundImage.sprite = null;
                    materialBackgroundImage.color = card.GetMaterialColor();
                }
            }
            else
            {
                materialBackgroundImage.sprite = null;
                materialBackgroundImage.color = Color.white;
            }
        }
        
        if (cardNameTxt != null)
        {
            cardNameTxt.gameObject.SetActive(true);
            cardNameTxt.text = card.cardName;
        }
        
        if (durabilityTxt != null)
        {
            durabilityTxt.gameObject.SetActive(true);
            int remainingUses = card.GetRemainingUses();
            if (remainingUses == -1)
            {
                durabilityTxt.text = "∞";
                durabilityTxt.color = Color.cyan;
            }
            else if (remainingUses > 0)
            {
                durabilityTxt.text = remainingUses.ToString();
                durabilityTxt.color = remainingUses > 2 ? Color.green : 
                                      remainingUses > 1 ? Color.yellow : Color.red;
            }
            else
            {
                durabilityTxt.text = "0";
                durabilityTxt.color = Color.red;
            }
        }
        
        if (emptySlotIndicator != null)
        {
            emptySlotIndicator.SetActive(false);
        }
    }
    
    private void ShowEmpty()
    {
        // Hide card components
        if (cardImage != null)
        {
            cardImage.gameObject.SetActive(false);
        }
        
        if (materialBackgroundImage != null)
        {
            materialBackgroundImage.gameObject.SetActive(false);
        }
        
        if (cardNameTxt != null)
        {
            cardNameTxt.gameObject.SetActive(false);
        }
        
        if (durabilityTxt != null)
        {
            durabilityTxt.gameObject.SetActive(false);
        }
        
        if (emptySlotIndicator != null)
        {
            emptySlotIndicator.SetActive(true);
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(selected);
        }
        
        if (slotBackgroundImage != null && !isHovered)
        {
            slotBackgroundImage.color = selected ? selectedColor : normalColor;
        }
        
        // Apply scale for better visibility
        if (keepSelectionVisible)
        {
            if (selected)
            {
                transform.localScale = Vector3.one * selectedScale;
            }
            else if (!isHovered)
            {
                transform.localScale = Vector3.one;
            }
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentPanel == null) return;
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            parentPanel.OnSlotSelected(this);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        
        // Only change color on hover if not selected
        if (slotBackgroundImage != null && !isSelected)
        {
            slotBackgroundImage.color = hoverColor;
        }
        
        // Slight scale increase on hover (but not as much as selection)
        if (!isSelected && keepSelectionVisible)
        {
            transform.localScale = Vector3.one * 1.05f;
        }
        
        // Show tooltip if card exists
        if (slotData != null && slotData.isOccupied && slotData.storedCard != null)
        {
            ShowTooltip();
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        
        // Reset color if not selected
        if (slotBackgroundImage != null && !isSelected)
        {
            slotBackgroundImage.color = normalColor;
        }
        
        // Reset scale - keep selected scale if selected
        if (keepSelectionVisible)
        {
            if (isSelected)
            {
                transform.localScale = Vector3.one * selectedScale;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }
        
        // Hide tooltip
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
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
        
        if (slotData.isEquipmentSlot)
        {
            tooltipText += "\n<color=yellow>Equipped</color>";
        }
        
        TooltipManager.Instance.ShowTooltip(tooltipText, transform.position);
    }
}

