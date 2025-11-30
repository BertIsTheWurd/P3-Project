
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridX = 7; // columns (width)
    public int gridZ = 5; // rows (height)
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
        playedCards = new GameObject[gridZ, gridX];
        cardSlots = new Vector3[gridZ, gridX];
        gridSlotObjects = new GameObject[gridZ, gridX];

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

    /// <summary>
    /// Creates a centered grid based on sprite size.
    /// </summary>
    private void CreateGridSlots()
    {
        float slotWidth = gridSlotSprite.bounds.size.x * gridSlotPrefab.transform.localScale.x;
        float slotHeight = gridSlotSprite.bounds.size.y * gridSlotPrefab.transform.localScale.y;

        float offsetX = (gridX - 1) * slotWidth / 2f;
        float offsetZ = (gridZ - 1) * slotHeight / 2f;

        Vector3 center = gridCenter != null ? gridCenter.position : Vector3.zero;

        for (int row = 0; row < gridZ; row++) // rows (Z-axis)
        {
            for (int col = 0; col < gridX; col++) // columns (X-axis)
            {
                Vector3 pos = new Vector3(
                    center.x + (col * slotWidth) - offsetX,
                    center.y,
                    center.z + (row * slotHeight) - offsetZ
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
        // Start card: middle row, first column
        var startCardObj = cardPool.CreateSpecialCard(startCardData);
        startCardObj.transform.position = cardSlots[gridZ / 2, 0];
        startCardObj.transform.rotation = Quaternion.Euler(90, 0, 0);
        startCardObj.SetActive(true);
        playedCards[gridZ / 2, 0] = startCardObj;
        gridSlotObjects[gridZ / 2, 0]?.GetComponent<GridSlot>()?.ShowAsOccupied();

        // End cards: last column, rows 1, 3, 5
        int[] endRows = { 0, 2, 4 }; // rows 1, 3, 5
        int endColumn = gridX - 1;
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
        if (playedCards[z, x] != null) return false;
        if ((z == gridZ / 2 && x == 0) ||
            (x == gridX - 1 && (z == 0 || z == 2 || z == 4))) return false;

        bool hasAdjacentCard = false;

        if (z > 0 && playedCards[z - 1, x] != null)
        {
            hasAdjacentCard = true;
            var neighbor = playedCards[z - 1, x].GetComponent<Card>();
            if (neighbor.ConnectsDown != newCard.connectsUp) return false;
        }
        if (z < gridZ - 1 && playedCards[z + 1, x] != null)
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
        if (x < gridX - 1 && playedCards[z, x + 1] != null)
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
        int startZ = gridZ / 2;
        int startX = 0;
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
            if (x == gridX - 1 && currentCard.cardData.isEnd)
            {
                bool isCorrect = (z == correctEndRow);
                return (true, isCorrect);
            }

            if (currentCard.ConnectsUp && z > 0 && playedCards[z - 1, x] != null)
            {
                var neighbor = playedCards[z - 1, x].GetComponent<Card>();
                if (neighbor.ConnectsDown) queue.Enqueue((z - 1, x));
            }
            if (currentCard.ConnectsDown && z < gridZ - 1 && playedCards[z + 1, x] != null)
            {
                var neighbor = playedCards[z + 1, x].GetComponent<Card>();
                if (neighbor.ConnectsUp) queue.Enqueue((z + 1, x));
            }
            if (currentCard.ConnectsLeft && x > 0 && playedCards[z, x - 1] != null)
            {
                var neighbor = playedCards[z, x - 1].GetComponent<Card>();
                if (neighbor.ConnectsRight) queue.Enqueue((z, x - 1));
            }
            if (currentCard.ConnectsRight && x < gridX - 1 && playedCards[z, x + 1] != null)
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
