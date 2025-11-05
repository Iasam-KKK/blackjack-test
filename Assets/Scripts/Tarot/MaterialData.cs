using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterial", menuName = "BlackJack/Material Data", order = 2)]
public class MaterialData : ScriptableObject
{
    [Header("Material Properties")]
    public string materialName;
    public int maxUses = 1; // -1 for unlimited (Diamond)
    public float rarityPercentage = 30f;
    
    [Header("Visual Properties")]
    [Tooltip("Assign your material background image here - this will be displayed behind the card image")]
    public Sprite backgroundSprite; // Material background image (preferred over color tint)
    [Tooltip("Fallback color tint used only if no background sprite is assigned")]
    public Color materialTint = Color.white; // Fallback tint color if no sprite is assigned
    
    [Header("Material Type")]
    public TarotMaterialType materialType = TarotMaterialType.Paper;
    
    // Get remaining uses for this material (-1 = unlimited)
    public bool HasUnlimitedUses()
    {
        return maxUses == -1;
    }
    
    // Get display name for the material
    public string GetDisplayName()
    {
        return materialName;
    }
    
    // Get material color for visual distinction
    public Color GetMaterialColor()
    {
        switch (materialType)
        {
            case TarotMaterialType.Paper: return new Color(0.95f, 0.95f, 0.90f); // Off-white
            case TarotMaterialType.Cardboard: return new Color(0.85f, 0.75f, 0.65f); // Brown
            case TarotMaterialType.Wood: return new Color(0.65f, 0.45f, 0.30f); // Dark brown
            case TarotMaterialType.Copper: return new Color(0.85f, 0.55f, 0.35f); // Copper
            case TarotMaterialType.Silver: return new Color(0.85f, 0.85f, 0.90f); // Silver
            case TarotMaterialType.Gold: return new Color(0.95f, 0.85f, 0.35f); // Gold
            case TarotMaterialType.Platinum: return new Color(0.90f, 0.90f, 0.95f); // Platinum
            case TarotMaterialType.Diamond: return new Color(0.95f, 0.95f, 1.0f); // Diamond white
            default: return materialTint;
        }
    }
}

// Define the different materials for tarot cards
public enum TarotMaterialType
{
    Paper,      // 1 use, 30% rarity
    Cardboard,  // 2 uses, 19% rarity  
    Wood,       // 3 uses, 17% rarity
    Copper,     // 4 uses, 15% rarity
    Silver,     // 5 uses, 10% rarity
    Gold,       // 6 uses, 5% rarity
    Platinum,   // 7 uses, 3% rarity
    Diamond     // Unlimited uses, 1% rarity
}

// Material manager for handling material selection and rarity
public static class MaterialManager
{
    // Array of all available materials (to be populated from Resources or assigned in inspector)
    private static MaterialData[] allMaterials;
    
    // Initialize materials from Resources folder
    public static void InitializeMaterials()
    {
        if (allMaterials == null)
        {
            allMaterials = Resources.LoadAll<MaterialData>("Materials");
            if (allMaterials.Length == 0)
            {
                Debug.LogWarning("No MaterialData assets found in Resources/Materials folder!");
            }
        }
    }
    
    // Get a random material based on rarity percentages
    public static MaterialData GetRandomMaterial()
    {
        InitializeMaterials();
        
        if (allMaterials == null || allMaterials.Length == 0)
        {
            Debug.LogError("No materials available! Creating default paper material.");
            return CreateDefaultMaterial();
        }
        
        float randomValue = Random.Range(0f, 100f);
        float cumulativePercentage = 0f;
        
        // Sort materials by rarity (rarest first for proper cumulative calculation)
        System.Array.Sort(allMaterials, (a, b) => a.rarityPercentage.CompareTo(b.rarityPercentage));
        
        foreach (MaterialData material in allMaterials)
        {
            cumulativePercentage += material.rarityPercentage;
            
            if (randomValue <= cumulativePercentage)
            {
                return material;
            }
        }
        
        // Fallback to first material if something goes wrong
        return allMaterials[0];
    }
    
    // Create a default material if none are found
    private static MaterialData CreateDefaultMaterial()
    {
        MaterialData defaultMaterial = ScriptableObject.CreateInstance<MaterialData>();
        defaultMaterial.materialName = "Paper";
        defaultMaterial.maxUses = 1;
        defaultMaterial.rarityPercentage = 100f;
        defaultMaterial.materialType = TarotMaterialType.Paper;
        return defaultMaterial;
    }
    
    // Get all available materials
    public static MaterialData[] GetAllMaterials()
    {
        InitializeMaterials();
        return allMaterials;
    }
}
