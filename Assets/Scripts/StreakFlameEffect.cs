using UnityEngine;
using UnityEngine.UI;

public class StreakFlameEffect : MonoBehaviour
{
    [Header("Flame Settings")]
    public Material flameMaterial;
    public Image flameImage;
    public float minStreakLevel = 0f;
    public float maxStreakLevel = 5f;
    
    [Header("Animation")]
    public float transitionSpeed = 5f;
    
    private float _targetStreakLevel = 0f;
    private float _currentStreakLevel = 0f;
    
    private static readonly int StreakLevelProperty = Shader.PropertyToID("_StreakLevel");
    
    void Start()
    {
        // Make sure flame image uses the flame material
        if (flameImage != null && flameMaterial != null)
        {
            // Create a new instance of the material to avoid changing the shared material
            Material instancedMaterial = new Material(flameMaterial);
            flameImage.material = instancedMaterial;
            
            // Initially set to no streak
            instancedMaterial.SetFloat(StreakLevelProperty, 0f);
            flameImage.enabled = false; // Hide flames initially
        }
        else
        {
            Debug.LogWarning("Flame image or material is not set. Streak visual effect will not work.");
        }
    }
    
    void Update()
    {
        // Smoothly transition between current and target streak levels
        if (Mathf.Abs(_currentStreakLevel - _targetStreakLevel) > 0.01f)
        {
            _currentStreakLevel = Mathf.Lerp(_currentStreakLevel, _targetStreakLevel, Time.deltaTime * transitionSpeed);
            
            if (flameImage != null && flameImage.material != null)
            {
                flameImage.material.SetFloat(StreakLevelProperty, _currentStreakLevel);
            }
        }
    }
    
    // Call this method when the streak level changes
    public void SetStreakLevel(int streakCount)
    {
        // Map streak count to shader streak level (0-5 range)
        _targetStreakLevel = Mathf.Clamp(streakCount, 0, (int)maxStreakLevel);
        
        // Enable/disable the flame image based on streak
        if (flameImage != null)
        {
            flameImage.enabled = (_targetStreakLevel > 0);
        }
    }
    
    // Method to instantly set streak level without transition
    public void SetStreakLevelImmediate(int streakCount)
    {
        _targetStreakLevel = Mathf.Clamp(streakCount, 0, (int)maxStreakLevel);
        _currentStreakLevel = _targetStreakLevel;
        
        if (flameImage != null && flameImage.material != null)
        {
            flameImage.material.SetFloat(StreakLevelProperty, _currentStreakLevel);
            flameImage.enabled = (_currentStreakLevel > 0);
        }
    }
} 