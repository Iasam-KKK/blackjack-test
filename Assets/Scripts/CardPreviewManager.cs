using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

public class CardPreviewManager : MonoBehaviour
{
    public static CardPreviewManager Instance { get; private set; }
    
    [Header("Preview UI Components")]
    public GameObject previewPanel;
    public Transform previewContainer;
   // public GameObject previewCardPrefab;
    // public Text previewTitleText;
    public Button confirmButton;
    public Button cancelButton;
    public Button shuffleButton; // For Mad Writer card
    
    [Header("Settings")]
    public float cardSpacing = 100f;
    public float animationDuration = 0.3f;
    public float dragScaleMultiplier = 1.1f; // Scale factor when dragging
    public float swapAnimationDuration = 0.3f; // Duration for swap animations
    
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
    private Vector3 originalDragCardPosition;
    private bool isDragging = false;
    [Header("Single Card Preview")]
    public Image singlePreviewImage;
    public TextMeshProUGUI singlePreviewName;
    [Header("Multi Card Preview")]
    public GameObject multiPreviewPanel;
    public Image[] previewImages; // Size 3
    public TextMeshProUGUI[] previewNames; // Size 3

    [SerializeField] private GameObject playerPreviewPanel;
    [SerializeField] private Image[] playerPreviewImages;
    [SerializeField] private TextMeshProUGUI[] playerPreviewNames;

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
    /*
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
    */
    public void ShowPreview(List<CardInfo> cardInfos, string title, bool canRearrange = false, bool canRemove = false, int maxRemove = 1, System.Action<List<CardInfo>> onConfirm = null, System.Action onCancel = null)
    {
        if (cardInfos == null || cardInfos.Count == 0) return;

        if (cardInfos.Count == 1)
        {
            // SINGLE PREVIEW
            CardInfo card = cardInfos[0];

            if (singlePreviewImage != null && card.cardSprite != null)
                singlePreviewImage.sprite = card.cardSprite;

            if (singlePreviewName != null)
                singlePreviewName.text = card.cardName;

            previewPanel.SetActive(true);
            previewPanel.transform.localScale = Vector3.zero;
            previewPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
            StartCoroutine(AutoClosePanel(previewPanel, 1.5f));
        }
        else
        {
            // MULTI PREVIEW
            multiPreviewPanel.SetActive(true);

            for (int i = 0; i < previewImages.Length; i++)
            {
                if (i < cardInfos.Count)
                {
                    previewImages[i].sprite = cardInfos[i].cardSprite;
                    previewImages[i].gameObject.SetActive(true);
                    previewNames[i].text = cardInfos[i].cardName;
                    previewNames[i].gameObject.SetActive(true);
                }
                else
                {
                    previewImages[i].gameObject.SetActive(false);
                    previewNames[i].gameObject.SetActive(false);
                }
            }

            multiPreviewPanel.transform.localScale = Vector3.zero;
            multiPreviewPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
            StartCoroutine(AutoClosePanel(multiPreviewPanel, 10f));
        }
    }

