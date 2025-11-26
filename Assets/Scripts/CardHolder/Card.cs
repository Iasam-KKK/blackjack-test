using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas;
    private Image imageComponent;
    [SerializeField] private bool instantiateVisual = true;
    // private VisualCardsHandler visualHandler; // TODO: Implement VisualCardsHandler class
    private Vector3 offset;

    [Header("Card Data")]
    public TarotCardData cardData;

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50;

    [Header("Selection")]
    public bool selected;
    public float selectionOffset = 50;
    private float pointerDownTime;
    private float pointerUpTime;
    private float lastClickTime;
    private const float doubleClickTime = 0.3f;

    [Header("Visual")]
    [SerializeField] private GameObject cardVisualPrefab;
    // [HideInInspector] public CardVisual cardVisual; // TODO: Implement CardVisual class

    [Header("States")]
    public bool isHovering;
    public bool isDragging;
    [HideInInspector] public bool wasDragged;

    [Header("Events")]
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();

        // TODO: Visual card system disabled - missing VisualCardsHandler and CardVisual classes
        /*
        if (!instantiateVisual)
            return;

        visualHandler = FindObjectOfType<VisualCardsHandler>();
        if (cardVisualPrefab != null)
        {
            cardVisual = Instantiate(cardVisualPrefab, visualHandler ? visualHandler.transform : canvas.transform).GetComponent<CardVisual>();
            cardVisual.Initialize(this);
        }
        */
        
        // Update card image if we have card data
        UpdateCardVisual();
    }

    public void UpdateCardVisual()
    {
        if (cardData != null && imageComponent != null && cardData.cardImage != null)
        {
            imageComponent.sprite = cardData.cardImage;
        }
        
        // TODO: Visual card system disabled
        /*
        if (cardVisual != null)
        {
            cardVisual.UpdateCardImage(cardData);
        }
        */
    }

    void Update()
    {
        ClampPosition();

        if (isDragging)
        {
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    void ClampPosition()
    {
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragEvent.Invoke(this);
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        imageComponent.raycastTarget = false;

        wasDragged = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent.Invoke(this);
        isDragging = false;
        canvas.GetComponent<GraphicRaycaster>().enabled = true;
        imageComponent.raycastTarget = true;

        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(this);
        isHovering = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent.Invoke(this);
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time;

        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

        if (pointerUpTime - pointerDownTime > .2f)
            return;

        if (wasDragged)
            return;

        // Check for double-click
        if (Time.time - lastClickTime < doubleClickTime)
        {
            // Double-click detected - use the card
            OnDoubleClick();
            lastClickTime = 0f; // Reset to prevent triple-click
        }
        else
        {
            // Single click - select/deselect
            selected = !selected;
            SelectEvent.Invoke(this, selected);

            if (selected)
                transform.localPosition += (transform.up * selectionOffset); // TODO: Use cardVisual.transform.up when CardVisual is implemented
            else
                transform.localPosition = Vector3.zero;

            lastClickTime = Time.time;
        }
    }

    private void OnDoubleClick()
    {
        Debug.Log($"Double-clicked card: {(cardData != null ? cardData.cardName : "Unknown")}");
        
        // TODO: CardUsageHelper not implemented yet
        /*
        // Try to use the card through the CardUsageHelper
        if (cardData != null)
        {
            CardUsageHelper.UseCard(cardData);
        }
        */
        
        // Fallback: Just log for now
        if (cardData != null)
        {
            Debug.Log($"Card double-clicked: {cardData.cardName}");
        }
    }

    public void Deselect()
    {
        if (selected)
        {
            selected = false;
            transform.localPosition = Vector3.zero;
        }
    }

    public int SiblingAmount()
    {
        if (transform.parent == null) return 0;
        bool isInSlot = transform.parent.CompareTag("Slot");
        return isInSlot && transform.parent.parent != null ? transform.parent.parent.childCount - 1 : 0;
    }

    public int ParentIndex()
    {
        if (transform.parent == null) return 0;
        bool isInSlot = transform.parent.CompareTag("Slot");
        return isInSlot ? transform.parent.GetSiblingIndex() : 0;
    }

    public float NormalizedPosition()
    {
        if (transform.parent == null) return 0;
        bool isInSlot = transform.parent.CompareTag("Slot");
        if (!isInSlot || transform.parent.parent == null) return 0;
        
        int parentIndex = ParentIndex();
        int totalSlots = transform.parent.parent.childCount - 1;
        
        // TODO: ExtensionMethods.Remap not implemented - using simple calculation
        return totalSlots > 0 ? (float)parentIndex / (float)totalSlots : 0;
    }

    private void OnDestroy()
    {
        // TODO: Visual card system disabled
        /*
        if (cardVisual != null)
            Destroy(cardVisual.gameObject);
        */
    }
}

