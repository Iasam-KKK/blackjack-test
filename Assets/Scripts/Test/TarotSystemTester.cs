using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TarotSystemTester : MonoBehaviour
{
    [Header("Test Controls")]
    public KeyCode testPurchaseKey = KeyCode.T;
    public KeyCode testEquipKey = KeyCode.E;
    public KeyCode testUnequipKey = KeyCode.U;
    public KeyCode refreshTarotPanelKey = KeyCode.R;
    
    [Header("Debug Info")]
    public bool showDebugInfo = true;
    
    private void Update()
    {
        // Test purchase simulation
        if (Input.GetKeyDown(testPurchaseKey))
        {
            SimulatePurchase();
        }
        
        // Test equip from storage
        if (Input.GetKeyDown(testEquipKey))
        {
            TestEquipFromStorage();
        }
        
        // Test unequip
        if (Input.GetKeyDown(testUnequipKey))
        {
            TestUnequip();
        }
        
        // Refresh tarot panel
        if (Input.GetKeyDown(refreshTarotPanelKey))
        {
            RefreshTarotPanel();
        }
    }
    
    private void SimulatePurchase()
    {
        Debug.Log("=== SIMULATING CARD PURCHASE ===");
        
        // Find a card in the shop
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null && shopManager.shopPanel != null)
        {
            TarotCard[] shopCards = shopManager.shopPanel.GetComponentsInChildren<TarotCard>();
            if (shopCards.Length > 0)
            {
                TarotCard firstCard = shopCards[0];
                Debug.Log($"Attempting to purchase: {firstCard.cardData.cardName}");
                
                // Simulate click on the card
                firstCard.TryPurchaseCard();
            }
            else
            {
                Debug.LogWarning("No cards found in shop!");
            }
        }
    }
    
    private void TestEquipFromStorage()
    {
        Debug.Log("=== TESTING EQUIP FROM STORAGE ===");
        
        if (InventoryManager.Instance != null && InventoryManager.Instance.inventoryData != null)
        {
            var storageSlots = InventoryManager.Instance.inventoryData.storageSlots;
            
            // Find first occupied storage slot
            for (int i = 0; i < storageSlots.Count; i++)
            {
                if (storageSlots[i].isOccupied)
                {
                    Debug.Log($"Equipping card from storage slot {i}: {storageSlots[i].storedCard.cardName}");
                    bool equipped = InventoryManager.Instance.EquipCardFromStorage(i);
                    Debug.Log($"Equip result: {equipped}");
                    break;
                }
            }
        }
    }
    
    private void TestUnequip()
    {
        Debug.Log("=== TESTING UNEQUIP ===");
        
        if (InventoryManager.Instance != null && InventoryManager.Instance.inventoryData != null)
        {
            var equipmentSlots = InventoryManager.Instance.inventoryData.equipmentSlots;
            
            // Find first occupied equipment slot
            for (int i = 0; i < equipmentSlots.Count; i++)
            {
                if (equipmentSlots[i].isOccupied)
                {
                    Debug.Log($"Unequipping card from equipment slot {i}: {equipmentSlots[i].storedCard.cardName}");
                    bool unequipped = InventoryManager.Instance.UnequipCard(i);
                    Debug.Log($"Unequip result: {unequipped}");
                    break;
                }
            }
        }
    }
    
    private void RefreshTarotPanel()
    {
        Debug.Log("=== REFRESHING TAROT PANEL ===");
        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RefreshAllTarotSlots();
            Debug.Log("Tarot panel refreshed!");
        }
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Label("=== TAROT SYSTEM TESTER ===", GUI.skin.box);
        GUILayout.Label($"Press {testPurchaseKey} to simulate purchase");
        GUILayout.Label($"Press {testEquipKey} to equip from storage");
        GUILayout.Label($"Press {testUnequipKey} to unequip");
        GUILayout.Label($"Press {refreshTarotPanelKey} to refresh tarot panel");
        
        GUILayout.Space(10);
        
        // Show inventory status
        if (InventoryManager.Instance != null && InventoryManager.Instance.inventoryData != null)
        {
            GUILayout.Label("=== INVENTORY STATUS ===", GUI.skin.box);
            
            var stats = InventoryManager.Instance.GetInventoryStats();
            GUILayout.Label($"Storage: {stats.storageUsed}/{stats.storageTotal}");
            GUILayout.Label($"Equipment: {stats.equipmentUsed}/{stats.equipmentTotal}");
            
            // Show equipped cards
            GUILayout.Label("Equipped Cards:");
            var equipmentSlots = InventoryManager.Instance.inventoryData.equipmentSlots;
            for (int i = 0; i < equipmentSlots.Count; i++)
            {
                if (equipmentSlots[i].isOccupied)
                {
                    GUILayout.Label($"  Slot {i}: {equipmentSlots[i].storedCard.cardName}");
                }
            }
        }
        
        GUILayout.EndArea();
    }
}
