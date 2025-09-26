using UnityEngine;

public class DeckMaterialManager : MonoBehaviour
{
    public static DeckMaterialManager Instance;

    [Header("Available Frames (assign in Inspector)")]
    public Sprite[] materialFrames; // drag all your frame sprites here

    private int selectedIndex = 0;
    private const string PREF_KEY = "DeckMaterialIndex";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        selectedIndex = PlayerPrefs.GetInt(PREF_KEY, 0);
        if (selectedIndex < 0 || selectedIndex >= materialFrames.Length)
            selectedIndex = 0;
    }

    public void EquipMaterial(int index)
    {
        if (index < 0 || index >= materialFrames.Length) return;

        selectedIndex = index;
        PlayerPrefs.SetInt(PREF_KEY, selectedIndex);
        PlayerPrefs.Save();

        Debug.Log($"DeckMaterialManager: Equipped material index {index}");
    }

    public Sprite GetCurrentFrame()
    {
        if (selectedIndex < 0 || selectedIndex >= materialFrames.Length) return null;
        return materialFrames[selectedIndex];
    }
}