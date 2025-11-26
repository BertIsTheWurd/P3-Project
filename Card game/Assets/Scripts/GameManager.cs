using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
    
    public List<GameObject> playerDeck = new List<GameObject>();
    public List<GameObject> playerHand = new List<GameObject>();
    public Vector3[][] cardSlots;
    public GameObject[][] playedCards;
    public bool[][] availableCardSlots;
    
    public int HandSize = 6;
    
    //Python Listener stuff
    private UdpClient udpClient;
    public int port = 5005; // Must match sender
    public bool lookingAway = false;


    //Debug Stuff
    public GameObject TestingCube;

    public void Start()
    {
        cardPool = GameObject.Find("Card Pool").GetComponent<CardPool>();
        discardPile = GameObject.Find("Discard Pile").GetComponent<DiscardPile>();
        
        cardSlots = new Vector3[gridZ][];
        playedCards = new GameObject[gridZ][];
        for (int i = 0; i < cardSlots.Length; i++)
        {
            cardSlots[i] = new Vector3[gridX];
            playedCards[i] = new GameObject[gridX];
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
        udpClient = new UdpClient(port);
        udpClient.BeginReceive(ReceiveCallback, null);
        Debug.Log("UDP Listener started on port " + port);
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
    
    //This currently only adds it to the DiscardPile list. Doesn't physically move it anywhere
    //Remember to remove the card from any other list/stack it might be a part of, when calling this
    public void DiscardCard(GameObject card)
    {
        discardPile.discardPile.Add(card);
        card.transform.position = discardPile.gameObject.transform.position;
    }
    
    public void PlayCard(GameObject template, int gridSpotX, int gridSpotZ)
    {
        Card cardInfo = template.GetComponent<Card>();
        Debug.Log("Playing card");
        GameObject card = cardPool.ReturnCard();
        
        Card cardData = card.GetComponent<Card>();
        cardInfo.CopyTo(cardData);
        
        card.transform.position = cardSlots[gridSpotZ][gridSpotX];
        playedCards[gridSpotZ][gridSpotX] = card;
        card.SetActive(true);
    }
    //Python listener bool
    private void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
        byte[] bytes = udpClient.EndReceive(ar, ref ip);
        string message = Encoding.UTF8.GetString(bytes);

        if (message == "LOOKING_AWAY")
        {
            lookingAway = true;
            Debug.Log("lookingAway = TRUE");
        }
        else if (message == "LOOKING")
        {
            lookingAway = false;
            Debug.Log("lookingAway = FALSE");
        }

        // Keep listening
        udpClient.BeginReceive(ReceiveCallback, null);
    }
    
    void OnApplicationQuit()
    {
        udpClient.Close();
    }
}
