using UnityEngine;
using System;

public class DeckMaterialManager : MonoBehaviour
{
    public static DeckMaterialManager Instance;

    [Header("Available Frames (assign in Inspector)")]
    public Sprite[] materialFrames; // drag all your frame sprites here

    private int selectedIndex = 0;
    private const string PREF_KEY = "DeckMaterialIndex";

    // Event fired whenever the equipped frame changes
    public event Action OnFrameChanged;

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

        // Load saved selection safely
        selectedIndex = PlayerPrefs.GetInt(PREF_KEY, 0);
        if (materialFrames == null || materialFrames.Length == 0) selectedIndex = 0;
        if (selectedIndex < 0 || (materialFrames != null && selectedIndex >= materialFrames.Length)) selectedIndex = 0;

        // Notify listeners about current selection so existing cards update on scene start
        OnFrameChanged?.Invoke();
    }

    // Equip and persist a material by index (0..N-1)
    public void EquipMaterial(int index)
    {
        if (materialFrames == null || materialFrames.Length == 0) 
        {
            Debug.LogWarning("DeckMaterialManager: No materialFrames assigned in Inspector.");
            return;
        }
        if (index < 0 || index >= materialFrames.Length) return;

        selectedIndex = index;
        PlayerPrefs.SetInt(PREF_KEY, selectedIndex);
        PlayerPrefs.Save();

        Debug.Log($"DeckMaterialManager: Equipped material index {index} -> {materialFrames[index].name}");
        OnFrameChanged?.Invoke(); // notify all listeners (cards)
    }

    // Safe getter for current frame sprite
    public Sprite GetCurrentFrame()
    {
        if (materialFrames == null || materialFrames.Length == 0) return null;
        if (selectedIndex < 0 || selectedIndex >= materialFrames.Length) return null;
        return materialFrames[selectedIndex];
    }

    public int GetSelectedIndex() => selectedIndex;
}
