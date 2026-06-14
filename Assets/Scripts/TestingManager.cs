using UnityEngine;

public class TestingManager : MonoBehaviour
{
    public F1Agent agent;
    
    [Header("evaluation settings")]
    public int totalEpisodesToTest = 20;
    
    // expanded weather conditions for deep testing
    public enum WeatherCondition 
    { 
        Optimal,    // 0.85 (rubbered track)
        Dry,        // 0.80 (baseline)
        Dusty,      // 0.70 (dirty track)
        Damp,       // 0.60 (light rain)
        Wet,        // 0.50 (steady rain)
        HeavyRain,  // 0.40 (extreme wet)
        Storm       // 0.30 (survival mode)
    }
    public WeatherCondition testCondition = WeatherCondition.Dry;

    private float targetFriction = 0.8f; 
    private int currentEpisode = 0;
    private int successfulLaps = 0;
    private int crashes = 0;

    void Start()
    {
        if (agent != null)
        {
            // disable automatic domain randomization for controlled testing
            agent.useDomainRandomization = false; 
            
            // map conditions to friction values
            switch (testCondition)
            {
                case WeatherCondition.Optimal:   targetFriction = 0.85f; break;
                case WeatherCondition.Dry:       targetFriction = 0.80f; break;
                case WeatherCondition.Dusty:     targetFriction = 0.70f; break;
                case WeatherCondition.Damp:      targetFriction = 0.60f; break;
                case WeatherCondition.Wet:       targetFriction = 0.50f; break;
                case WeatherCondition.HeavyRain: targetFriction = 0.40f; break;
                case WeatherCondition.Storm:     targetFriction = 0.30f; break;
            }

            SetTrackCondition(targetFriction);
            Debug.Log($"➔ starting test suite: {totalEpisodesToTest} episodes. condition: {testCondition} ({targetFriction} μ)");
        }
    }

    // call this from race manager when a full lap is completed
    public void RecordSuccess()
    {
        successfulLaps++;
        AdvanceEpisode();
    }

    // call this from race manager or f1agent when it hits a wall
    public void RecordCrash()
    {
        crashes++;
        AdvanceEpisode();
    }

    private void AdvanceEpisode()
    {
        currentEpisode++;
        
        if (currentEpisode >= totalEpisodesToTest)
        {
            PrintFinalResults();
            // pause game when test is complete
            UnityEditor.EditorApplication.isPaused = true; 
        }
    }

    private void SetTrackCondition(float frictionVal)
    {
        if (agent.trackMaterial != null)
        {
            agent.trackMaterial.dynamicFriction = frictionVal;
            agent.trackMaterial.staticFriction = frictionVal;
        }
    }

    private void PrintFinalResults()
    {
        float successRate = ((float)successfulLaps / totalEpisodesToTest) * 100f;
        
        Debug.Log("===================================");
        Debug.Log("   EVALUATION PROTOCOL COMPLETE    ");
        Debug.Log("===================================");
        Debug.Log($"weather: {testCondition} (friction: {targetFriction})");
        Debug.Log($"total episodes: {totalEpisodesToTest}");
        Debug.Log($"crashes: {crashes}");
        Debug.Log($"successful laps: {successfulLaps}");
        Debug.Log($"SUCCESS RATE: {successRate}%");
        Debug.Log("===================================");
    }
}