using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MaterialSlotUIV3 : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Components")]
    public Image materialImage; // The material background image
    public Image materialBackgroundImage; // Optional second layer
    public TextMeshProUGUI materialNameTxt;
    public TextMeshProUGUI usesTxt; // Shows max uses (1, 2, 3, etc. or ∞)
    public GameObject selectionHighlight;
    
    [Header("Visual Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color hoverColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    
    [Header("Material Data")]
    public MaterialData materialData;
    public bool isSelected;
    
    private InventoryPanelUIV3 parentPanel;
    private bool isHovered;
    
    public void Initialize(MaterialData data, InventoryPanelUIV3 parent)
    {
        materialData = data;
        parentPanel = parent;
        isSelected = false;
        
        UpdateDisplay();
    }
    
    public void UpdateDisplay()
    {
        if (materialData == null)
        {
            Debug.LogWarning("MaterialSlotUIV3: materialData is null!");
            return;
        }
        
        // Set material image
        if (materialImage != null)
        {
            if (materialData.backgroundSprite != null)
            {
                materialImage.sprite = materialData.backgroundSprite;
                materialImage.color = Color.white;
            }
            else
            {
                materialImage.sprite = null;
                materialImage.color = materialData.GetMaterialColor();
            }
        }
        
        // Set material name
        if (materialNameTxt != null)
        {
            materialNameTxt.text = materialData.materialName;
        }
        
        // Set uses
        if (usesTxt != null)
        {
            if (materialData.maxUses == -1)
            {
                usesTxt.text = "∞";
            }
            else
            {
                usesTxt.text = materialData.maxUses.ToString();
            }
        }
        
        // Update selection highlight
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isSelected);
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isSelected);
        }
        
        Debug.Log($"[MaterialSlotUIV3] {materialData.materialName} selected: {isSelected}");
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentPanel != null)
        {
            parentPanel.OnMaterialSlotSelected(this);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        // Optional: add hover effect
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        // Optional: remove hover effect
    }
}

