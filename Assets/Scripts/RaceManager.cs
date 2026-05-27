using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RaceManager : MonoBehaviour
{
    [Header("ui components")]
    public TextMeshProUGUI timerText;       
    public TextMeshProUGUI leaderboardText; 
    public TextMeshProUGUI sectorsText;     // s1, s2, s3 text

    [Header("checkpoints system")]
    public List<Checkpoint> checkpointList = new List<Checkpoint>(); 
    private int nextCheckpointIndex = 0;

    [Header("race data")]
    private float currentTime;
    private bool isTimerRunning = false;
    private List<float> lapHistory = new List<float>(); 
    
    // current lap sectors
    private float sector1Time = 0f;
    private float sector2Time = 0f;

    // session best sectors
    private float bestSector1 = float.MaxValue;
    private float bestSector2 = float.MaxValue;
    private float bestSector3 = float.MaxValue;

    // saved ui strings
    private string s1Text = "-";
    private string s2Text = "-";
    private string s3Text = "-";

    [Header("curriculum learning")]
    private int consecutivePerfectLaps = 0;
    public int lapsToIncreaseSpeed = 5;
    public int lapsToEnableRain = 10;

    private F1Agent f1Agent; 

    void Start()
    {
        currentTime = 0f;
        isTimerRunning = false;
        
        f1Agent = FindObjectOfType<F1Agent>();

        // link checkpoints
        foreach (Checkpoint cp in checkpointList)
        {
            if (cp != null) cp.raceManager = this;
        }

        nextCheckpointIndex = 0;
        UpdateTimerUI();
        UpdateLeaderboardUI(); 
        UpdateLiveSectorsUI(); 
    }

    void Update()
    {
        if (isTimerRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI();
            UpdateLiveSectorsUI(); // updates sector clock every frame
        }
    }

    // start from grid
    public void StartTimer()
    {
        isTimerRunning = true;
        nextCheckpointIndex = 0; 
        sector1Time = 0f;
        sector2Time = 0f;
        
        s1Text = "-"; 
        s2Text = "-"; 
        s3Text = "-";
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    // called when the ai crashes or falls
    public void ResetRaceOnCrash()
    {
        isTimerRunning = false; 
        currentTime = 0f;
        nextCheckpointIndex = 0; 
        sector1Time = 0f;
        sector2Time = 0f;
        
        // reset curriculum progress on crash
        consecutivePerfectLaps = 0;
        
        // clean ui text
        s1Text = "-";
        s2Text = "-";
        s3Text = "-";
        
        UpdateLiveSectorsUI();
        UpdateTimerUI();
    }

    public void CarPassedCheckpoint(Checkpoint checkpoint)
    {
        int index = checkpointList.IndexOf(checkpoint);

        if (index == nextCheckpointIndex)
        {
            if (index == 0) // passed cp1 -> end s1
            {
                sector1Time = currentTime;
                string color = GetSectorColor(sector1Time, bestSector1);
                if (sector1Time < bestSector1) bestSector1 = sector1Time;

                // freeze s1 text with color
                s1Text = $"<color={color}>{FormatTime(sector1Time)}</color>";
                Debug.Log($"➔ sector 1: {FormatTime(sector1Time)}");
            }
            else if (index == 1) // passed cp2 -> end s2
            {
                sector2Time = currentTime - sector1Time;
                string color = GetSectorColor(sector2Time, bestSector2);
                if (sector2Time < bestSector2) bestSector2 = sector2Time;

                // freeze s2 text with color
                s2Text = $"<color={color}>{FormatTime(sector2Time)}</color>";
                Debug.Log($"➔ sector 2: {FormatTime(sector2Time)}");
            }

            nextCheckpointIndex++;
        }
    }

    // f1 color logic (purple or yellow)
    private string GetSectorColor(float time, float bestTime)
    {
        if (time < bestTime) return "#A020F0"; // record
        return "#FFFF00"; // slower
    }

    public void CompleteLap()
    {
        if (!isTimerRunning) return;

        // check if all passed
        if (nextCheckpointIndex < checkpointList.Count)
        {
            Debug.LogWarning("➔ lap invalid: missing checkpoints!");
            return;
        }

        // calc s3 time
        float sector3Time = currentTime - (sector1Time + sector2Time);
        string colorS3 = GetSectorColor(sector3Time, bestSector3);
        if (sector3Time < bestSector3) bestSector3 = sector3Time;

        // save lap
        lapHistory.Add(currentTime);
        Debug.Log($"➔ sector 3: {FormatTime(sector3Time)} (color: {colorS3})");
        Debug.Log($"lap {lapHistory.Count} time: {FormatTime(currentTime)}");

        // --- curriculum learning logic ---
        consecutivePerfectLaps++;
        Debug.Log($"➔ consecutive perfect laps: {consecutivePerfectLaps}");

        if (f1Agent != null)
        {
            if (consecutivePerfectLaps == lapsToIncreaseSpeed)
            {
                f1Agent.moveSpeed = 200f; // bump speed automatically
                Debug.Log("➔ curriculum update: speed increased to 200!");
            }
            else if (consecutivePerfectLaps == lapsToEnableRain)
            {
                f1Agent.useDomainRandomization = true; // enable weather variations
                Debug.Log("➔ curriculum update: domain randomization enabled (rain possible)!");
            }
        }
        // ---------------------------------

        UpdateLeaderboardUI();
        
        // reset timers for the next continuous lap
        currentTime = 0f;
        nextCheckpointIndex = 0; 
        sector1Time = 0f;
        sector2Time = 0f;

        // clean ui text for new lap (s1 will auto-run in update)
        s1Text = "-";
        s2Text = "-";
        s3Text = "-";
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTime);
        }
    }

    // shows live running time for the active sector
    private void UpdateLiveSectorsUI()
    {
        if (sectorsText == null) return;

        string displayS1 = s1Text;
        string displayS2 = s2Text;
        string displayS3 = s3Text;

        if (isTimerRunning)
        {
            if (nextCheckpointIndex == 0) // currently in s1
            {
                displayS1 = FormatTime(currentTime);
            }
            else if (nextCheckpointIndex == 1) // currently in s2
            {
                displayS2 = FormatTime(currentTime - sector1Time);
            }
            else if (nextCheckpointIndex == 2) // currently in s3
            {
                displayS3 = FormatTime(currentTime - (sector1Time + sector2Time));
            }
        }

        sectorsText.text = $"S1: {displayS1}\nS2: {displayS2}\nS3: {displayS3}";
    }

    private void UpdateLeaderboardUI()
    {
        if (leaderboardText == null) return;

        List<float> sortedLaps = new List<float>(lapHistory);
        sortedLaps.Sort();

        string sb = "TOP LAPS\n";
        for (int i = 0; i < 3; i++)
        {
            if (i < sortedLaps.Count)
            {
                sb += $"{i + 1}. {FormatTime(sortedLaps[i])}\n";
            }
            else
            {
                sb += $"{i + 1}. --:--.--\n";
            }
        }
        leaderboardText.text = sb;
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        int milliseconds = Mathf.FloorToInt((time * 100F) % 100F);

        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }
}