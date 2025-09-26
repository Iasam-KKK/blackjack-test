using UnityEngine;
using System;

public class DeckMaterialManager : MonoBehaviour
{
    public static DeckMaterialManager Instance;

    [Header("Available Frames (assign in Inspector)")]
    public Sprite[] materialFrames; // drag your frame sprites here

    private int selectedIndex = -1; // -1 = no frame equipped
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

        // Load saved value
        selectedIndex = PlayerPrefs.GetInt(PREF_KEY, -1);

        // Validate index
        if (materialFrames == null || materialFrames.Length == 0) selectedIndex = -1;
        if (selectedIndex >= materialFrames.Length) selectedIndex = -1;

        // Notify cards so they set frame (or none)
        OnFrameChanged?.Invoke();
    }

    // Equip a frame (0..N-1). Call with -1 to remove frame.
    public void EquipMaterial(int index)
    {
        if (index == -1)
        {
            selectedIndex = -1;
            PlayerPrefs.SetInt(PREF_KEY, selectedIndex);
            PlayerPrefs.Save();

            Debug.Log("DeckMaterialManager: No frame equipped.");
            OnFrameChanged?.Invoke();
            return;
        }

        if (materialFrames == null || materialFrames.Length == 0) return;
        if (index < 0 || index >= materialFrames.Length) return;

        selectedIndex = index;
        PlayerPrefs.SetInt(PREF_KEY, selectedIndex);
        PlayerPrefs.Save();

        Debug.Log($"DeckMaterialManager: Equipped material index {index} -> {materialFrames[index].name}");
        OnFrameChanged?.Invoke();
    }

    public Sprite GetCurrentFrame()
    {
        if (selectedIndex == -1) return null; // no frame
        if (materialFrames == null || materialFrames.Length == 0) return null;
        if (selectedIndex < 0 || selectedIndex >= materialFrames.Length) return null;
        return materialFrames[selectedIndex];
    }

    public int GetSelectedIndex() => selectedIndex;
}
