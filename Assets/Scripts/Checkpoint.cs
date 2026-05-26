using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [HideInInspector]
    public RaceManager raceManager; // set automatically by manager

    private void OnTriggerEnter(Collider other)
    {
        // check if the car entered the checkpoint trigger
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Contenidor"))
        {
            raceManager.CarPassedCheckpoint(this);
        }
    }
}