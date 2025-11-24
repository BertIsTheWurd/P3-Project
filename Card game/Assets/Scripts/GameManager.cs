using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private CardPool cardPool;
    private DiscardPile discardPile;
    
    public int gridX;
    public int gridZ;
    public Transform gridStart, gridEnd;
    
    public List<Card> playerDeck = new List<Card>();
    public List<Card> playerHand = new List<Card>();
    public Vector3[][] cardSlots;
    public Card[][] playedCards;
    public bool[][] availableCardSlots;
    
    public int HandSize = 6;

    //Debug Stuff
    public GameObject TestingCube;

    public void Start()
    {
        cardPool = GameObject.Find("Card Pool").GetComponent<CardPool>();
        discardPile = GameObject.Find("Discard Pile").GetComponent<DiscardPile>();
        
        cardSlots = new Vector3[gridZ][];
        playedCards = new Card[gridZ][];
        for (int i = 0; i < cardSlots.Length; i++)
        {
            cardSlots[i] = new Vector3[gridX];
            playedCards[i] = new Card[gridX];
        }
        
        float gridSizeX = gridEnd.position.x - gridStart.position.x;
        float gridSizeZ = gridEnd.position.z - gridStart.position.z;

        float cardSpaceX = gridSizeX / gridX;
        float cardSpaceZ = gridSizeZ / gridZ;
        
        //Distributes Spaces based on grid size & gridStart/End
        for (int i = 0; i < cardSlots.Length; i++)
        {
            for (int j = 0; j < cardSlots[i].Length; j++)
            {
                cardSlots[i][j] = new Vector3(gridStart.position.x + cardSpaceX * j, gridStart.position.y, gridStart.position.z + cardSpaceZ * i);
            }
        }
    }

    public void DebugCubes()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            for (int j = 0; j < cardSlots[i].Length; j++)
            {
                Instantiate(TestingCube, cardSlots[i][j], Quaternion.identity);
            }
        }
    }
    
    public GameObject DrawToMax()
    {
        Debug.Log("Drawing to max");
        return null;
    }
    
    //Remember to remove the card from any other list/stack it might be a part of, when calling this
    public void DiscardCard(GameObject card)
    {
        discardPile.discardPile.Add(card);
    }
    
    public void PlayCard(Card cardInfo, int gridSpotX, int gridSpotZ)
    {
        Debug.Log("Playing card");
        GameObject card = cardPool.ReturnCard();
        
        Card cardData = card.GetComponent<Card>();
        cardInfo.CopyTo(cardData);
        
        card.transform.position = cardSlots[gridSpotZ][gridSpotX];
        playedCards[gridSpotZ][gridSpotX] = cardData;
        card.SetActive(true);
    }
}
