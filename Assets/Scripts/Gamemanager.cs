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
    public bool hasSaved = true, hasRolled = false, addDice = true;

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

        //Debug.Log("Rolling dice in PlayableDice...");

        List<int> diceValues = new List<int>();

        foreach (Transform dice in playableDiceParent)
        {
            yOffset = (Random.Range(-5, 15) * 0.1f % 1.5f) + 1;
            dice.position = new Vector3(dice.position.x, yOffset, dice.position.z);

            int randDie = Random.Range(0, diceImages.Length);
            dice.GetComponent<SpriteRenderer>().sprite = diceImages[randDie];

            // Check dice and dice vals
            //Debug.Log("Rolled: " + dice.name + " → Face: " + randDie);

            int diceValue = dice.GetComponent<Dice>().GetDiceValue();
            diceValues.Add(diceValue);
        }

        if (CalculateDiceScore(diceValues.ToArray()) > 0)
        {
            Debug.Log("Scoring dice found!");
        }
        else
        {
            Debug.Log("Farked it! No scoring dice.");
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
        List<int> latestSavedDiceValues = new List<int>();

<<<<<<< Updated upstream
        Transform RoundSaved = GameObject.Find("RoundSDice").transform;

        // Collect all saved dice values
        foreach (Transform roundGroup in RoundSaved)
        {
            foreach (Transform dice in roundGroup)
            {
                savedDiceValues.Add(dice.GetComponent<Dice>().GetDiceValue());
=======
        // Find the most recent SavedDice Turn# group
        Transform roundSaved = GameObject.Find("RoundSDice").transform;
        Transform latestGroup = null;
        int highestTurnNumber = -1;

        foreach (Transform savedGroup in roundSaved)
        {
            string groupName = savedGroup.name;
            if (groupName.StartsWith("SavedDice Turn"))
            {
                int turnNumber = int.Parse(groupName.Replace("SavedDice Turn", ""));
                if (turnNumber > highestTurnNumber)
                {
                    highestTurnNumber = turnNumber;
                    latestGroup = savedGroup;
                }
>>>>>>> Stashed changes
            }
        }

        // If we found a valid latest group, collect dice values
        if (latestGroup != null)
        {
            foreach (Transform dice in latestGroup)
            {
                latestSavedDiceValues.Add(dice.GetComponent<Dice>().GetDiceValue());
            }
        }

        // Calculate score
        turnScore += CalculateDiceScore(latestSavedDiceValues.ToArray());

        UpdateScoreBoard();
        Debug.Log("Turn Score Updated: " + turnScore);
    }


<<<<<<< Updated upstream




=======
>>>>>>> Stashed changes
    public int CalculateDiceScore(int[] diceValues)
    {
        Dictionary<int, int> diceCounts = new Dictionary<int, int>();

        // Count occurrences of each dice value
        foreach (int value in diceValues)
        {
            if (diceCounts.ContainsKey(value))
                diceCounts[value]++;
            else
                diceCounts[value] = 1;
        }

        int score = 0;

        foreach (var pair in diceCounts)
        {
            int diceValue = pair.Key;
            int count = pair.Value;

            if (diceValue == 1)
            {
                // 1s are worth 100 each, but 3x1s are worth 1000
                if (count >= 3)
                {
                    score += 1000 + (count - 3) * 100;  // Extra 1s still count as 100 each
                }
                else
                {
                    score += count * 100;
                }
            }
            else if (diceValue == 5)
            {
                // 5s are worth 50 each, but 3x5s are worth 500
                if (count >= 3)
                {
                    score += 500 + (count - 3) * 50;  // Extra 5s still count as 50 each
                }
                else
                {
                    score += count * 50;
                }
            }
            else
            {
                // Standard triple rule (e.g., 3x2s = 200, 3x3s = 300, etc.)
                if (count >= 3)
                    score += diceValue * 100;
            }
        }
        Debug.Log("Score: " + score);
        return score;
    }


}
