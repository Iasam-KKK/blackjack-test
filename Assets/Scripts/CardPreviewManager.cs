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
    
    [Header("Panel-Specific Buttons")]
    [Header("Blind Seer Buttons")]
    public Button blindSeerConfirmButton;
    public Button blindSeerCancelButton;
    public Button blindSeerShuffleButton;
    
    [Header("Corrupt Judge Buttons")]
    public Button corruptJudgeConfirmButton;
    public Button corruptJudgeCancelButton;
    public Button corruptJudgeShuffleButton;
    
    [Header("Hitman Buttons")]
    public Button hitmanConfirmButton;
    public Button hitmanCancelButton;
    public Button hitmanShuffleButton;
    
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
    /*[Header("Multi Card Preview")]
    private GameObject multiPreviewPanel;
    private Image[] previewImages; // Size 3
    private TextMeshProUGUI[] previewNames; // Size 3*/
    
    [Header("Blind Seer Preview")]
    [SerializeField] private GameObject blindSeerPanel;
    [SerializeField] private Image[] blindSeerImages; // Size 3
    [SerializeField] private TextMeshProUGUI[] blindSeerNames; // Size 3

    [Header("Corrupt Judge Preview")]
    [SerializeField] private GameObject corruptJudgePanel;
    [SerializeField] private Image[] corruptJudgeImages; // Size 3
    [SerializeField] private TextMeshProUGUI[] corruptJudgeNames; // Size 3
    
    [Header("HitMan Preview")]
    [SerializeField] private GameObject playerPreviewPanel;
    [SerializeField] private Image[] playerPreviewImages;
    [SerializeField] private TextMeshProUGUI[] playerPreviewNames;
    
    // Hitman card selection variables
    private int selectedCardIndex = -1;
    private bool isCardSelected = false;

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
        
        // Set up button listeners for main panel
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
        
        // Set up button listeners for Blind Seer panel
        if (blindSeerConfirmButton != null)
        {
            blindSeerConfirmButton.onClick.AddListener(ConfirmPreview);
        }
        
        if (blindSeerCancelButton != null)
        {
            blindSeerCancelButton.onClick.AddListener(CancelPreview);
        }
        
        if (blindSeerShuffleButton != null)
        {
            blindSeerShuffleButton.onClick.AddListener(TriggerShuffle);
        }
        
        // Set up button listeners for Corrupt Judge panel
        if (corruptJudgeConfirmButton != null)
        {
            corruptJudgeConfirmButton.onClick.AddListener(ConfirmPreview);
        }
        
        if (corruptJudgeCancelButton != null)
        {
            corruptJudgeCancelButton.onClick.AddListener(CancelPreview);
        }
        
        if (corruptJudgeShuffleButton != null)
        {
            corruptJudgeShuffleButton.onClick.AddListener(TriggerShuffle);
        }
        
        // Set up button listeners for Hitman panel
        if (hitmanConfirmButton != null)
        {
            hitmanConfirmButton.onClick.AddListener(ConfirmPreview);
        }
        
        if (hitmanCancelButton != null)
        {
            hitmanCancelButton.onClick.AddListener(CancelPreview);
        }
        
        if (hitmanShuffleButton != null)
        {
            hitmanShuffleButton.onClick.AddListener(TriggerShuffle);
        }
    }
    

