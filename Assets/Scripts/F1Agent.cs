using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class F1Agent : Agent
{
    // speed variables
    public float moveSpeed = 50f;     // Top speed going forward
    public float reverseSpeed = 15f;  // Slow speed for reverse gear
    public float turnSpeed = 250f;

    // axis configuration 
    public Vector3 forwardAxis = new Vector3(1, 0, 0); // forward acceleration direction
    public Vector3 turnAxis = new Vector3(0, 1, 0);    // steering rotation axis

    // spawn point reference
    public Transform spawnPoint; 

    private Rigidbody rb;

    // runs once at start
    public override void Initialize() 
    { 
        rb = GetComponent<Rigidbody>();
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
    }

    // get sensor data
    public override void CollectObservations(VectorSensor sensor) { }

    // receive AI actions and move
    public override void OnActionReceived(ActionBuffers actions)
    {
        // accelerate and brake
        float moveInput = actions.ContinuousActions[0];
        // steer left and right
        float turnInput = actions.ContinuousActions[1];
        
        // determine which speed to use based on input direction
        float currentSpeed = (moveInput >= 0) ? moveSpeed : reverseSpeed;

        // 1. ROTATION
        transform.Rotate(turnAxis * turnInput * turnSpeed * Time.deltaTime);

        // 2. FORWARD / BACKWARD MOVEMENT
        transform.Translate(forwardAxis * moveInput * currentSpeed * Time.deltaTime);

        // 3. TRACK ALIGNMENT (Slope adaptation)
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
        {
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, Time.deltaTime * 15f);
        }

        // --- ML-AGENTS: FALL SAFETY NET ---
        // If the car falls through the map 15 meters below the spawn line, reset the episode.
        if (transform.position.y < spawnPoint.position.y - 15f)
        {
            EndEpisode(); // This function automatically calls OnEpisodeBegin()
        }
    }

    // --- ML-AGENTS: CRASH DETECTION ---
    // Automatically triggered when the Box Collider hits another physical object.
    private void OnCollisionEnter(Collision collision)
    {
        // If the collided object is tagged as a "Wall"...
        if (collision.gameObject.CompareTag("Wall"))
        {
            EndEpisode(); // Crash = Game Over, reset to the starting grid
        }
    }

    // manual testing with keyboard
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        
        // w/s keys
        continuousActions[0] = Input.GetAxisRaw("Vertical");
        // a/d keys
        continuousActions[1] = Input.GetAxisRaw("Horizontal");
    }
}