using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RaceManager : MonoBehaviour
{
    [Header("ui components")]
    public TextMeshProUGUI timerText;       
    public TextMeshProUGUI leaderboardText; 
    public TextMeshProUGUI sectorsText;     

    [Header("checkpoints system")]
    public List<Checkpoint> checkpointList = new List<Checkpoint>(); 
    
    [Header("race data")]
    private float currentTime;
    private bool isTimerRunning = false;
    private List<float> lapHistory = new List<float>(); 
    
    private int currentSectorIndex = 1; 
    
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

    // anti-glitch guards
    private float lastSectorTriggerTime = 0f;
    private float minSectorDuration = 2f;

    [Header("curriculum learning")]
    private int consecutivePerfectLaps = 0;
    public int lapsToIncreaseSpeed = 5; 

    private F1Agent f1Agent; 
    private EvaluationManager evalManager;

    void Start()
    {
        currentTime = 0f;
        f1Agent = FindObjectOfType<F1Agent>();
        evalManager = FindObjectOfType<EvaluationManager>();

        // link checkpoints
        foreach (Checkpoint cp in checkpointList)
        {
            if (cp != null) cp.raceManager = this;
        }

        // force domain randomization from start
        if (f1Agent != null)
        {
            f1Agent.useDomainRandomization = true;
        }

        // auto-start timer
        StartTimer(); 
        
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
            UpdateLiveSectorsUI(); 
        }
    }

    public void StartTimer()
    {
        isTimerRunning = true;
        currentSectorIndex = 1; 
        sector1Time = 0f;
        sector2Time = 0f;
        
        s1Text = "-"; 
        s2Text = "-"; 
        s3Text = "-";
        lastSectorTriggerTime = Time.time;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void ResetRaceOnCrash()
    {
        isTimerRunning = true; 
        currentTime = 0f;
        currentSectorIndex = 1; 
        sector1Time = 0f;
        sector2Time = 0f;
        
        consecutivePerfectLaps = 0;
        lastSectorTriggerTime = Time.time;

        // ai safety reset keeping randomization active
        if (f1Agent != null)
        {
            f1Agent.moveSpeed = 40f; 
            f1Agent.turnSpeed = 20f;
            f1Agent.useDomainRandomization = true;
        }
        
        s1Text = "-";
        s2Text = "-";
        s3Text = "-";
        
        UpdateLiveSectorsUI();
        UpdateTimerUI();

        if (evalManager != null) evalManager.RecordCrash();
    }

    // triggered by sector tags
    public void CarPassedSector()
    {
        if (!isTimerRunning) return;

        // block rapid double triggers from crashes or spins
        if (Time.time - lastSectorTriggerTime < minSectorDuration) return;
        lastSectorTriggerTime = Time.time;

        if (currentSectorIndex == 1)
        {
            sector1Time = currentTime;
            string color = GetSectorColor(sector1Time, bestSector1);
            if (sector1Time < bestSector1) bestSector1 = sector1Time;

            s1Text = $"<color={color}>{FormatTime(sector1Time)}</color>";
            currentSectorIndex = 2; // move to s2
        }
        else if (currentSectorIndex == 2)
        {
            sector2Time = currentTime - sector1Time;
            string color = GetSectorColor(sector2Time, bestSector2);
            if (sector2Time < bestSector2) bestSector2 = sector2Time;

            s2Text = $"<color={color}>{FormatTime(sector2Time)}</color>";
            currentSectorIndex = 3; // move to s3
        }
        else if (currentSectorIndex == 3)
        {
            CompleteLap();
        }
    }

    // keep to prevent checkpoint.cs errors
    public void CarPassedCheckpoint(Checkpoint checkpoint)
    {
    }

    private string GetSectorColor(float time, float bestTime)
    {
        if (time < bestTime) return "#A020F0"; // purple
        return "#FFFF00"; // yellow
    }

    public void CompleteLap()
    {
        if (!isTimerRunning) return;

        // final check to reject impossible short laps
        // if (currentTime < 45f) return;

        float sector3Time = currentTime - (sector1Time + sector2Time);
        string colorS3 = GetSectorColor(sector3Time, bestSector3);
        if (sector3Time < bestSector3) bestSector3 = sector3Time;

        lapHistory.Add(currentTime);
        
        if (evalManager != null) evalManager.RecordLap(currentTime);
        
        // curriculum learning
        consecutivePerfectLaps++;
        
        // ==========================================
        // AVALUATION: we coment the increase in speed
        // ==========================================
        /*
        if (f1Agent != null)
        {
            // progressive speed scale capped at 80f
            if (f1Agent.moveSpeed < 80f)
            {
                f1Agent.moveSpeed += 5f;
                f1Agent.turnSpeed += 1f; // adjust turning
                Debug.Log($"➔ pace increase! speed: {f1Agent.moveSpeed} | turn: {f1Agent.turnSpeed}");
            }
        }
        */

        UpdateLeaderboardUI();
        
        // reset timers for next lap
        currentTime = 0f;
        currentSectorIndex = 1; 
        sector1Time = 0f;
        sector2Time = 0f;

        s1Text = "-";
        s2Text = "-";
        s3Text = "-";

        // reactivate checkpoints
        foreach (Checkpoint cp in checkpointList)
        {
            if (cp != null) cp.gameObject.SetActive(true);
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null) timerText.text = FormatTime(currentTime);
    }

    private void UpdateLiveSectorsUI()
    {
        if (sectorsText == null) return;

        string displayS1 = s1Text;
        string displayS2 = s2Text;
        string displayS3 = s3Text;

        if (isTimerRunning)
        {
            if (currentSectorIndex == 1) 
                displayS1 = FormatTime(currentTime);
            else if (currentSectorIndex == 2) 
                displayS2 = FormatTime(currentTime - sector1Time);
            else if (currentSectorIndex == 3) 
                displayS3 = FormatTime(currentTime - (sector1Time + sector2Time));
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
                sb += $"{i + 1}. {FormatTime(sortedLaps[i])}\n";
            else
                sb += $"{i + 1}. --:--.--\n";
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