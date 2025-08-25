using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    public Transform shopPanel; // Parent object for shop tarot cards
    public Transform tarotPanel; // Parent object for owned tarot cards
    public GameObject tarotCardPrefab; // Prefab for tarot card UI element
    public Deck deck; // Reference to the game deck for balance
    
    [Header("Shop Settings")]
    public List<TarotCardData> availableTarotCards = new List<TarotCardData>();
    public bool setupOnStart = true;
    
    public List<Transform> shopSlots = new List<Transform>();
    
    [Header("Tarot Panel Settings")]
    public List<Transform> tarotSlots = new List<Transform>();
    
    [Header("Streak Reward Settings")]
    public GameObject streakRewardNotification; // Notification panel for streak rewards
    public float notificationDuration = 3f; // Duration to show notification

    private void Start()
    {
        
        if (setupOnStart)
        {
            SetupShop();
        }
        
        // Hide notification if it exists
        if (streakRewardNotification != null)
        {
            streakRewardNotification.SetActive(false);
        }
    }
    

    
    // Method to give a random tarot card as streak reward
    public void GiveRandomTarotCard()
    {
        if (availableTarotCards.Count == 0)
        {
            Debug.LogWarning("No tarot cards available to give as reward!");
            return;
        }
        
        // Get a random card from the available cards
        int randomIndex = Random.Range(0, availableTarotCards.Count);
        TarotCardData cardData = availableTarotCards[randomIndex];
        
        GiveSpecificTarotCard(cardData);
    }
    
    // Method to give a specific tarot card as reward
    public void GiveSpecificTarotCard(TarotCardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("Cannot give null tarot card!");
            return;
        }
        
        // Check if there's an empty slot in the tarot panel
        Transform emptySlot = GetEmptyTarotSlot();
        if (emptySlot == null)
        {
            Debug.LogWarning("No empty slots available for free tarot card!");
            return;
        }
        
        // Create new card instance
        GameObject cardObject = Instantiate(tarotCardPrefab, emptySlot);
        TarotCard card = cardObject.GetComponent<TarotCard>();
        if (card != null)
        {
            card.cardData = cardData;
            card.isInShop = false;
            card.deck = deck;
            card.transform.localPosition = Vector3.zero;
            card.transform.localScale = Vector3.one * 0.8f;
            
            // Set up the card's RectTransform
            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(100, 150); // Smaller size
            }
            
            // Add to PlayerStats
            if (PlayerStats.instance != null)
            {
                PlayerStats.instance.ownedCards.Add(cardData);
                Debug.Log("Added " + cardData.cardName + " to player's owned cards via reward");
            }
            
            // Notify the player
            ShowRewardNotification("Boss Reward: " + cardData.cardName);
        }
    }
    
    // Method to show a reward notification
    private void ShowRewardNotification(string message)
    {
        if (streakRewardNotification != null)
        {
            // Set the notification text
            Text notificationText = streakRewardNotification.GetComponentInChildren<Text>();
            if (notificationText != null)
            {
                notificationText.text = message;
            }
            
            // Show the notification
            streakRewardNotification.SetActive(true);
            
            // Hide it after a delay
            CancelInvoke("HideRewardNotification");
            Invoke("HideRewardNotification", notificationDuration);
        }
    }
    
    // Method to hide the reward notification
    private void HideRewardNotification()
    {
        if (streakRewardNotification != null)
        {
            streakRewardNotification.SetActive(false);
        }
    }
    

    
    /*
    public void SetupShop()
    {
        // Clear any existing cards
        foreach (Transform child in shopPanel)
        {
            if (child.GetComponent<TarotCard>() != null)
                Destroy(child.gameObject);
        }
        
        // Create cards for each available tarot card, placing them in slots
        for (int i = 0; i < Mathf.Min(availableTarotCards.Count, shopSlots.Count); i++)
        {
            GameObject cardObject = Instantiate(tarotCardPrefab, shopSlots[i]);
            cardObject.transform.localPosition = Vector3.zero; // Center in slot
            
            TarotCard card = cardObject.GetComponent<TarotCard>();
            if (card != null)
            {
                card.cardData = availableTarotCards[i];
                card.isInShop = true;
                card.deck = deck;
            }
        }
        
        FixCardPositioning();
    }
    */
    /*public void SetupShop()
    {
        // 🔹 Check if tarot cards are disabled for this boss
        BossManager bossManager = FindObjectOfType<BossManager>();
        if (bossManager != null && bossManager.GetCurrentBoss() != null)
        {
            if (!bossManager.GetCurrentBoss().allowTarotCards)
            {
                Debug.Log("Tarot cards disabled for this boss: " + bossManager.GetCurrentBoss().bossName);
                return; // 🚫 Stop before spawning any tarot cards
            }
        }

        // Clear any existing cards
        foreach (Transform child in shopPanel)
        {
            if (child.GetComponent<TarotCard>() != null)
                Destroy(child.gameObject);
        }
    
        // Create cards for each available tarot card, placing them in slots
        for (int i = 0; i < Mathf.Min(availableTarotCards.Count, shopSlots.Count); i++)
        {
            GameObject cardObject = Instantiate(tarotCardPrefab, shopSlots[i]);
            cardObject.transform.localPosition = Vector3.zero; // Center in slot
        
            TarotCard card = cardObject.GetComponent<TarotCard>();
            if (card != null)
            {
                card.cardData = availableTarotCards[i];
                card.isInShop = true;
                card.deck = deck;
            }
        }
    
        FixCardPositioning();
    }*/
    private bool isPositioningCards = false;
    private bool isSettingUpShop = false;

    public void SetupShop()
    { 
        if (isSettingUpShop) return; // Prevent multiple calls
        
        isSettingUpShop = true;
        
        BossManager bossManager = FindObjectOfType<BossManager>();
        if (bossManager != null && bossManager.GetCurrentBoss() != null)
        {
            if (!bossManager.GetCurrentBoss().allowTarotCards)
            {
                Debug.Log("Tarot cards disabled for this boss: " + bossManager.GetCurrentBoss().bossName);
                isSettingUpShop = false;
                return; 
            }
        }

        // Clear any existing cards
        foreach (Transform child in shopPanel)
        {
            if (child.GetComponent<TarotCard>() != null)
                Destroy(child.gameObject);
        }

        // ✅ Limit to only 2 cards
        int maxCardsToShow = 2;

        // Create cards for each available tarot card, placing them in slots
        for (int i = 0; i < Mathf.Min(maxCardsToShow, availableTarotCards.Count); i++)
        {
            GameObject cardObject = Instantiate(tarotCardPrefab, shopSlots[i]);
            cardObject.transform.localPosition = Vector3.zero; // Center in slot
    
            TarotCard card = cardObject.GetComponent<TarotCard>();
            if (card != null)
            {
                card.cardData = availableTarotCards[i];
                card.isInShop = true;
                card.deck = deck;
            }
        }

        FixCardPositioning();
        
        isSettingUpShop = false;
    }


    private void FixCardPositioning()
    {
        if (isPositioningCards) return; // Prevent infinite loop
        
        isPositioningCards = true;
        
        // Directly position all cards in the shop panel
        TarotCard[] shopCards = shopPanel.GetComponentsInChildren<TarotCard>();
        
        for (int i = 0; i < shopCards.Length; i++)
        {
            // Force card to proper position
            RectTransform cardRect = shopCards[i].GetComponent<RectTransform>();
            if (cardRect != null)
            {
                // Reset transforms completely
                cardRect.localPosition = new Vector3(150 * i, 0, 0); // Space horizontally
                cardRect.localRotation = Quaternion.identity;
                cardRect.localScale = Vector3.one;
                
                // Fix anchoring
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                
                // Set fixed size
                cardRect.sizeDelta = new Vector2(120, 180);
            }
        }
        
        isPositioningCards = false;
    }
    
    // Called by TarotCard when purchased
    public void OnCardPurchased(TarotCard card)
    {
        // Card will handle moving itself to the tarot panel
        Debug.Log("Card purchased: " + card.cardData.cardName);
    }
    
    // Reset all owned tarot cards for a new round
    public void ResetTarotCardsForNewRound()
    {
        TarotCard[] tarotCards = tarotPanel.GetComponentsInChildren<TarotCard>();
        foreach (TarotCard card in tarotCards)
        {
            card.ResetForNewRound();
        }
    }
    
    public Transform GetEmptyTarotSlot()
    {
        foreach (Transform slot in tarotSlots)
        {
            // If slot has no card children, it's available
            if (slot.childCount == 0)
            {
                return slot;
            }
        }
        return null;
    }
    
    public void AddCardToTarotPanel(TarotCard card)
    {
        Transform emptySlot = GetEmptyTarotSlot();
        
        if (emptySlot != null)
        {
            card.transform.SetParent(emptySlot, false);
            card.transform.localPosition = Vector3.zero;
            card.transform.localScale = Vector3.one * 0.8f; // Smaller scale
            
            // Adjust RectTransform
            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(100, 150); // Smaller size
            }
        }
        else
        {
            Debug.LogWarning("No empty slots available in tarot panel!");
        }
    }
} 