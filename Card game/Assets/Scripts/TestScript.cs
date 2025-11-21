using UnityEngine;

public class TestScript : MonoBehaviour
{
    public KeyCode drawToMax = KeyCode.D;
    public KeyCode playCard= KeyCode.P;
    private GameManager gameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager =  GameObject.Find("Game Manager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        //This is abhorrent, do not do this for actual scripts
        if (Input.GetKeyDown(drawToMax))
        {
            gameManager.DrawToMax();
        }

        if (Input.GetKeyDown(playCard))
        {
            gameManager.PlayCard();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            gameManager.DebugCubes();
        }
    }
}
