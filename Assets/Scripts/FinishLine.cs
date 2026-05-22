using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private RaceManager raceManager;

    void Start()
    {
        raceManager = FindObjectOfType<RaceManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Contenidor"))
        {
            // start timer if stopped, otherwise register lap
            if (!raceManager.IsTimerRunning()) 
            {
                raceManager.StartTimer();
            }
            else 
            {
                raceManager.CompleteLap();
            }
        }
    }
}