using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TutorialHighlightOverlay : MonoBehaviour
{
    [Header("Overlay Settings")]
    public Color overlayColor = new Color(0, 0, 0, 0.7f);
    public float highlightPadding = 20f;
    
    [Header("Highlight Animation")]
    public bool animateHighlight = true;
    public float pulseSpeed = 2f;
    public float pulseBrightness = 0.3f;
    
    private Image overlayImage;
    private RectTransform overlayRect;
    private RectTransform highlightTarget;
    private Material overlayMaterial;
    
    // Shader property IDs for performance
    private static readonly int HighlightPosition = Shader.PropertyToID("_HighlightPosition");
    private static readonly int HighlightSize = Shader.PropertyToID("_HighlightSize");
    private static readonly int HighlightRadius = Shader.PropertyToID("_HighlightRadius");
    private static readonly int PulseIntensity = Shader.PropertyToID("_PulseIntensity");
    
    private void Awake()
    {
        overlayImage = GetComponent<Image>();
        overlayRect = GetComponent<RectTransform>();
        
        // Set overlay color
        overlayImage.color = overlayColor;
        
        // Create material for highlight effect if shader is available
        CreateHighlightMaterial();
    }
    
    private void CreateHighlightMaterial()
    {
        // Try to find a highlight shader, or fall back to default UI material
        Shader highlightShader = Shader.Find("UI/TutorialHighlight");
        if (highlightShader != null)
        {
            overlayMaterial = new Material(highlightShader);
            overlayImage.material = overlayMaterial;
        }
        else
        {
            // Fallback: use masking approach with a separate mask GameObject
            Debug.LogWarning("TutorialHighlight shader not found. Using basic overlay without highlight hole.");
        }
    }
    
    private void Update()
    {
        if (highlightTarget != null && overlayMaterial != null)
        {
            UpdateHighlightShader();
        }
    }
    
    private void UpdateHighlightShader()
    {
        if (overlayMaterial == null || highlightTarget == null) return;
        
        // Convert target position to UV coordinates
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, highlightTarget.position);
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, screenPos, null, out localPos);
        
        // Normalize to UV coordinates (0-1)
        Vector2 normalizedPos = new Vector2(
            (localPos.x + overlayRect.rect.width * 0.5f) / overlayRect.rect.width,
            (localPos.y + overlayRect.rect.height * 0.5f) / overlayRect.rect.height
        );
        
        // Calculate highlight size based on target size
        Vector2 targetSize = highlightTarget.rect.size + Vector2.one * highlightPadding;
        Vector2 normalizedSize = new Vector2(
            targetSize.x / overlayRect.rect.width,
            targetSize.y / overlayRect.rect.height
        );
        
        // Update shader properties
        overlayMaterial.SetVector(HighlightPosition, normalizedPos);
        overlayMaterial.SetVector(HighlightSize, normalizedSize);
        
        // Animate pulse effect
        if (animateHighlight)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseBrightness + 1f;
            overlayMaterial.SetFloat(PulseIntensity, pulse);
        }
    }
    
    /// <summary>
    /// Set the target to highlight
    /// </summary>
    /// <param name="target">RectTransform of the UI element to highlight</param>
    public void SetHighlightTarget(RectTransform target)
    {
        highlightTarget = target;
        
        if (target == null)
        {
            // Hide highlight
            if (overlayMaterial != null)
            {
                overlayMaterial.SetVector(HighlightSize, Vector2.zero);
            }
        }
    }
    
    /// <summary>
    /// Set highlight target by GameObject
    /// </summary>
    /// <param name="target">GameObject with RectTransform to highlight</param>
    public void SetHighlightTarget(GameObject target)
    {
        if (target != null)
        {
            RectTransform rectTransform = target.GetComponent<RectTransform>();
            SetHighlightTarget(rectTransform);
        }
        else
        {
            SetHighlightTarget((RectTransform)null);
        }
    }
    
    /// <summary>
    /// Clear the current highlight
    /// </summary>
    public void ClearHighlight()
    {
        SetHighlightTarget((RectTransform)null);
    }
    
    /// <summary>
    /// Show the overlay
    /// </summary>
    public void ShowOverlay()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Hide the overlay
    /// </summary>
    public void HideOverlay()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Set overlay opacity
    /// </summary>
    /// <param name="alpha">Alpha value (0-1)</param>
    public void SetOverlayAlpha(float alpha)
    {
        Color color = overlayImage.color;
        color.a = alpha;
        overlayImage.color = color;
    }
    
    private void OnDestroy()
    {
        // Clean up created material
        if (overlayMaterial != null)
        {
            DestroyImmediate(overlayMaterial);
        }
    }
} 