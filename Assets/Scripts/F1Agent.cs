using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class F1Agent : Agent
{
    // speed variables
    public float moveSpeed = 30f;
    public float turnSpeed = 100f;

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

        // apply movement using the custom axis
        transform.Translate(forwardAxis * moveInput * moveSpeed * Time.deltaTime);
        // apply rotation using the custom turn axis
        transform.Rotate(turnAxis * turnInput * turnSpeed * Time.deltaTime);
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