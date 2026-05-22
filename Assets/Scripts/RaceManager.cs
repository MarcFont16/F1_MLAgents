using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RaceManager : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI timerText; // ui text reference

    [Header("Race Data")]
    private float currentTime;
    private bool isTimerRunning = false;
    private List<float> lapHistory = new List<float>(); // saves lap times

    void Start()
    {
        // init at 0 and wait for car to cross the line
        currentTime = 0f;
        isTimerRunning = false;
        UpdateTimerUI();
    }

    void Update()
    {
        if (isTimerRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void StartTimer()
    {
        isTimerRunning = true;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void ResetTimer()
    {
        currentTime = 0f;
    }

    public void CompleteLap()
    {
        if (!isTimerRunning) return;

        // save lap and print to console
        lapHistory.Add(currentTime);
        Debug.Log($"lap {lapHistory.Count} time: {FormatTime(currentTime)}");

        ResetTimer();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTime);
        }
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