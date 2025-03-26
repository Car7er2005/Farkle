using System.Collections.Generic;
using System.Linq;
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
    public List<int> turnSavedDiceValues = new List<int>();
    public GameObject currentSavedGroup;

    public Text turnScoreText, roundScoreText, totalScoreText;
    
    public int turnScore, roundScore, totalScore, turnNumber;
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

        // Allow rolling without saving any dice in the first round
        if (turnNumber > 1 && HasNonScoringDice())
        {
            Debug.Log("Cannot roll because there is a non-scoring die selected.");
            roll.interactable = false;
            return;
        }

        hasRolled = true;
        UpdateScoreBoard();

        // Move previous round's saved dice to the left and make them smaller
        float startX = -7; // Left side
        float startY = 3;  // Start stacking down from here
        float yOffset = -0.5f; // Space between dice

        int savedDiceCount = 0; // Track how many dice have been moved

        roundScore += turnScore; // Add turn score to round score

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

        List<int> diceValues = new List<int>();

        foreach (Transform dice in playableDiceParent)
        {
            yOffset = (Random.Range(-5, 15) * 0.1f % 1.5f) + 1;
            dice.position = new Vector3(dice.position.x, yOffset, dice.position.z);

            int randDie = Random.Range(0, diceImages.Length);
            dice.GetComponent<SpriteRenderer>().sprite = diceImages[randDie];

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
            roundScore = 0;
            Bank();
            UpdateScoreBoard();
        }

        hasSaved = false; // Reset so a die must be saved before rolling again        
    }

    public void Bank()
    {
        totalScore += roundScore + turnScore;
        roundScore = 0;
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
            diceScript.state = Dice.DiceState.InPlay;
            if (ogPositions.ContainsKey(dice))
            {
                dice.localScale = new Vector3(0.3f, 0.3f, 1);
                dice.position = ogPositions[dice];
            }
        }
        turnSavedDiceValues.Clear();

        roundScoreText.text = "Round Score: " + roundScore;
        CreateNewSavedGroup();
        UpdateScoreBoard();

        // Ensure the dice are rolled for the new round after banking
        hasSaved = true; // Allow rolling without saving any dice in the first roll of the new round
        throwDice();
    }

    public void UpdateScoreBoard()
    {
        turnScoreText.text = "Turn Score: " + turnScore;
        roundScoreText.text = "Round Score: " + roundScore;
        totalScoreText.text = "Total Score: " + totalScore;
    }
    public void UpdateTurnScore()
    {
        List<int> latestSavedDiceValues = new List<int>();

        // Collect dice values from the current saved group
        foreach (Transform dice in currentSavedGroup.transform)
        {
            latestSavedDiceValues.Add(dice.GetComponent<Dice>().GetDiceValue());
        }

        // Recalculate the turn score from scratch based on the current turn saved dice values
        turnScore = CalculateDiceScore(latestSavedDiceValues.ToArray());

        // Enable or disable the roll button based on whether all selected dice contribute to the score
        roll.interactable = turnNumber == 1 || !HasNonScoringDice();

        UpdateScoreBoard();
        Debug.Log("Turn Score Updated: " + turnScore);
    }

    private bool HasNonScoringDice()
    {
        List<int> latestSavedDiceValues = new List<int>();

        // Collect dice values from the current saved group
        foreach (Transform dice in currentSavedGroup.transform)
        {
            latestSavedDiceValues.Add(dice.GetComponent<Dice>().GetDiceValue());
        }

        // Check if the turn score is zero, indicating non-scoring dice
        return CalculateDiceScore(latestSavedDiceValues.ToArray()) == 0;
    }

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

        // Check for straight (1-5, 2-6, 1-6)
        if (diceCounts.Count == 5 && diceCounts.ContainsKey(1) && diceCounts.ContainsKey(2) && diceCounts.ContainsKey(3) && diceCounts.ContainsKey(4) && diceCounts.ContainsKey(5))
        {
            score += 1500; // 1-5 straight
        }
        else if (diceCounts.Count == 5 && diceCounts.ContainsKey(2) && diceCounts.ContainsKey(3) && diceCounts.ContainsKey(4) && diceCounts.ContainsKey(5) && diceCounts.ContainsKey(6))
        {
            score += 1500; // 2-6 straight
        }
        else if (diceCounts.Count == 6 && diceCounts.ContainsKey(1) && diceCounts.ContainsKey(2) && diceCounts.ContainsKey(3) && diceCounts.ContainsKey(4) && diceCounts.ContainsKey(5) && diceCounts.ContainsKey(6))
        {
            score += 2000; // 1-6 straight
        }

        // Check for three pairs
        if (diceCounts.Count == 3 && diceCounts.Values.All(count => count == 2))
        {
            score += 1500; // Three pairs
        }

        // Check for two three-of-a-kinds
        if (diceCounts.Count == 2 && diceCounts.Values.All(count => count == 3))
        {
            score += 2500; // Two three-of-a-kinds
        }

        Debug.Log("Score: " + score);
        return score;
    }
}
