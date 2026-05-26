using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class F1Agent : Agent
{
    // speed variables
    public float moveSpeed = 200f;    // top speed going forward
    public float reverseSpeed = 50f;  // slow speed for reverse gear
    public float turnSpeed = 100f;    // steering sensitivity

    // axis configuration 
    public Vector3 forwardAxis = new Vector3(1, 0, 0); // forward acceleration direction
    public Vector3 turnAxis = new Vector3(0, 1, 0);    // steering rotation axis

    // spawn point reference
    public Transform spawnPoint; 

    private Rigidbody rb;
    private float currentActualSpeed; // tracks real-time speed for telemetry
    private RaceManager raceManager;  // reference to ui manager

    // runs once at start
    public override void Initialize() 
    { 
        rb = GetComponent<Rigidbody>();
        raceManager = FindObjectOfType<RaceManager>();
    }

    // runs on crash or restart (reset to starting grid using the spawn point)
    public override void OnEpisodeBegin() 
    { 
        if (spawnPoint != null)
        {
            transform.localPosition = spawnPoint.localPosition;
            transform.localRotation = spawnPoint.localRotation;
        }

        // stop all physics forces
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        currentActualSpeed = 0f;

        // tell race manager to clean sectors and clock on crash
        if (raceManager != null)
        {
            raceManager.ResetRaceOnCrash();
        }
    }

    // --- ML-AGENTS: TELEMETRY (SENSORS) ---
    public override void CollectObservations(VectorSensor sensor) 
    { 
        sensor.AddObservation(currentActualSpeed);
    }

    // receive AI actions and move
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0];
        float turnInput = actions.ContinuousActions[1];
        
        float speedMultiplier = (moveInput >= 0) ? moveSpeed : reverseSpeed;
        currentActualSpeed = moveInput * speedMultiplier; 

        // 1. ROTATION
        transform.Rotate(turnAxis * turnInput * turnSpeed * Time.deltaTime);

        // 2. FORWARD / BACKWARD MOVEMENT
        transform.Translate(forwardAxis * currentActualSpeed * Time.deltaTime);

        // 3. TRACK ALIGNMENT (Slope adaptation)
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
        {
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, Time.deltaTime * 15f);
        }

        // --- ML-AGENTS: FALL SAFETY NET ---
        if (transform.position.y < spawnPoint.position.y - 300f)
        {
            Debug.LogWarning("➔ s'ha reiniciat per caiguda! alçada actual: " + transform.position.y);
            EndEpisode(); 
        }
    }

    // --- ML-AGENTS: CRASH DETECTION ---
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.LogError("➔ s'ha reiniciat per xoc amb el mur: " + collision.gameObject.name);
            EndEpisode(); 
        }
    }

    // manual testing with keyboard
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Vertical");
        continuousActions[1] = Input.GetAxisRaw("Horizontal");
    }
}