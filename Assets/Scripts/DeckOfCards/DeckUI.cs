using UnityEngine;

public class DeckUI : MonoBehaviour
{
    private int pendingIndex = -1;

    public void OnSelectMaterial(int index)
    {
        pendingIndex = index;
        // optional: show preview
    }

    public void OnEquipMaterial()
    {
        if (pendingIndex < 0) return;
        if (DeckMaterialManager.Instance == null) return;

        DeckMaterialManager.Instance.EquipMaterial(pendingIndex);

        // optional: force deck to refresh (redundant because CardUI listens already)
        var deck = FindObjectOfType<Deck>();
        if (deck != null) deck.RefreshAllActiveCardFrames();

        pendingIndex = -1;
    }

    public void OnCancelSelection()
    {
        pendingIndex = -1;
    }
}
