using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Represents an action card that can be used during gameplay
/// Consumes action budget when used
/// </summary>
public class ActionCard : MonoBehaviour, IPointerClickHandler
{
    [Header("Action Card Data")]
    public ActionCardData actionData;
    
    [Header("UI Components")]
    public Image cardImage;
    public Image iconImage;
    public Text nameText;
    public Text costText;
    
    private Deck deck;
    private bool hasBeenUsedThisHand = false;
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    private void Start()
    {
        deck = FindObjectOfType<Deck>();
        InitializeVisuals();
    }
    
    private void InitializeVisuals()
    {
        if (actionData == null) return;
        
        // Set card visuals
        if (cardImage != null && actionData.cardBackground != null)
        {
            cardImage.sprite = actionData.cardBackground;
        }
        else if (cardImage != null)
        {
            cardImage.color = actionData.cardColor;
        }
        
        // Set icon
        if (iconImage != null && actionData.actionIcon != null)
        {
            iconImage.sprite = actionData.actionIcon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }
        
        // Set name
        if (nameText != null)
        {
            nameText.text = actionData.actionName;
        }
        
        // Set cost
        if (costText != null)
        {
            costText.text = $"{actionData.actionsRequired}";
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        TryUseAction();
    }
    
    private void TryUseAction()
    {
        if (deck == null)
        {
            Debug.LogError("Deck not found!");
            return;
        }
        
        // Check if already used this hand (if not reusable)
        if (hasBeenUsedThisHand && !actionData.canBeUsedMultipleTimes)
        {
            Debug.Log($"Action {actionData.actionName} already used this hand");
            return;
        }
        
        // Check if enough actions available
        if (deck.GetRemainingActions() < actionData.actionsRequired)
        {
            Debug.LogWarning($"Not enough actions! Need {actionData.actionsRequired}, have {deck.GetRemainingActions()}");
            return;
        }
        
        // Try to execute the action
        bool actionExecuted = ExecuteAction();
        
        if (actionExecuted)
        {
            // Consume action points
            for (int i = 0; i < actionData.actionsRequired; i++)
            {
                deck.ConsumeAction();
            }
            
            hasBeenUsedThisHand = true;
            
            // Visual feedback
            StartCoroutine(PlayUseAnimation());
            
            Debug.Log($"Action {actionData.actionName} used successfully!");
        }
    }
    
    private bool ExecuteAction()
    {
        switch (actionData.actionType)
        {
            case ActionCardType.SwapTwoCards:
                return deck.ActionSwapTwoCards();
            
            case ActionCardType.AddOneToCard:
                // ActionAddOneToCard is now void and handles its own action consumption
                // So this shouldn't be called from ActionCard anymore
                deck.ActionAddOneToCard();
                return true; // Assume success since method handles its own validation
            
            case ActionCardType.SubtractOneFromCard:
                return deck.ActionSubtractOneFromCard();
            
            case ActionCardType.PeekDealerCard:
                return deck.ActionPeekDealerCard();
            
            case ActionCardType.ForceRedraw:
                return deck.ActionForceRedraw();
            
            case ActionCardType.DoubleCardValue:
                return deck.ActionDoubleCardValue();
            
            case ActionCardType.SetCardToTen:
                return deck.ActionSetCardToTen();
            
            case ActionCardType.FlipAce:
                return deck.ActionFlipAce();
            
            case ActionCardType.ShieldCard:
                return deck.ActionShieldCard();
            
            case ActionCardType.CopyCard:
                return deck.ActionCopyCard();
            
            // Low-impact action card modifiers
            case ActionCardType.ValuePlusOne:
                return deck.ActionValuePlusOne();
            
            case ActionCardType.MinorSwapWithDealer:
                return deck.ActionMinorSwapWithDealer();
            
            case ActionCardType.MinorHeal:
                return deck.ActionMinorHeal();
            
            default:
                Debug.LogWarning($"Action type {actionData.actionType} not implemented!");
                return false;
        }
    }
    
    private IEnumerator PlayUseAnimation()
    {
        // Pulse animation
        transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(0.2f);
        transform.DOScale(1.0f, 0.2f).SetEase(Ease.InQuad);
        
        // Dim if not reusable
        if (!actionData.canBeUsedMultipleTimes && canvasGroup != null)
        {
            canvasGroup.alpha = 0.5f;
        }
    }
    
    public void ResetForNewHand()
    {
        hasBeenUsedThisHand = false;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1.0f;
        }
        
        transform.localScale = Vector3.one;
    }
}

