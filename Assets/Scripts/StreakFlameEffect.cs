using UnityEngine;
using UnityEngine.UI;

public class StreakFlameEffect : MonoBehaviour
{
    [Header("Flame Prefabs")]
    public GameObject blueFlame;    // Level 1 (default)
    public GameObject greenFlame;   // Level 2
    public GameObject yellowFlame;  // Level 3
    public GameObject pinkFlame;    // Level 4
    public GameObject purpleFlame;  // Level 5
    
    [Header("UI References")]
    public Transform flameContainer; // Where to place the flame
    public Text streakText;         // Text showing current streak
    
    [Header("Testing")]
    [SerializeField] private int testStreakLevel = 1; // For testing in inspector
    
    private GameObject _currentFlame;
    private int _currentLevel = -1; // Start with -1 to force initial update
    private int _lastTestLevel = 1; // Track last test level for editor changes
    
    void Start()
    {
        // Use this object as container if none specified
        if (flameContainer == null)
            flameContainer = transform;
            
        // Initialize with blue flame (1x streak)
        UpdateFlame(1);
        
        Debug.Log("StreakFlameEffect: Started with blue flame");
    }
    
    // This method is called when values change in the inspector (Editor only)
    void OnValidate()
    {
        // Clamp test level to valid range
        testStreakLevel = Mathf.Clamp(testStreakLevel, 1, 5);
        
        // Only update if the test level changed and we're in play mode
        if (Application.isPlaying && testStreakLevel != _lastTestLevel)
        {
            _lastTestLevel = testStreakLevel;
            SetStreakLevel(testStreakLevel);
            Debug.Log($"StreakFlameEffect: Test level changed to {testStreakLevel}");
        }
    }
    
    public void SetStreakLevel(int level)
    {
        // Convert streak multiplier to display level
        // If level is 0, show as 1x with blue flame
        int displayLevel = (level <= 0) ? 1 : level;
        UpdateFlame(displayLevel);
    }
    
    public void SetStreakLevelImmediate(int level)
    {
        // Convert streak multiplier to display level
        int displayLevel = (level <= 0) ? 1 : level;
        UpdateFlame(displayLevel);
    }
    
    private void UpdateFlame(int level)
    {
        Debug.Log($"StreakFlameEffect: UpdateFlame called with level {level}");
        
        // Only update if the level changed
        if (level == _currentLevel) return;
        
        // Store new level
        _currentLevel = level;
        
        // Update streak text
        if (streakText != null)
        {
            streakText.text = level + "x";
            Debug.Log($"StreakFlameEffect: Updated text to {streakText.text}");
        }
        else
        {
            Debug.LogWarning("StreakFlameEffect: streakText is null!");
        }
        
        // Remove current flame
        if (_currentFlame != null)
        {
            Destroy(_currentFlame);
            _currentFlame = null;
        }
        
        // Add appropriate flame based on level
        GameObject flamePrefab = null;
        switch (level)
        {
            case 1: flamePrefab = blueFlame; break;
            case 2: flamePrefab = greenFlame; break;
            case 3: flamePrefab = yellowFlame; break;
            case 4: flamePrefab = pinkFlame; break;
            case 5: flamePrefab = purpleFlame; break;
            default: flamePrefab = blueFlame; break; // Default to blue for any other value
        }
        
        // Instantiate new flame if we have a valid prefab
        if (flamePrefab != null && flameContainer != null)
        {
            _currentFlame = Instantiate(flamePrefab, flameContainer);
            
            // Reset transform for UI compatibility
            _currentFlame.transform.localPosition = Vector3.zero;
            _currentFlame.transform.localRotation = Quaternion.identity;
            _currentFlame.transform.localScale = Vector3.one;
            
            // Ensure particle system renders in front of UI
            ParticleSystem ps = _currentFlame.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.sortingLayerName = "Default";
                    renderer.sortingOrder = 10; // High value to render on top
                }
            }
            
            Debug.Log($"StreakFlameEffect: Instantiated {flamePrefab.name} flame");
        }
        else
        {
            if (flamePrefab == null)
                Debug.LogWarning($"StreakFlameEffect: No flame prefab assigned for level {level}");
            if (flameContainer == null)
                Debug.LogWarning("StreakFlameEffect: flameContainer is null!");
        }
    }
} 