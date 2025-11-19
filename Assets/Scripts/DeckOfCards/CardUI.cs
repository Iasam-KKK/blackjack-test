using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Transform))]
public class CardUI : MonoBehaviour
{
    // We'll auto-find the child named "Frame" (Image)
    private Image frameRenderer;

    private void Awake()
    {
        // find "Frame" child (case-sensitive)
        Transform t = transform.Find("Frame");
        if (t != null)
            frameRenderer = t.GetComponent<Image>();
    }

    private void OnEnable()
    {
        // Subscribe if manager already exists
        if (DeckMaterialManager.Instance != null)
        {
            DeckMaterialManager.Instance.OnFrameChanged += ApplyEquippedFrame;
        }

        // Apply current frame immediately (in case Equip happened earlier)
        ApplyEquippedFrame();
    }

    private void OnDisable()
    {
        if (DeckMaterialManager.Instance != null)
        {
            DeckMaterialManager.Instance.OnFrameChanged -= ApplyEquippedFrame;
        }
    }

    /// <summary>
    /// Set frameRenderer.sprite to the currently equipped sprite.
    /// </summary>
    public void ApplyEquippedFrame()
    {
        if (frameRenderer == null)
        {
            Debug.LogWarning($"CardUI: Frame child not found on '{gameObject.name}'. Expected child named 'Frame'.");
            return;
        }

        if (DeckMaterialManager.Instance == null)
        {
            Debug.LogError("CardUI: DeckMaterialManager.Instance is missing in scene!");
            return;
        }

        Sprite frame = DeckMaterialManager.Instance.GetCurrentFrame();
        if (frame != null)
        {
            frameRenderer.sprite = frame;
            frameRenderer.enabled = true;
            // optional: make sure rendering order is correct
        }
        else
        {
            // No frame selected -> hide frame
            frameRenderer.sprite = null;
            frameRenderer.enabled = false;
        }
    }
}