using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Gamemanager : MonoBehaviour
{
    [SerializeField] Sprite[] diceImages;
    [SerializeField] SpriteRenderer[] inPlay;
    [SerializeField] Button roll;
    private Dictionary<Transform, Vector3> ogPositions = new Dictionary<Transform, Vector3>();
    private Transform diceParent;
    private Transform roundSavedDiceParent;
    public GameObject currentSavedGroup;

    public Text turnScoreText, totalScoreText;
    
    public int turnScore, totalScore, turnNumber;
    public bool hasSaved = true, hasRolled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        diceParent = GameObject.Find("PlayableDice").transform;
        foreach (Transform dice in diceParent)
        {
            ogPositions[dice] = dice.position; // Store original position
        }
        roundSavedDiceParent = GameObject.Find("RoundSDice").transform;

        CreateNewSavedGroup();
        //throwDice();
    }

    void CreateNewSavedGroup()
    {
        turnNumber++;
        currentSavedGroup = new GameObject("SavedDice Turn" + turnNumber);
        currentSavedGroup.transform.SetParent(GameObject.Find("RoundSDice").transform);
        roll.interactable = true;
    }

    public void throwDice()
    {
        if (!hasSaved && hasRolled)
        {
            Debug.Log("You must save at least one die before rolling!");
            return;
        }

        hasRolled = true;

        // Move previous round's saved dice to the left and make them smaller
        float startX = -7; // Left side
        float startY = 3;  // Start stacking down from here
        float yOffset = -0.5f; // Space between dice

        int savedDiceCount = 0; // Track how many dice have been moved

        foreach (Transform group in GameObject.Find("RoundSDice").transform)
        {
            if (group != currentSavedGroup) // Ignore the current round's saved dice
            {
                foreach (Transform dice in group)
                {
                    dice.localScale = new Vector3(0.1f, 0.1f, 1); // Shrink size
                    dice.position = new Vector3(startX, startY + savedDiceCount * yOffset, dice.position.z);
                    savedDiceCount++;
                }
            }
        }


        CreateNewSavedGroup();
        Transform playableDiceParent = GameObject.Find("PlayableDice")?.transform;

        if (playableDiceParent == null)
        {
            Debug.LogError("PlayableDice object not found");
            return;
        }

        Debug.Log("Rolling dice in PlayableDice...");

        List<int> diceValues = new List<int>();

        foreach (Transform dice in playableDiceParent)
        {
            yOffset = (Random.Range(-5, 15) * 0.1f % 1.5f) + 1;
            dice.position = new Vector3(dice.position.x, yOffset, dice.position.z);

            int randDie = Random.Range(0, diceImages.Length);
            dice.GetComponent<SpriteRenderer>().sprite = diceImages[randDie];

            Debug.Log("Rolled: " + dice.name + " → Face: " + randDie);

            int diceValue = dice.GetComponent<Dice>().GetDiceValue();
            diceValues.Add(diceValue);
        }

        if (CalculateDiceScore(diceValues.ToArray()) > 0)
        {
            Debug.Log("Scoring dice found!");
        }
        else
        {
            Debug.Log("Farkle! No scoring dice.");
            turnScore = 0;
            Bank();
            UpdateScoreBoard();
        }

        hasSaved = false; // Reset so a die must be saved before rolling again        
    }


    public void Bank()
    {
        totalScore += turnScore;
        turnScore = 0;

        List<Transform> diceToMove = new List<Transform>();

        // Collect all dice first
        foreach (Transform group in roundSavedDiceParent)
        {
            foreach (Transform dice in group)
            {
                diceToMove.Add(dice);
            }
        }

        // Move all dice back to PlayableDice
        foreach (Transform dice in diceToMove)
        {
            dice.SetParent(diceParent, true);
            Dice diceScript = dice.GetComponent<Dice>();
            diceScript.permanentlySaved = false;
            diceScript.isSaved = false;
            if (ogPositions.ContainsKey(dice))
            {
                dice.localScale = new Vector3(0.3f, 0.3f, 1);
                dice.position = ogPositions[dice];
            }
            
        }        

        CreateNewSavedGroup();
        UpdateScoreBoard();
        throwDice();
    }

    public void UpdateScoreBoard()
    {
        turnScoreText.text = "Turn Score: " + turnScore;
        totalScoreText.text = "Total Score: " + totalScore;
    }

    public void UpdateTurnScore()
    {
        List<int> savedDiceValues = new List<int>();

        Transform RoundSaved = GameObject.Find("RoundSDice").transform;

        // Collect all saved dice values
        foreach (Transform roundGroup in RoundSaved)
        {
            foreach (Transform dice in roundGroup)
            {
                savedDiceValues.Add(dice.GetComponent<Dice>().GetDiceValue());
            }
        }

        // **Recalculate turnScore from scratch**
        turnScore = CalculateDiceScore(savedDiceValues.ToArray());

        UpdateScoreBoard();
        Debug.Log("Turn Score Updated: " + turnScore);
    }






    public int CalculateDiceScore(int[] diceValues)
    {
        int score = 0;
        int[] counts = new int[7]; // 1-based index, ignore index 0

        foreach (int value in diceValues)
        {
            counts[value]++;
        }

        // Check for three-of-a-kind
        for (int i = 1; i <= 6; i++)
        {
            if (counts[i] >= 6)
            {
                score += 3000;
            }else if (counts[i] >= 5)
            {
                score += 2000;
            }else if (counts[i] >= 4)
            {
                score += 1000;
            }else if (counts[i] >= 3)
            {
                if (i == 1) score += 1000 + (counts[i] - 3) * 100;  // Special rule for 1s
                else score += i * 100;  // Correct multiplication for other numbers
            }           
            
        }

        if (counts[1] < 3) score += counts[1] * 100;
        if (counts[5] < 3) score += counts[5] * 50;

        Debug.Log("Score Calculated: " + score);
        return score;
    }

}
