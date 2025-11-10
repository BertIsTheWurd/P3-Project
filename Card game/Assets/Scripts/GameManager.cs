using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<Card> playerDeck = new List<Card>();
    public List<Card> playerHand = new List<Card>();
    public Transform[][] cardSlots;
    public bool[][] availableCardSlots;

    public int HandSize = 6;
    
    public void DrawToMax()
    {
        while (playerHand.Count < HandSize)
        {
            
        }
    }

    public void PlayCard()
    {
        
    }
}
