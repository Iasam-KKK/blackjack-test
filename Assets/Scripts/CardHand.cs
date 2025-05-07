using System.Collections.Generic;
using UnityEngine;

public class CardHand : MonoBehaviour
{
    public List<GameObject> cards = new List<GameObject>();
    public GameObject card;
    public bool isDealer = false;
    public int points;
    private int coordY;
     
    private void Awake() => 
        DefaultState();

    public void Clear()
    {
        DefaultState();
    
        foreach (GameObject c in cards) { Destroy(c); }
        cards.Clear();
    }

    private void DefaultState()
    {
        points = 0;

        // Card placement for both the player and the dealer
        coordY = isDealer ? -1 : 3;
    }

    public void FlipFirstCard() => 
        cards[0].GetComponent<CardModel>().ToggleFace(true);
    
    public void Push(Sprite front, int value)
    {
        // Create a new card and add it to the current hand
        GameObject cardCopy = (GameObject) Instantiate(card);
        cards.Add(cardCopy);

        // Position it on the table
        float coordX = 1.4f * (float) (cards.Count - 4);
        cardCopy.transform.position = new Vector3(coordX, coordY);

        // Assign it the right cover and value
        cardCopy.GetComponent<CardModel>().cardFront = front;
        cardCopy.GetComponent<CardModel>().value = value;
        
        // Cover up the dealer's first card
        bool isCovered = (isDealer && cards.Count <= 1) ? false : true;
        cardCopy.GetComponent<CardModel>().ToggleFace(isCovered);
        
        // Compute the hand points
        int val = 0;
        int aces = 0;
        foreach (GameObject c in cards)
        {            
            if (c.GetComponent<CardModel>().value != 11) 
            {
                val += c.GetComponent<CardModel>().value;
            }
            else { aces++; }
        }

        // Consider soft aces situations
        for (int i = 0; i < aces; ++i)
        {
            val += (val + 11 <= 21) ? 11 : 1;
        }

        points = val;
    }
}