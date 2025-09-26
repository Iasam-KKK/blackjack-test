using UnityEngine;

public class CardUI : MonoBehaviour
{
    [Header("Assign in Prefab")]
    public SpriteRenderer frameRenderer;  // card border/frame only

    /// <summary>
    /// Apply the currently equipped frame from DeckMaterialManager
    /// </summary>
    public void ApplyEquippedFrame()
    {
        if (frameRenderer == null)
        {
            Debug.LogError("❌ CardUI: frameRenderer is not assigned in prefab!");
            return;
        }

        if (DeckMaterialManager.Instance != null)
        {
            Sprite frame = DeckMaterialManager.Instance.GetCurrentFrame();
            if (frame != null)
            {
                frameRenderer.sprite = frame;
                frameRenderer.enabled = true;
                Debug.Log($"✅ CardUI: Frame applied -> {frame.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ CardUI: Frame sprite is NULL from DeckMaterialManager.");
            }
        }
        else
        {
            Debug.LogError("❌ CardUI: DeckMaterialManager.Instance is missing in scene!");
        }
    }
}