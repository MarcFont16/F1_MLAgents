using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class EvaluationManager : MonoBehaviour
{
    [Header("evaluation settings")]
    public F1Agent agent;
    public int episodesPerCondition = 1; // set to 1 for efficient benchmarking
    
    [System.Serializable]
    public struct WeatherCondition {
        public string name;
        public float friction;
    }
    public List<WeatherCondition> testProtocol = new List<WeatherCondition>();

    private int currentConditionIndex = 0;
    private int currentEpisode = 0;
    private int successfulLaps = 0;
    private int crashes = 0;
    private float accumulatedLapTimes = 0f;
    private bool isProcessingEvent = false;

    void Start() {
        if (agent != null) RunNextCondition();
    }

    private void RunNextCondition() {
        if (currentConditionIndex >= testProtocol.Count) {
            Debug.Log("➔ protocol complete.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
            return;
        }

        // reset metrics
        currentEpisode = 0;
        successfulLaps = 0;
        crashes = 0;
        accumulatedLapTimes = 0f;
        
        // apply friction
        agent.trackMaterial.dynamicFriction = testProtocol[currentConditionIndex].friction;
        agent.trackMaterial.staticFriction = testProtocol[currentConditionIndex].friction;
        
        Debug.Log($"➔ testing: {testProtocol[currentConditionIndex].name} ({testProtocol[currentConditionIndex].friction} mu)");
    }

    public void RecordLap(float time) {
        if (isProcessingEvent) return;
        isProcessingEvent = true;

        successfulLaps++;
        accumulatedLapTimes += time;
        AdvanceEpisode();
        
        isProcessingEvent = false;
    }

    public void RecordCrash() {
        if (isProcessingEvent) return;
        isProcessingEvent = true;

        crashes++;
        AdvanceEpisode();
        
        isProcessingEvent = false;
    }

    private void AdvanceEpisode() {
        currentEpisode++;
        if (currentEpisode >= episodesPerCondition) {
            SaveResultsToCSV();
            currentConditionIndex++;
            RunNextCondition();
        }
        // endepisode removed to prevent recursion
    }

    private void SaveResultsToCSV() {
        string path = Application.dataPath + "/results/final_evaluation_results.csv";
        float rate = ((float)successfulLaps / episodesPerCondition) * 100f;
        string line = $"{testProtocol[currentConditionIndex].name},{testProtocol[currentConditionIndex].friction},{successfulLaps},{crashes},{rate:F2},{ (successfulLaps > 0 ? accumulatedLapTimes/successfulLaps : 0):F2}\n";
        
        if (!File.Exists(path)) File.WriteAllText(path, "condition,friction,successes,crashes,rate,avg_time\n" + line);
        else File.AppendAllText(path, line);
    }
}