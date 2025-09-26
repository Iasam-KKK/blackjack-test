using UnityEngine;

public class DeckUI : MonoBehaviour
{
    private int pendingIndex = -1;

    // Called when a material thumbnail/button is clicked (set pending)
    public void OnSelectMaterial(int index)
    {
        pendingIndex = index;
        Debug.Log($"DeckUI: Selected material index {index} (pending).");
        // Optional: show preview in UI here (call preview method)
    }

    // Called when user presses the Equip/Confirm button
    public void OnEquipMaterial()
    {
        if (pendingIndex < 0)
        {
            Debug.LogWarning("DeckUI: No material selected to equip.");
            return;
        }

        if (DeckMaterialManager.Instance == null)
        {
            Debug.LogError("DeckUI: DeckMaterialManager.Instance is null.");
            return;
        }

        DeckMaterialManager.Instance.EquipMaterial(pendingIndex);

        // Also try to refresh cards dealt by Deck (for non-CardUI sprites)
        var deckScript = FindObjectOfType<Deck>(); // replace Deck if your class name differs
        if (deckScript != null)
        {
            deckScript.RefreshAllActiveCardFrames();
        }

        pendingIndex = -1; // clear selection
    }

    // Called when user presses Cancel – clear pending choice
    public void OnCancelSelection()
    {
        pendingIndex = -1;
        Debug.Log("DeckUI: Selection canceled.");
    }
}