    /// <summary>
    /// Show preview for Corrupt Judge with drag and drop functionality
    /// </summary>
    public void ShowCorruptJudgePreview(List<CardInfo> cardInfos, System.Action<List<CardInfo>> onConfirm, System.Action onCancel)
    {
        if (cardInfos == null || cardInfos.Count == 0) return;

        originalCardInfos = new List<CardInfo>(cardInfos);
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;
        allowRearranging = true;
        allowRemoving = false;
        maxRemovable = 0;
        removedCount = 0;
        
        // Show confirm and cancel buttons
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            Text buttonText = confirmButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Confirm Arrangement";
            }
        }
        
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(true);
        }
        
        if (shuffleButton != null)
        {
            shuffleButton.gameObject.SetActive(false);
        }
        
        // Use the multi-preview panel for Corrupt Judge
        multiPreviewPanel.SetActive(true);

        // Clear any existing drag interactions
        ClearPreviewCards();

        for (int i = 0; i < previewImages.Length; i++)
        {
            if (i < cardInfos.Count)
            {
                previewImages[i].sprite = cardInfos[i].cardSprite;
                previewImages[i].gameObject.SetActive(true);
                previewNames[i].text = cardInfos[i].cardName;
                previewNames[i].gameObject.SetActive(true);
                
                // Add drag functionality to the first two cards only
                if (i < 2)
                {
                    AddDragInteractionToImage(previewImages[i].gameObject, i);
                }
            }
            else
            {
                previewImages[i].gameObject.SetActive(false);
                previewNames[i].gameObject.SetActive(false);
            }
        }
        
        // Set initial positions for all cards
        SetInitialCardPositions();

        multiPreviewPanel.transform.localScale = Vector3.zero;
        multiPreviewPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
    }
    
    private void AddDragInteractionToImage(GameObject imageObj, int index)
    {
        // Add EventTrigger for drag and drop
        EventTrigger trigger = imageObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = imageObj.AddComponent<EventTrigger>();
        }
        
        // Clear existing triggers
        trigger.triggers.Clear();
        
        // Add drag functionality
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { StartDragImage(imageObj, index, (PointerEventData)data); });
        trigger.triggers.Add(pointerDown);
        
        EventTrigger.Entry drag = new EventTrigger.Entry();
        drag.eventID = EventTriggerType.Drag;
        drag.callback.AddListener((data) => { OnDragImage((PointerEventData)data); });
        trigger.triggers.Add(drag);
        
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.EndDrag;
        pointerUp.callback.AddListener((data) => { EndDragImage((PointerEventData)data); });
        trigger.triggers.Add(pointerUp);
        
        // Add click functionality as fallback
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback.AddListener((data) => { OnCardClick(imageObj, index, (PointerEventData)data); });
        trigger.triggers.Add(pointerClick);
    }
    
    private void OnCardClick(GameObject imageObj, int index, PointerEventData eventData)
    {
        // Only handle clicks if we're not currently dragging
        if (isDragging) return;
        
        // Simple swap: if card 0 is clicked, swap with card 1, and vice versa
        if (index == 0 && previewImages[1].gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateImageSwap(0, 1));
        }
        else if (index == 1 && previewImages[0].gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateImageSwap(1, 0));
        }
    }
    
    private void StartDragImage(GameObject imageObj, int index, PointerEventData eventData)
    {
        if (!allowRearranging || isDragging) return;
        
        // Check if we clicked on a button - if so, don't start dragging
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        if (clickedObject != null)
        {
            Button clickedButton = clickedObject.GetComponent<Button>();
            if (clickedButton != null)
            {
                return;
            }
        }
        
        isDragging = true;
        draggedCard = imageObj;
        draggedCardIndex = index;
        originalDragCardPosition = imageObj.transform.localPosition;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            multiPreviewPanel.transform as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint);
        
        dragOffset = imageObj.transform.localPosition - new Vector3(localPoint.x, localPoint.y, 0);
        
        // Bring to front
        imageObj.transform.SetAsLastSibling();
        
        // Visual feedback - scale up
        imageObj.transform.DOScale(Vector3.one * dragScaleMultiplier, 0.1f);
        
        // Add shadow or glow effect
        Image cardImage = imageObj.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.color = new Color(1f, 1f, 1f, 0.9f); // Slightly transparent
        }
    }
    
    private void OnDragImage(PointerEventData eventData)
    {
        if (draggedCard == null || !isDragging) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            multiPreviewPanel.transform as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint);
        
        draggedCard.transform.localPosition = new Vector3(localPoint.x, localPoint.y, 0) + dragOffset;
        
        // Highlight potential swap targets
        HighlightPotentialSwapTargetsImage();
    }
    
    private void EndDragImage(PointerEventData eventData)
    {
        if (draggedCard == null || !isDragging) return;
        
        // Find closest position to swap with
        int closestIndex = FindClosestImagePosition(draggedCard.transform.localPosition);
        
        if (closestIndex != -1 && closestIndex != draggedCardIndex && closestIndex < 2)
        {
            // Animate the swap
            StartCoroutine(AnimateImageSwap(draggedCardIndex, closestIndex));
        }
        else
        {
            // Return to original position
            draggedCard.transform.DOLocalMove(originalDragCardPosition, 0.2f).SetEase(Ease.OutQuad);
        }
        
        // Reset visual state
        draggedCard.transform.DOScale(Vector3.one, 0.1f);
        
        // Reset card appearance
        Image cardImage = draggedCard.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.color = Color.white;
        }
        
        // Clear highlights
        ClearImageHighlights();
        
        draggedCard = null;
        draggedCardIndex = -1;
        isDragging = false;
    }
    
    private void HighlightPotentialSwapTargetsImage()
    {
        for (int i = 0; i < 2; i++) // Only first two cards
        {
            if (previewImages[i].gameObject == draggedCard) continue;
            
            Image cardImage = previewImages[i].GetComponent<Image>();
            if (cardImage != null)
            {
                // Check if this card is close enough to be a swap target
                float distance = Vector3.Distance(draggedCard.transform.localPosition, previewImages[i].transform.localPosition);
                if (distance < cardSpacing * 0.5f)
                {
                    cardImage.color = new Color(1f, 1f, 0.8f, 1f); // Light yellow highlight
                }
                else
                {
                    cardImage.color = Color.white;
                }
            }
        }
    }
    
    private void ClearImageHighlights()
    {
        for (int i = 0; i < previewImages.Length; i++)
        {
            Image cardImage = previewImages[i].GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = Color.white;
            }
        }
    }
    
    private int FindClosestImagePosition(Vector3 position)
    {
        float closestDistance = float.MaxValue;
        int closestIndex = -1;
        
        for (int i = 0; i < 2; i++) // Only first two cards
        {
            if (previewImages[i].gameObject == draggedCard) continue;
            
            float distance = Vector3.Distance(position, previewImages[i].transform.localPosition);
            if (distance < closestDistance && distance < cardSpacing * 0.5f) // Only consider close cards
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }
    
    private IEnumerator AnimateImageSwap(int index1, int index2)
    {
        if (index1 < 0 || index1 >= 2 || index2 < 0 || index2 >= 2)
            yield break;
        
        GameObject card1 = previewImages[index1].gameObject;
        GameObject card2 = previewImages[index2].gameObject;
        
        Vector3 pos1 = card1.transform.localPosition;
        Vector3 pos2 = card2.transform.localPosition;
        
        // Animate both cards to their new positions
        card1.transform.DOLocalMove(pos2, swapAnimationDuration).SetEase(Ease.OutQuad);
        card2.transform.DOLocalMove(pos1, swapAnimationDuration).SetEase(Ease.OutQuad);
        
        yield return new WaitForSeconds(swapAnimationDuration);
        
        // Swap in original card infos list
        CardInfo tempInfo = originalCardInfos[index1];
        originalCardInfos[index1] = originalCardInfos[index2];
        originalCardInfos[index2] = tempInfo;
        
        // Update the display
        previewImages[index1].sprite = originalCardInfos[index1].cardSprite;
        previewImages[index2].sprite = originalCardInfos[index2].cardSprite;
        previewNames[index1].text = originalCardInfos[index1].cardName;
        previewNames[index2].text = originalCardInfos[index2].cardName;
        
        // Reposition cards to their proper positions with proper spacing
        RepositionCardsAfterSwap();
    }
    
    private void RepositionCardsAfterSwap()
    {
        // Calculate proper positions for all cards
        float totalWidth = (previewImages.Length - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < previewImages.Length; i++)
        {
            if (previewImages[i].gameObject.activeInHierarchy)
            {
                Vector3 targetPosition = new Vector3(startX + (i * cardSpacing), 0, 0);
                previewImages[i].transform.DOLocalMove(targetPosition, 0.3f).SetEase(Ease.OutQuad);
            }
        }
    }
    
    private void SetInitialCardPositions()
    {
        // Calculate proper positions for all cards
        float totalWidth = (previewImages.Length - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < previewImages.Length; i++)
        {
            if (previewImages[i].gameObject.activeInHierarchy)
            {
                Vector3 targetPosition = new Vector3(startX + (i * cardSpacing), 0, 0);
                previewImages[i].transform.localPosition = targetPosition;
            }
        }
    }

    public void ShowPlayerPreview(List<CardInfo> cardInfos)
    {
        if (cardInfos == null || cardInfos.Count == 0) return;

        playerPreviewPanel.SetActive(true);

        for (int i = 0; i < playerPreviewImages.Length; i++)
        {
            if (i < cardInfos.Count)
            {
                playerPreviewImages[i].sprite = cardInfos[i].cardSprite;
                playerPreviewImages[i].gameObject.SetActive(true);
                playerPreviewNames[i].text = cardInfos[i].cardName;
                playerPreviewNames[i].gameObject.SetActive(true);
            }
            else
            {
                playerPreviewImages[i].gameObject.SetActive(false);
                playerPreviewNames[i].gameObject.SetActive(false);
            }
        }

        playerPreviewPanel.transform.localScale = Vector3.zero;
        playerPreviewPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        StartCoroutine(AutoClosePanel(playerPreviewPanel, 1.5f));
    }

    private IEnumerator AutoClosePanel(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
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
            //GameObject cardObj = CreatePreviewCard(cardInfos[i], i);
           // previewCards.Add(cardObj);
        }
        
        // Arrange cards
        ArrangePreviewCards();
    }
    
    /*
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
    */
    
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
        cardObj.transform.DOScale(Vector3.one * dragScaleMultiplier, 0.1f);
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
                previewCards[i].transform.DOLocalMove(targetPos, swapAnimationDuration).SetEase(Ease.OutQuad);
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
        
        // Hide the appropriate panel based on what's currently active
        if (multiPreviewPanel != null && multiPreviewPanel.activeInHierarchy)
        {
            HideMultiPreviewPanel();
        }
        else
        {
            HidePreview();
        }
    }
    
    private void HideMultiPreviewPanel()
    {
        if (multiPreviewPanel != null)
        {
            multiPreviewPanel.transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
                .OnComplete(() => {
                    multiPreviewPanel.SetActive(false);
                });
        }
    }
    
    private void CancelPreview()
    {
        onCancelCallback?.Invoke();
        
        // Hide the appropriate panel based on what's currently active
        if (multiPreviewPanel != null && multiPreviewPanel.activeInHierarchy)
        {
            HideMultiPreviewPanel();
        }
        else
        {
            HidePreview();
        }
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

    private IEnumerator AutoClosePanel(float dealy)
    {
        yield return new WaitForSeconds(dealy);
        HidePreview();
    }
} 