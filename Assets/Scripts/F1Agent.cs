using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class F1Agent : Agent
{
    // speed variables
    public float moveSpeed = 280f;    
    public float reverseSpeed = 50f;  
    public float turnSpeed = 110f;    

    // axis configuration 
    public Vector3 forwardAxis = new Vector3(1, 0, 0); 
    public Vector3 turnAxis = new Vector3(0, 1, 0);    

    // spawn point reference
    public Transform spawnPoint; 

    private Rigidbody rb;
    private float currentActualSpeed; 
    private RaceManager raceManager;  

    public override void Initialize() 
    { 
        rb = GetComponent<Rigidbody>();
        raceManager = FindObjectOfType<RaceManager>();
    }

    public override void OnEpisodeBegin() 
    { 
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        currentActualSpeed = 0f;

        if (raceManager != null)
        {
            raceManager.ResetRaceOnCrash();
        }
    }

    public override void CollectObservations(VectorSensor sensor) 
    { 
        sensor.AddObservation(currentActualSpeed);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0];
        float turnInput = actions.ContinuousActions[1];
        
        float speedMultiplier = (moveInput >= 0) ? moveSpeed : reverseSpeed;
        currentActualSpeed = moveInput * speedMultiplier; 

        // 1. rotation
        transform.Rotate(turnAxis * turnInput * turnSpeed * Time.deltaTime);

        // 2. forward movement with strict anti-ghosting
        Vector3 localMovement = forwardAxis * currentActualSpeed * Time.deltaTime;
        Vector3 worldMovement = transform.TransformDirection(localMovement);
        
        if (Physics.SphereCast(transform.position, 1.0f, worldMovement.normalized, out RaycastHit hit, worldMovement.magnitude))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                AddReward(-1.0f); 
                EndEpisode(); 
                return; 
            }
        }

        transform.Translate(localMovement);

        // 3. speed reward: incentivize moving fast (only if not crashing)
        float speedReward = (currentActualSpeed / moveSpeed) * 0.005f;
        AddReward(speedReward);

        // 4. track alignment
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitFloor, 1.5f))
        {
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, hitFloor.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, Time.deltaTime * 15f);
        }

        // fall safety net
        if (transform.position.y < spawnPoint.position.y - 300f)
        {
            EndEpisode(); 
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.5f); 
            currentActualSpeed *= 0.2f; 
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Vertical");
        continuousActions[1] = Input.GetAxisRaw("Horizontal");
    }
}