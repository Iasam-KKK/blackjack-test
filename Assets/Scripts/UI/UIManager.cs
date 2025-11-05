using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Deck deckController;
    
    [Header("Boss System UI")]
    public NewBossPanel newBossPanel;
    public Button nextHandButton; // This should reference the Play Again button in the inspector
    
    private void Start()
    {
        if (deckController == null)
        {
            deckController = FindObjectOfType<Deck>();
            if (deckController == null)
            {
                Debug.LogError("No Deck controller found in scene!");
                return;
            }
        }
        
        // Set up boss UI reference in Deck script
        if (newBossPanel != null)
            deckController.newBossPanel = newBossPanel;
        else
            Debug.LogWarning("New Boss Panel not assigned in UI Manager!");
            
        // Make sure the Play Again button is properly named
        if (nextHandButton != null)
        {
            Text buttonText = nextHandButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Next Hand";
            }
            
            // Connect Play Again button if not already connected
            if (deckController.playAgainButton == null)
            {
                deckController.playAgainButton = nextHandButton;
            }
        }
    }
} 