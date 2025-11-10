using UnityEngine;
using System.Collections;

public class TarotPanelDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private bool autoRefresh = false;
    [SerializeField] private float refreshInterval = 1f;
    
    private float nextRefreshTime;
    
    void Update()
    {
        // F5 key to manually refresh tarot panel
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("[TarotPanelDebugger] Manual refresh triggered (F5)");
            RefreshTarotPanel();
        }
        
        // F6 key to reinitialize tarot panel
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log("[TarotPanelDebugger] Reinitialize triggered (F6)");
            ReinitializeTarotPanel();
        }
        
        // F7 key to debug current state
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.Log("[TarotPanelDebugger] Debug state triggered (F7)");
            DebugCurrentState();
        }
        
        // Auto refresh if enabled
        if (autoRefresh && Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + refreshInterval;
            RefreshTarotPanel();
        }
    }
    
    private void RefreshTarotPanel()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ForceSyncWithTarotPanel();
            Debug.Log("[TarotPanelDebugger] Tarot panel refreshed");
        }
        else
        {
            Debug.LogWarning("[TarotPanelDebugger] InventoryManager.Instance is null");
        }
    }
    
    private void ReinitializeTarotPanel()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ForceReinitializeTarotPanel();
            Debug.Log("[TarotPanelDebugger] Tarot panel reinitialized");
        }
        else
        {
            Debug.LogWarning("[TarotPanelDebugger] InventoryManager.Instance is null");
        }
    }
    
    private void DebugCurrentState()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.DebugTarotPanelState();
            
            // Also debug equipment slots
            Debug.Log("=== EQUIPMENT SLOTS ===");
            var inventoryData = InventoryManager.Instance.inventoryData;
            if (inventoryData != null)
            {
                for (int i = 0; i < inventoryData.equipmentSlots.Count; i++)
                {
                    var slot = inventoryData.equipmentSlots[i];
                    Debug.Log($"Equipment Slot {i}: {(slot.isOccupied ? slot.storedCard.cardName : "empty")}");
                }
            }
            
            // Debug ShopManager state
            var shopManager = FindObjectOfType<ShopManager>();
            if (shopManager != null)
            {
                Debug.Log("=== SHOP MANAGER ===");
                Debug.Log($"Tarot Panel: {(shopManager.tarotPanel != null ? "exists" : "null")}");
                Debug.Log($"Tarot Slots Count: {shopManager.tarotSlots.Count}");
                
                for (int i = 0; i < shopManager.tarotSlots.Count; i++)
                {
                    var slot = shopManager.tarotSlots[i];
                    Debug.Log($"Tarot Slot {i}: {(slot != null ? $"exists, children: {slot.childCount}" : "null")}");
                    
                    if (slot != null)
                    {
                        var cards = slot.GetComponentsInChildren<TarotCard>(true);
                        foreach (var card in cards)
                        {
                            Debug.Log($"  - Card: {(card.cardData != null ? card.cardData.cardName : "no data")}, Active: {card.gameObject.activeSelf}");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[TarotPanelDebugger] InventoryManager.Instance is null");
        }
    }
}
