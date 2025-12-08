using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Deck deckController;
    
    [Header("Boss System UI")]
    public NewBossPanel newBossPanel;
    public Button nextHandButton; // This should reference the Play Again button in the inspector
    
    [Header("Panel Toggle Buttons")]
    public Button shopButton;
    public Button inventoryButton;
    
    [Header("Panel References")]
    public ShopManagerV2 shopManager;
    public InventoryPanelUIV3 inventoryPanel;
    
    private void Start()
    {
        if (deckController == null)
        {
            deckController = FindObjectOfType<Deck>();
            if (deckController == null)
            {
                Debug.LogError("No Deck controller found in scene!");
                return;
            }
        }
        
        // Set up boss UI reference in Deck script
        if (newBossPanel != null)
            deckController.newBossPanel = newBossPanel;
        else
            Debug.LogWarning("New Boss Panel not assigned in UI Manager!");
            
        // Make sure the Play Again button is properly named
        if (nextHandButton != null)
        {
            Text buttonText = nextHandButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Next Hand";
            }
            
            // Connect Play Again button if not already connected
            if (deckController.playAgainButton == null)
            {
                deckController.playAgainButton = nextHandButton;
            }
        }
        
        // Auto-find panel managers if not assigned
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManagerV2>();
        }
        
        if (inventoryPanel == null)
        {
            inventoryPanel = FindObjectOfType<InventoryPanelUIV3>();
        }
        
        // Setup shop button listener
        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners();
            shopButton.onClick.AddListener(OnShopButtonClicked);
        }
        else
        {
            Debug.LogWarning("[UIManager] Shop button not assigned!");
        }
        
        // Setup inventory button listener
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
        }
        else
        {
            Debug.LogWarning("[UIManager] Inventory button not assigned!");
        }
    }
    
    /// <summary>
    /// Called when the shop button is clicked
    /// Toggles shop panel and closes inventory if open
    /// </summary>
    private void OnShopButtonClicked()
    {
        if (shopManager == null)
        {
            Debug.LogError("[UIManager] ShopManager not found!");
            return;
        }
        
        // If shop is already open, close it (toggle behavior)
        if (shopManager.IsShopOpen())
        {
            shopManager.CloseShop();
            return;
        }
        
        // Close inventory if it's open
        if (inventoryPanel != null && inventoryPanel.IsInventoryOpen)
        {
            inventoryPanel.HideInventory();
        }
        
        // Open shop
        shopManager.OpenShop();
    }
    
    /// <summary>
    /// Called when the inventory button is clicked
    /// Toggles inventory panel and closes shop if open
    /// </summary>
    private void OnInventoryButtonClicked()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[UIManager] InventoryPanel not found!");
            return;
        }
        
        // If inventory is already open, close it (toggle behavior)
        if (inventoryPanel.IsInventoryOpen)
        {
            inventoryPanel.HideInventory();
            return;
        }
        
        // Close shop if it's open
        if (shopManager != null && shopManager.IsShopOpen())
        {
            shopManager.CloseShop();
        }
        
        // Open inventory
        inventoryPanel.ShowInventory();
    }
    
    private void OnDestroy()
    {
        // Clean up button listeners
        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners();
        }
        
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
        }
    }
} 