using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardPreviewManager : MonoBehaviour
{
    public static CardPreviewManager Instance { get; private set; }
    
    [Header("Preview UI Components")]
    public GameObject previewPanel;
    public Transform previewContainer;
    public GameObject previewCardPrefab;
    // public Text previewTitleText;
    public Button confirmButton;
    public Button cancelButton;
    public Button shuffleButton; // For Mad Writer card
    
    [Header("Settings")]
    public float cardSpacing = 100f;
    public float animationDuration = 0.3f;
    
    // Current preview state
    private List<GameObject> previewCards = new List<GameObject>();
    private List<CardInfo> originalCardInfos = new List<CardInfo>();
    private System.Action<List<CardInfo>> onConfirmCallback;
    private System.Action onCancelCallback;
    private bool allowRearranging = false;
    private bool allowRemoving = false;
    private int maxRemovable = 0;
    private int removedCount = 0;
    
    // Drag and drop variables
    private GameObject draggedCard;
    private Vector3 dragOffset;
    private int draggedCardIndex = -1;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Hide preview panel initially
        if (previewPanel != null)
        {
            previewPanel.SetActive(false);
        }
        
        // Set up button listeners
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmPreview);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelPreview);
        }
        
        if (shuffleButton != null)
        {
            shuffleButton.onClick.AddListener(TriggerShuffle);
        }
    }
    
    /// <summary>
    /// Show preview cards with optional interaction capabilities
    /// </summary>
    public void ShowPreview(List<CardInfo> cardInfos, string title, bool canRearrange = false, bool canRemove = false, int maxRemove = 1, System.Action<List<CardInfo>> onConfirm = null, System.Action onCancel = null)
    {
        originalCardInfos = new List<CardInfo>(cardInfos);
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;
        allowRearranging = canRearrange;
        allowRemoving = canRemove;
        maxRemovable = maxRemove;
        removedCount = 0;
        
        // Set title
        // if (previewTitleText != null)
        // {
        //     previewTitleText.text = title;
        // }
        
        // Show/hide buttons based on functionality
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(canRearrange || canRemove);
        }
        
        if (shuffleButton != null)
        {
            shuffleButton.gameObject.SetActive(false); // Only show for Mad Writer
        }
        
        // Create preview cards
        CreatePreviewCards(cardInfos);
        
        // Show panel with animation
        previewPanel.SetActive(true);
        previewPanel.transform.localScale = Vector3.zero;
        previewPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
    }
    
    /// <summary>
    /// Show preview for Mad Writer (with shuffle option)
    /// </summary>
    public void ShowMadWriterPreview(CardInfo nextCard, System.Action onConfirm, System.Action onShuffle, System.Action onCancel)
    {
        List<CardInfo> cardList = new List<CardInfo> { nextCard };
        onConfirmCallback = (cards) => onConfirm?.Invoke(); // Wrap the action properly
        onCancelCallback = onCancel;
        allowRearranging = false;
        allowRemoving = false;
        
        // Set title
        //  if (previewTitleText != null)
        // {
        //     previewTitleText.text = "Next Card - Shuffle deck if desired";
        // }
            
        // Show shuffle button
        if (shuffleButton != null)
        {
            shuffleButton.gameObject.SetActive(true);
            shuffleButton.onClick.RemoveAllListeners();
            shuffleButton.onClick.AddListener(() => {
                onShuffle?.Invoke();
                HidePreview();
            });
        }
        
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
        }
        
        // Create preview card
        CreatePreviewCards(cardList);
        
        // Show panel
        previewPanel.SetActive(true);
        previewPanel.transform.localScale = Vector3.zero;
        previewPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
    }
    
    private void CreatePreviewCards(List<CardInfo> cardInfos)
    {
        // Clear existing cards
        ClearPreviewCards();
        
        // Create new cards
        for (int i = 0; i < cardInfos.Count; i++)
        {
            GameObject cardObj = CreatePreviewCard(cardInfos[i], i);
            previewCards.Add(cardObj);
        }
        
        // Arrange cards
        ArrangePreviewCards();
    }
    
    private GameObject CreatePreviewCard(CardInfo cardInfo, int index)
    {
        GameObject cardObj = Instantiate(previewCardPrefab, previewContainer);
        
        // Set up card display
        Image cardImage = cardObj.GetComponent<Image>();
        if (cardImage != null && cardInfo.cardSprite != null)
        {
            cardImage.sprite = cardInfo.cardSprite;
        }
        
        // Add card name text
        Text cardNameText = cardObj.GetComponentInChildren<Text>();
        if (cardNameText != null)
        {
            cardNameText.text = cardInfo.cardName;
        }
        
        // Add interaction if allowed
        if (allowRearranging || allowRemoving)
        {
            AddCardInteraction(cardObj, index);
        }
        
        return cardObj;
    }
    
    private void AddCardInteraction(GameObject cardObj, int index)
    {
        // Add EventTrigger for drag and drop or removal
        EventTrigger trigger = cardObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = cardObj.AddComponent<EventTrigger>();
        }
        
        if (allowRearranging)
        {
            // Add drag functionality
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { StartDrag(cardObj, index, (PointerEventData)data); });
            trigger.triggers.Add(pointerDown);
            
            EventTrigger.Entry drag = new EventTrigger.Entry();
            drag.eventID = EventTriggerType.Drag;
            drag.callback.AddListener((data) => { OnDrag((PointerEventData)data); });
            trigger.triggers.Add(drag);
            
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.EndDrag;
            pointerUp.callback.AddListener((data) => { EndDrag((PointerEventData)data); });
            trigger.triggers.Add(pointerUp);
        }
        
        if (allowRemoving)
        {
            // Add click to remove functionality
            Button cardButton = cardObj.GetComponent<Button>();
            if (cardButton == null)
            {
                cardButton = cardObj.AddComponent<Button>();
            }
            
            cardButton.onClick.AddListener(() => RemoveCard(cardObj, index));
            
            // Visual indicator for removable cards
            Image cardImage = cardObj.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = new Color(1f, 0.8f, 0.8f); // Light red tint
            }
        }
    }
    
    private void StartDrag(GameObject cardObj, int index, PointerEventData eventData)
    {
        if (!allowRearranging) return;
        
        draggedCard = cardObj;
        draggedCardIndex = index;
        
        Vector3 worldPoint;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            previewContainer as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out worldPoint);
        
        dragOffset = cardObj.transform.position - worldPoint;
        
        // Bring to front
        cardObj.transform.SetAsLastSibling();
        
        // Visual feedback
        cardObj.transform.DOScale(Vector3.one * 1.1f, 0.1f);
    }
    
    private void OnDrag(PointerEventData eventData)
    {
        if (draggedCard == null) return;
        
        Vector3 worldPoint;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            previewContainer as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out worldPoint);
        
        draggedCard.transform.position = worldPoint + dragOffset;
    }
    
    private void EndDrag(PointerEventData eventData)
    {
        if (draggedCard == null) return;
        
        // Find closest position to swap with
        int closestIndex = FindClosestCardPosition(draggedCard.transform.position);
        
        if (closestIndex != -1 && closestIndex != draggedCardIndex)
        {
            // Swap cards
            SwapCards(draggedCardIndex, closestIndex);
        }
        
        // Reset visual state
        draggedCard.transform.DOScale(Vector3.one, 0.1f);
        draggedCard = null;
        draggedCardIndex = -1;
        
        // Rearrange all cards
        ArrangePreviewCards();
    }
    
    private int FindClosestCardPosition(Vector3 position)
    {
        float closestDistance = float.MaxValue;
        int closestIndex = -1;
        
        for (int i = 0; i < previewCards.Count; i++)
        {
            if (previewCards[i] == draggedCard) continue;
            
            float distance = Vector3.Distance(position, previewCards[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }
    
    private void SwapCards(int index1, int index2)
    {
        if (index1 < 0 || index1 >= previewCards.Count || index2 < 0 || index2 >= previewCards.Count)
            return;
        
        // Swap in preview cards list
        GameObject temp = previewCards[index1];
        previewCards[index1] = previewCards[index2];
        previewCards[index2] = temp;
        
        // Swap in original card infos list
        CardInfo tempInfo = originalCardInfos[index1];
        originalCardInfos[index1] = originalCardInfos[index2];
        originalCardInfos[index2] = tempInfo;
    }
    
    private void RemoveCard(GameObject cardObj, int index)
    {
        if (!allowRemoving || removedCount >= maxRemovable) return;
        
        // Mark as removed
        cardObj.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        cardObj.GetComponent<Button>().interactable = false;
        
        // Add removed indicator
        Text removedText = cardObj.GetComponentInChildren<Text>();
        if (removedText != null)
        {
            removedText.text = "REMOVED";
            removedText.color = Color.red;
        }
        
        removedCount++;
        
        // Update confirm button text
        if (confirmButton != null)
        {
            Text buttonText = confirmButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Remove ({removedCount}/{maxRemovable})";
            }
        }
    }
    
    private void ArrangePreviewCards()
    {
        for (int i = 0; i < previewCards.Count; i++)
        {
            float xPos = (i - (previewCards.Count - 1) * 0.5f) * cardSpacing;
            Vector3 targetPos = new Vector3(xPos, 0, 0);
            
            if (previewCards[i] != draggedCard)
            {
                previewCards[i].transform.DOLocalMove(targetPos, 0.2f).SetEase(Ease.OutQuad);
            }
        }
    }
    
    private void ClearPreviewCards()
    {
        foreach (GameObject card in previewCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        previewCards.Clear();
    }
    
    private void ConfirmPreview()
    {
        List<CardInfo> resultCards = new List<CardInfo>();
        
        if (allowRemoving)
        {
            // Only include non-removed cards
            for (int i = 0; i < previewCards.Count; i++)
            {
                Button cardButton = previewCards[i].GetComponent<Button>();
                if (cardButton != null && cardButton.interactable)
                {
                    resultCards.Add(originalCardInfos[i]);
                }
            }
        }
        else
        {
            resultCards = new List<CardInfo>(originalCardInfos);
        }
        
        onConfirmCallback?.Invoke(resultCards);
        HidePreview();
    }
    
    private void CancelPreview()
    {
        onCancelCallback?.Invoke();
        HidePreview();
    }
    
    private void TriggerShuffle()
    {
        // This will be handled by Mad Writer specific callback
        HidePreview();
    }
    
    public void HidePreview()
    {
        if (previewPanel != null)
        {
            previewPanel.transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
                .OnComplete(() => {
                    previewPanel.SetActive(false);
                    ClearPreviewCards();
                });
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
} 