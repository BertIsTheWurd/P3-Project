using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MikkelGameManager : MonoBehaviour
{
    public List<Card> playerDeck = new List<Card>();
    public List<Card> playerHand = new List<Card>();
    public Transform[][] cardSlots;
    public bool[][] availableCardSlots;

    public int HandSize = 6;

    public UnityEvent LookAtCardsEvent = new UnityEvent(); 
    public UnityEvent DrawCardsEvent = new UnityEvent();
    public UnityEvent ChooseCardEvent = new UnityEvent(); 
    public UnityEvent PlayCardEvent = new UnityEvent(); 

    void Start()
    {
        PlayCardEvent.AddListener(PlayCard);
    }
    
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
