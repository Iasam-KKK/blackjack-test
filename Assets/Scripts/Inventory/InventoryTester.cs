using UnityEngine;

/// <summary>
/// Simple script to test inventory functionality and persistence
/// </summary>
public class InventoryTester : MonoBehaviour
{
    [Header("Testing Controls")]
    [Tooltip("Press these keys during play mode to test functionality")]
    public KeyCode addCardKey = KeyCode.T;
    public KeyCode clearInventoryKey = KeyCode.C;
    public KeyCode saveInventoryKey = KeyCode.S;
    public KeyCode loadInventoryKey = KeyCode.L;
    public KeyCode syncTarotPanelKey = KeyCode.Y;
    
    private void Update()
    {
        if (InventoryManager.Instance == null) return;
        
        if (Input.GetKeyDown(addCardKey))
        {
            AddTestCard();
        }
        
        if (Input.GetKeyDown(clearInventoryKey))
        {
            ClearInventory();
        }
        
        if (Input.GetKeyDown(saveInventoryKey))
        {
            Debug.Log("Saving inventory manually...");
            // The save happens automatically, but this logs it
        }
        
        if (Input.GetKeyDown(loadInventoryKey))
        {
            TestLoadInventory();
        }
        
        if (Input.GetKeyDown(syncTarotPanelKey))
        {
            InventoryManager.Instance.SyncEquippedCardsToTarotPanel();
        }
    }
    
    private void AddTestCard()
    {
        // Create test card with random properties
        TarotCardData testCard = ScriptableObject.CreateInstance<TarotCardData>();
        testCard.cardName = "Test Card " + Random.Range(1, 1000);
        testCard.description = "Auto-generated test card";
        testCard.cardType = (TarotCardType)Random.Range(0, System.Enum.GetValues(typeof(TarotCardType)).Length);
        testCard.price = 100;
        
        // Create random material
        MaterialData material = ScriptableObject.CreateInstance<MaterialData>();
        material.materialName = "Test Material";
        material.maxUses = Random.Range(1, 6);
        material.materialType = (TarotMaterialType)Random.Range(0, System.Enum.GetValues(typeof(TarotMaterialType)).Length);
        
        testCard.AssignMaterial(material);
        
        bool success = InventoryManager.Instance.AddPurchasedCard(testCard);
        Debug.Log(success ? $"Added test card: {testCard.cardName}" : "Failed to add card - inventory full");
    }
    
    private void ClearInventory()
    {
        if (InventoryManager.Instance.inventoryData == null) return;
        
        // Clear all slots
        foreach (var slot in InventoryManager.Instance.inventoryData.storageSlots)
        {
            slot.RemoveCard();
        }
        foreach (var slot in InventoryManager.Instance.inventoryData.equipmentSlots)
        {
            slot.RemoveCard();
        }
        
        // Clear saved data
        InventoryManager.Instance.ClearSavedInventory();
        
        Debug.Log("Cleared all inventory data");
    }
    
    private void TestLoadInventory()
    {
        Debug.Log("Testing inventory persistence...");
        var stats = InventoryManager.Instance.GetInventoryStats();
        Debug.Log($"Current inventory: {stats.storageUsed}/{stats.storageTotal} storage, {stats.equipmentUsed}/{stats.equipmentTotal} equipped");
        
        // Check PlayerPrefs
        string savedData = PlayerPrefs.GetString("InventoryData_v1", "");
        if (string.IsNullOrEmpty(savedData))
        {
            Debug.LogWarning("No inventory data found in PlayerPrefs!");
        }
        else
        {
            Debug.Log($"PlayerPrefs inventory data: {savedData.Substring(0, Mathf.Min(100, savedData.Length))}...");
        }
    }
    
    private void OnGUI()
    {
        if (InventoryManager.Instance == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("INVENTORY TESTER", GUI.skin.box);
        GUILayout.Label($"Press '{addCardKey}' to add test card");
        GUILayout.Label($"Press '{clearInventoryKey}' to clear inventory");
        GUILayout.Label($"Press '{syncTarotPanelKey}' to sync tarot panel");
        GUILayout.Label($"Press 'I' to toggle inventory UI");
        
        var stats = InventoryManager.Instance.GetInventoryStats();
        GUILayout.Label($"Storage: {stats.storageUsed}/{stats.storageTotal}");
        GUILayout.Label($"Equipment: {stats.equipmentUsed}/{stats.equipmentTotal}");
        GUILayout.Label($"Usable Cards: {stats.usableCards}");
        
        GUILayout.EndArea();
    }
}