public void ShowPreview(List<CardInfo> cardInfos, string title, bool canRearrange = false, bool canRemove = false, int maxRemove = 1, System.Action<List<CardInfo>> onConfirm = null, System.Action onCancel = null)
{
    if (cardInfos == null || cardInfos.Count == 0) return;

    if (cardInfos.Count == 1)
    {
        // Single Card Preview (Spy or similar)
        singlePreviewImage.sprite = cardInfos[0].cardSprite;
        singlePreviewName.text = cardInfos[0].cardName;
        previewPanel.SetActive(true);
        previewPanel.transform.localScale = Vector3.zero;
        previewPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        StartCoroutine(AutoClosePanel(previewPanel, 1.5f));
    }
    else if (title.Contains("Blind Seer"))
    {
        // Initialize Blind Seer variables
        originalCardInfos = new List<CardInfo>(cardInfos);
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;
        allowRemoving = canRemove;
        maxRemovable = maxRemove;
        
        // Show Blind Seer specific buttons
        if (blindSeerConfirmButton != null)
        {
            blindSeerConfirmButton.gameObject.SetActive(true);
        }

        if (blindSeerCancelButton != null)
        {
            blindSeerCancelButton.gameObject.SetActive(true);
        }

        if (blindSeerShuffleButton != null)
        {
            blindSeerShuffleButton.gameObject.SetActive(false);
        }
        
        blindSeerPanel.SetActive(true);

        for (int i = 0; i < blindSeerImages.Length; i++)
        {
            if (i < cardInfos.Count)
            {
                blindSeerImages[i].sprite = cardInfos[i].cardSprite;
                blindSeerImages[i].gameObject.SetActive(true);
                blindSeerNames[i].text = cardInfos[i].cardName;
                blindSeerNames[i].gameObject.SetActive(true);
            }
            else
            {
                blindSeerImages[i].gameObject.SetActive(false);
                blindSeerNames[i].gameObject.SetActive(false);
            }
        }

        blindSeerPanel.transform.localScale = Vector3.zero;
        blindSeerPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        StartCoroutine(AutoClosePanel(blindSeerPanel, 2f));
    }
    else if (title.Contains("Hitman"))
    {
        // Initialize Hitman-specific variables
        originalCardInfos = new List<CardInfo>(cardInfos);
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;
        allowRemoving = canRemove;
        maxRemovable = maxRemove;
        selectedCardIndex = -1;
        isCardSelected = false;
        
        // Show Hitman specific buttons
        if (hitmanConfirmButton != null)
        {
            hitmanConfirmButton.gameObject.SetActive(true);
            Text buttonText = hitmanConfirmButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Confirm";
            }
            hitmanConfirmButton.interactable = false; // Disabled until card is selected
        }

        if (hitmanCancelButton != null)
        {
            hitmanCancelButton.gameObject.SetActive(true);
        }

        if (hitmanShuffleButton != null)
        {
            hitmanShuffleButton.gameObject.SetActive(false);
        }
        
        playerPreviewPanel.SetActive(true);

        for (int i = 0; i < playerPreviewImages.Length; i++)
        {
            if (i < cardInfos.Count)
            {
                playerPreviewImages[i].sprite = cardInfos[i].cardSprite;
                playerPreviewImages[i].gameObject.SetActive(true);
                playerPreviewNames[i].text = cardInfos[i].cardName;
                playerPreviewNames[i].gameObject.SetActive(true);
                
                // Add click functionality for card selection
                AddHitmanCardInteraction(playerPreviewImages[i].gameObject, i);
            }
            else
            {
                playerPreviewImages[i].gameObject.SetActive(false);
                playerPreviewNames[i].gameObject.SetActive(false);
            }
        }

        playerPreviewPanel.transform.localScale = Vector3.zero;
        playerPreviewPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
    }
    else
    {
        Debug.LogWarning("Unhandled card preview case in ShowPreview");
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

        allowRearranging = false; // No rearranging for Corrupt Judge now
        allowRemoving = false;
        maxRemovable = 0;
        removedCount = 0;

        // Show Corrupt Judge specific buttons
        if (corruptJudgeConfirmButton != null)
        {
            corruptJudgeConfirmButton.gameObject.SetActive(true);
            Text buttonText = corruptJudgeConfirmButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Confirm";
            }
        }

        if (corruptJudgeCancelButton != null)
        {
            corruptJudgeCancelButton.gameObject.SetActive(true);
        }

        if (corruptJudgeShuffleButton != null)
        {
            corruptJudgeShuffleButton.gameObject.SetActive(false);
        }

        // Show Corrupt Judge panel (not multiPreview)
        corruptJudgePanel.SetActive(true);

        // Fill the card previews
        for (int i = 0; i < corruptJudgeImages.Length; i++)
        {
            if (i < cardInfos.Count)
            {
                corruptJudgeImages[i].sprite = cardInfos[i].cardSprite;
                corruptJudgeImages[i].gameObject.SetActive(true);
                corruptJudgeNames[i].text = cardInfos[i].cardName;
                corruptJudgeNames[i].gameObject.SetActive(true);
                // Add drag functionality to the first two cards only
                if (i < 2)
                {
                    AddDragInteractionToImage(corruptJudgeImages[i].gameObject, i);
                }

            }
            else
            {
                corruptJudgeImages[i].gameObject.SetActive(false);
                corruptJudgeNames[i].gameObject.SetActive(false);
            }
        }

        // Optional animation
        corruptJudgePanel.transform.localScale = Vector3.zero;
        corruptJudgePanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        StartCoroutine(AutoClosePanel(corruptJudgePanel, 10f));

    }
    public void ShowBlindSeerPreview(List<CardInfo> cardInfos)
    {
        if (cardInfos == null || cardInfos.Count == 0) return;

        // Show the Blind Seer preview panel
        blindSeerPanel.SetActive(true);

        for (int i = 0; i < blindSeerImages.Length; i++)
        {
            if (i < cardInfos.Count)
            {
                blindSeerImages[i].sprite = cardInfos[i].cardSprite;
                blindSeerImages[i].gameObject.SetActive(true);
                blindSeerNames[i].text = cardInfos[i].cardName;
                blindSeerNames[i].gameObject.SetActive(true);
            }
            else
            {
                blindSeerImages[i].gameObject.SetActive(false);
                blindSeerNames[i].gameObject.SetActive(false);
            }
        }

        blindSeerPanel.transform.localScale = Vector3.zero;
        blindSeerPanel.transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
        StartCoroutine(AutoClosePanel(blindSeerPanel, 10f));

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
    
    /// <summary>
    /// Add click interaction for Hitman card selection
    /// </summary>
    private void AddHitmanCardInteraction(GameObject imageObj, int index)
    {
        // Add EventTrigger for click selection
        EventTrigger trigger = imageObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = imageObj.AddComponent<EventTrigger>();
        }
        
        // Clear existing triggers
        trigger.triggers.Clear();
        
        // Add click functionality for card selection
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback.AddListener((data) => { OnHitmanCardClick(imageObj, index, (PointerEventData)data); });
        trigger.triggers.Add(pointerClick);
    }
    
    private void OnCardClick(GameObject imageObj, int index, PointerEventData eventData)
    {
        if (isDragging) return;

        // Safety checks
        if (corruptJudgeImages == null || corruptJudgeImages.Length < 3)
        {
            Debug.LogWarning("Preview images not properly assigned.");
            return;
        }

        // Only allow valid indices
        if (index < 0 || index > 2) return;

        // Try swapping with the next index if it's valid
        int nextIndex = (index + 1) % 2;
        if (corruptJudgeImages[nextIndex].gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateImageSwap(index, nextIndex));
        }
    }
    
    /// <summary>
    /// Handle card click for Hitman card selection
    /// </summary>
    private void OnHitmanCardClick(GameObject imageObj, int index, PointerEventData eventData)
    {
        // Safety checks
        if (playerPreviewImages == null || playerPreviewImages.Length < 3)
        {
            Debug.LogWarning("Player preview images not properly assigned.");
            return;
        }

        // Only allow valid indices
        if (index < 0 || index >= playerPreviewImages.Length) return;
        
        // If clicking the same card, deselect it
        if (selectedCardIndex == index)
        {
            DeselectCard();
            return;
        }
        
        // Deselect previous card if any
        if (selectedCardIndex != -1)
        {
            DeselectCard();
        }
        
        // Select new card
        SelectCard(index);
    }
    
    /// <summary>
    /// Select a card with visual feedback and animation
    /// </summary>
    private void SelectCard(int index)
    {
        selectedCardIndex = index;
        isCardSelected = true;
        
        GameObject cardObj = playerPreviewImages[index].gameObject;
        
        // Animate card pop-up effect
        Vector3 originalScale = cardObj.transform.localScale;
        Vector3 originalPosition = cardObj.transform.localPosition;
        
        // Pop up animation
        cardObj.transform.DOScale(originalScale * 1.2f, 0.3f).SetEase(Ease.OutBack);
        cardObj.transform.DOLocalMoveY(originalPosition.y + 30f, 0.3f).SetEase(Ease.OutQuad);
        
        // Add visual feedback - glow effect
        Image cardImage = cardObj.GetComponent<Image>();
        if (cardImage != null)
        {
            // Add a subtle glow by changing color
            cardImage.color = new Color(1.2f, 1.2f, 1.0f, 1f); // Slightly yellow tint
        }
        
        // Enable confirm button
        if (hitmanConfirmButton != null)
        {
            hitmanConfirmButton.interactable = true;
            Text buttonText = hitmanConfirmButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Remove {originalCardInfos[index].cardName}";
            }
        }
        
        Debug.Log($"Selected card: {originalCardInfos[index].cardName}");
    }
    
    /// <summary>
    /// Deselect the currently selected card
    /// </summary>
    private void DeselectCard()
    {
        if (selectedCardIndex == -1) return;
        
        GameObject cardObj = playerPreviewImages[selectedCardIndex].gameObject;
        
        // Animate card back to original position
        cardObj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad);
        cardObj.transform.DOLocalMoveY(0f, 0.3f).SetEase(Ease.OutQuad);
        
        // Reset visual feedback
        Image cardImage = cardObj.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.color = Color.white;
        }
        
        // Disable confirm button
        if (hitmanConfirmButton != null)
        {
            hitmanConfirmButton.interactable = false;
            Text buttonText = hitmanConfirmButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Confirm";
            }
        }
        
        selectedCardIndex = -1;
        isCardSelected = false;
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
            playerPreviewPanel.transform as RectTransform, 
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
            playerPreviewPanel.transform as RectTransform, 
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
            if (corruptJudgeImages[i].gameObject == draggedCard) continue;
            
            Image cardImage = corruptJudgeImages[i].GetComponent<Image>();
            if (cardImage != null)
            {
                // Check if this card is close enough to be a swap target
                float distance = Vector3.Distance(draggedCard.transform.localPosition, corruptJudgeImages[i].transform.localPosition);
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
        for (int i = 0; i < corruptJudgeImages.Length; i++)
        {
            Image cardImage = corruptJudgeImages[i].GetComponent<Image>();
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
            if (corruptJudgeImages[i].gameObject == draggedCard) continue;
            
            float distance = Vector3.Distance(position, corruptJudgeImages[i].transform.localPosition);
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
        if (index1 < 0 || index1 >= corruptJudgeImages.Length || index2 < 0 || index2 >= corruptJudgeImages.Length)
            yield break;

        GameObject card1 = corruptJudgeImages[index1].gameObject;
        GameObject card2 = corruptJudgeImages[index2].gameObject;


        Vector3 pos1 = card1.transform.localPosition;
        Vector3 pos2 = card2.transform.localPosition;

        // Animate both cards to new positions
        card1.transform.DOLocalMove(pos2, swapAnimationDuration).SetEase(Ease.OutQuad);
        card2.transform.DOLocalMove(pos1, swapAnimationDuration).SetEase(Ease.OutQuad);
        

        yield return new WaitForSeconds(swapAnimationDuration);

        // Swap data
        CardInfo tempInfo = originalCardInfos[index1];
        originalCardInfos[index1] = originalCardInfos[index2];
        originalCardInfos[index2] = tempInfo;

        // Update all 3 sprites and names after any swap
        for (int i = 0; i < 3; i++)
        {
            corruptJudgeImages[i].sprite = originalCardInfos[i].cardSprite;
            corruptJudgeNames[i].text = originalCardInfos[i].cardName;
        }

        // Reposition the cards properly
        RepositionCardsAfterSwap();
    }
    
    private void RepositionCardsAfterSwap()
    {
        // Calculate proper positions for all cards
        float totalWidth = (corruptJudgeImages.Length - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < corruptJudgeImages.Length; i++)
        {
            if (corruptJudgeImages[i].gameObject.activeInHierarchy)
            {
                Vector3 targetPosition = new Vector3(startX + (i * cardSpacing), 0, 0);
                corruptJudgeImages[i].transform.DOLocalMove(targetPosition, 0.3f).SetEase(Ease.OutQuad);
            }
        }
    }
    
    private void SetInitialCardPositions()
    {
        // Calculate proper positions for all cards
        float totalWidth = (corruptJudgeImages.Length - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < corruptJudgeImages.Length; i++)
        {
            if (corruptJudgeImages[i].gameObject.activeInHierarchy)
            {
                Vector3 targetPosition = new Vector3(startX + (i * cardSpacing), 0, 0);
                corruptJudgeImages[i].transform.localPosition = targetPosition;
            }
        }
    }

    public void ShowPlayerPreview(List<CardInfo> cardInfos)
    {
        if (cardInfos == null || cardInfos.Count == 0) return;

        playerPreviewPanel.SetActive(true);

        for (int i = 0; i < corruptJudgeImages.Length; i++)
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
            // For Hitman card, remove the selected card
            if (isCardSelected && selectedCardIndex != -1)
            {
                // Create a list with all cards except the selected one
                for (int i = 0; i < originalCardInfos.Count; i++)
                {
                    if (i != selectedCardIndex)
                    {
                        resultCards.Add(originalCardInfos[i]);
                    }
                }
                
                Debug.Log($"Hitman: Removing card {originalCardInfos[selectedCardIndex].cardName}");
            }
            else
            {
                // No card selected, return all cards (shouldn't happen as button should be disabled)
                resultCards = new List<CardInfo>(originalCardInfos);
                Debug.LogWarning("Hitman: No card selected for removal");
            }
        }
        else
        {
            resultCards = new List<CardInfo>(originalCardInfos);
        }
        
        onConfirmCallback?.Invoke(resultCards);
        
        // Hide the appropriate panel based on what's currently active
        if (playerPreviewPanel != null && playerPreviewPanel.activeInHierarchy)
        {
            HideMultiPreviewPanel();
        }
        else if (blindSeerPanel != null && blindSeerPanel.activeInHierarchy)
        {
            HideBlindSeerPanel();
        }
        else if (corruptJudgePanel != null && corruptJudgePanel.activeInHierarchy)
        {
            HideCorruptJudgePanel();
        }
        else
        {
            HidePreview();
        }
    }
    
    private void HideMultiPreviewPanel()
    {
        if (playerPreviewPanel != null)
        {
            playerPreviewPanel.transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
                .OnComplete(() => {
                    playerPreviewPanel.SetActive(false);
                    // Hide Hitman buttons
                    if (hitmanConfirmButton != null) hitmanConfirmButton.gameObject.SetActive(false);
                    if (hitmanCancelButton != null) hitmanCancelButton.gameObject.SetActive(false);
                    if (hitmanShuffleButton != null) hitmanShuffleButton.gameObject.SetActive(false);
                    // Reset Hitman card selection state
                    selectedCardIndex = -1;
                    isCardSelected = false;
                });
        }
    }
    
    private void HideBlindSeerPanel()
    {
        if (blindSeerPanel != null)
        {
            blindSeerPanel.transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
                .OnComplete(() => {
                    blindSeerPanel.SetActive(false);
                    // Hide Blind Seer buttons
                    if (blindSeerConfirmButton != null) blindSeerConfirmButton.gameObject.SetActive(false);
                    if (blindSeerCancelButton != null) blindSeerCancelButton.gameObject.SetActive(false);
                    if (blindSeerShuffleButton != null) blindSeerShuffleButton.gameObject.SetActive(false);
                });
        }
    }
    
    private void HideCorruptJudgePanel()
    {
        if (corruptJudgePanel != null)
        {
            corruptJudgePanel.transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
                .OnComplete(() => {
                    corruptJudgePanel.SetActive(false);
                    // Hide Corrupt Judge buttons
                    if (corruptJudgeConfirmButton != null) corruptJudgeConfirmButton.gameObject.SetActive(false);
                    if (corruptJudgeCancelButton != null) corruptJudgeCancelButton.gameObject.SetActive(false);
                    if (corruptJudgeShuffleButton != null) corruptJudgeShuffleButton.gameObject.SetActive(false);
                });
        }
    }
    
    private void CancelPreview()
    {
        onCancelCallback?.Invoke();
        
        // Hide the appropriate panel based on what's currently active
        if (playerPreviewPanel != null && playerPreviewPanel.activeInHierarchy)
        {
            HideMultiPreviewPanel();
        }
        else if (blindSeerPanel != null && blindSeerPanel.activeInHierarchy)
        {
            HideBlindSeerPanel();
        }
        else if (corruptJudgePanel != null && corruptJudgePanel.activeInHierarchy)
        {
            HideCorruptJudgePanel();
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