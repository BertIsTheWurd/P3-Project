using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridX = 5; // rows (height) - set to 5 in inspector
    public int gridZ = 7; // columns (width) - set to 7 in inspector
    public GameObject gridSlotPrefab;
    public Sprite gridSlotSprite;
    public Transform gridCenter; // Optional center point

    [Header("Card Data")]
    public DirectionalCardData startCardData;
    public DirectionalCardData correctEndCardData;
    public DirectionalCardData wrongEndCardData;

    [Header("References")]
    public CardPool cardPool;
    public DrawPile drawPile;
    public DiscardPile discardPile;
    public CardsInHandController handController;
    public GameObject[,] playedCards;
    public Vector3[,] cardSlots;
    private GameObject[,] gridSlotObjects;

    [Header("Player Settings")]
    public int HandSize = 6;

    [Header("UDP Listener")]
    private UdpClient udpClient;
    public int port = 5005;
    public bool lookingAway = false;

    private int correctEndRow = -1;

    private void Start()
    {
        playedCards = new GameObject[gridX, gridZ];
        cardSlots = new Vector3[gridX, gridZ];
        gridSlotObjects = new GameObject[gridX, gridZ];

        CreateGridSlots();
        PlaceStartAndEndCards();
        StartCoroutine(InitializeGameAfterCardPool());
    }

    private System.Collections.IEnumerator InitializeGameAfterCardPool()
    {
        yield return null;
        drawPile.Initialize(cardPool.cards);
        DrawUntilFullHand();

        udpClient = new UdpClient(port);
        udpClient.BeginReceive(ReceiveCallback, null);
        Debug.Log("UDP Listener started on port " + port);
    }

    private void CreateGridSlots()
    {
        float slotWidth = gridSlotSprite.bounds.size.x * gridSlotPrefab.transform.localScale.x;
        float slotHeight = gridSlotSprite.bounds.size.y * gridSlotPrefab.transform.localScale.y;

        // gridX=5 (rows), gridZ=7 (columns)
        // We want 7 wide (left-right), 5 tall (top-bottom)
        float offsetX = (gridZ - 1) * slotWidth / 2f;   // gridZ=7 for width
        float offsetZ = (gridX - 1) * slotHeight / 2f;  // gridX=5 for height

        Vector3 center = gridCenter != null ? gridCenter.position : Vector3.zero;

        for (int row = 0; row < gridX; row++) // 5 rows
        {
            for (int col = 0; col < gridZ; col++) // 7 columns
            {
                Vector3 pos = new Vector3(
                    center.x + (col * slotWidth) - offsetX,      // col controls X (left-right)
                    center.y,
                    center.z + (row * slotHeight) - offsetZ      // row controls Z (top-bottom)
                );

                var slot = Instantiate(gridSlotPrefab, pos, Quaternion.Euler(90, 0, 0));
                slot.tag = "GridSlot";

                if (slot.GetComponent<GridSlot>() == null)
                    slot.AddComponent<GridSlot>();

                var gridSlot = slot.GetComponent<GridSlot>();
                if (gridSlot != null && gridSlotSprite != null)
                    gridSlot.emptySlotSprite = gridSlotSprite;

                gridSlotObjects[row, col] = slot;
                cardSlots[row, col] = pos;
            }
        }
    }

    private void PlaceStartAndEndCards()
    {
        // Array is [row, col] where row is 0-4 (gridX=5) and col is 0-6 (gridZ=7)
        // Start: middle row (2 of 5), leftmost column (0 of 7)
        // End: top/middle/bottom rows (0,2,4 of 5), rightmost column (6 of 7)
        
        int startRow = gridX / 2;  // = 2 (middle of 5 rows)
        int startCol = 0;           // = 0 (leftmost of 7 columns)
        
        var startCardObj = cardPool.CreateSpecialCard(startCardData);
        startCardObj.transform.position = cardSlots[startRow, startCol];
        startCardObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        startCardObj.SetActive(true);
        playedCards[startRow, startCol] = startCardObj;
        gridSlotObjects[startRow, startCol]?.GetComponent<GridSlot>()?.ShowAsOccupied();
        
        Debug.Log($"Start card placed at row {startRow}, col {startCol}");

        int[] endRows = { 0, 2, 4 };  // Top, middle, bottom of 5 rows
        int endColumn = gridZ - 1;     // = 6 (rightmost of 7 columns)
        correctEndRow = endRows[UnityEngine.Random.Range(0, endRows.Length)];

        foreach (int row in endRows)
        {
            DirectionalCardData endData = (row == correctEndRow) ? correctEndCardData : wrongEndCardData;
            var endCardObj = cardPool.CreateSpecialCard(endData);
            endCardObj.transform.position = cardSlots[row, endColumn];
            endCardObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            endCardObj.SetActive(true);
            playedCards[row, endColumn] = endCardObj;
            gridSlotObjects[row, endColumn]?.GetComponent<GridSlot>()?.ShowAsOccupied();
            
            Debug.Log($"End card placed at row {row}, col {endColumn}");
        }

        Debug.Log($"Correct end is at row {correctEndRow}");
    }

    public void DrawUntilFullHand()
    {
        while (handController.HandCount < HandSize)
        {
            var card = drawPile.DrawCard();
            if (card == null)
            {
                Debug.Log("No more cards to draw!");
                break;
            }
            handController.AddCardToHand(card);
        }
    }

    public void DiscardCard(GameObject card)
    {
        discardPile.AddToDiscard(card);
    }

    public bool CanPlaceCard(int x, int z, DirectionalCardData newCard)
    {
        // x is col (0-6), z is row (0-4)
        if (playedCards[z, x] != null) return false;
        
        // Start card: middle row (gridX/2), first column (0)
        // End cards: last column (gridZ-1), rows 0, 2, 4
        if ((z == gridX / 2 && x == 0) ||
            (x == gridZ - 1 && (z == 0 || z == 2 || z == 4))) return false;

        bool hasAdjacentCard = false;

        // z is row, x is col
        if (z > 0 && playedCards[z - 1, x] != null)
        {
            hasAdjacentCard = true;
            var neighbor = playedCards[z - 1, x].GetComponent<Card>();
            if (neighbor.ConnectsDown != newCard.connectsUp) return false;
        }
        if (z < gridX - 1 && playedCards[z + 1, x] != null)
        {
            hasAdjacentCard = true;
            var neighbor = playedCards[z + 1, x].GetComponent<Card>();
            if (neighbor.ConnectsUp != newCard.connectsDown) return false;
        }
        if (x > 0 && playedCards[z, x - 1] != null)
        {
            hasAdjacentCard = true;
            var neighbor = playedCards[z, x - 1].GetComponent<Card>();
            if (neighbor.ConnectsRight != newCard.connectsLeft) return false;
        }
        if (x < gridZ - 1 && playedCards[z, x + 1] != null)
        {
            hasAdjacentCard = true;
            var neighbor = playedCards[z, x + 1].GetComponent<Card>();
            if (neighbor.ConnectsLeft != newCard.connectsRight) return false;
        }

        return hasAdjacentCard;
    }

    public void PlayCard(GameObject cardObj, int x, int z)
    {
        var card = cardObj.GetComponent<Card>();
        if (!CanPlaceCard(x, z, card.cardData))
        {
            Debug.Log("Invalid placement: path does not connect or no adjacent cards.");
            return;
        }

        handController.RemoveCardFromHand(cardObj);
        cardObj.transform.SetParent(null);
        cardObj.transform.position = cardSlots[z, x];
        cardObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        cardObj.transform.localScale = Vector3.one;
        cardObj.tag = "PlayedCard";
        cardObj.SetActive(true);
        playedCards[z, x] = cardObj;
        gridSlotObjects[z, x]?.GetComponent<GridSlot>()?.ShowAsOccupied();

        DrawUntilFullHand();

        var pathResult = ValidatePath();
        if (pathResult.isComplete)
        {
            if (pathResult.isCorrect)
            {
                Debug.Log("🎉 Path complete! You found the CORRECT exit!");
                OnVictory();
            }
            else
            {
                Debug.Log("❌ Path complete, but this is the WRONG exit!");
                OnWrongExit();
            }
        }
    }

    private (bool isComplete, bool isCorrect) ValidatePath()
    {
        int startZ = gridX / 2;  // middle row
        int startX = 0;           // first column
        if (playedCards[startZ, startX] == null) return (false, false);

        HashSet<(int z, int x)> visited = new HashSet<(int, int)>();
        Queue<(int z, int x)> queue = new Queue<(int, int)>();
        queue.Enqueue((startZ, startX));

        while (queue.Count > 0)
        {
            var (z, x) = queue.Dequeue();
            if (visited.Contains((z, x))) continue;
            visited.Add((z, x));

            var currentCard = playedCards[z, x].GetComponent<Card>();
            if (x == gridZ - 1 && currentCard.cardData.isEnd)  // last column
            {
                bool isCorrect = (z == correctEndRow);
                return (true, isCorrect);
            }

            if (currentCard.ConnectsUp && z > 0 && playedCards[z - 1, x] != null)
            {
                var neighbor = playedCards[z - 1, x].GetComponent<Card>();
                if (neighbor.ConnectsDown) queue.Enqueue((z - 1, x));
            }
            if (currentCard.ConnectsDown && z < gridX - 1 && playedCards[z + 1, x] != null)
            {
                var neighbor = playedCards[z + 1, x].GetComponent<Card>();
                if (neighbor.ConnectsUp) queue.Enqueue((z + 1, x));
            }
            if (currentCard.ConnectsLeft && x > 0 && playedCards[z, x - 1] != null)
            {
                var neighbor = playedCards[z, x - 1].GetComponent<Card>();
                if (neighbor.ConnectsRight) queue.Enqueue((z, x - 1));
            }
            if (currentCard.ConnectsRight && x < gridZ - 1 && playedCards[z, x + 1] != null)
            {
                var neighbor = playedCards[z, x + 1].GetComponent<Card>();
                if (neighbor.ConnectsLeft) queue.Enqueue((z, x + 1));
            }
        }

        return (false, false);
    }

    private void OnVictory() => enabled = false;
    private void OnWrongExit() { }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
            byte[] bytes = udpClient.EndReceive(ar, ref ip);
            string message = Encoding.UTF8.GetString(bytes);
            if (message == "LOOKING_AWAY")
            {
                lookingAway = true;
                Debug.Log("👁️ lookingAway = TRUE");
            }
            else if (message == "LOOKING")
            {
                lookingAway = false;
                Debug.Log("👁️ lookingAway = FALSE");
            }
            udpClient.BeginReceive(ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP Error: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}