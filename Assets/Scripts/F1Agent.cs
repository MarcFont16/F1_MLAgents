using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class F1Agent : Agent
{
    // speed settings
    public float moveSpeed = 60f;    
    public float reverseSpeed = 15f;  
    public float turnSpeed = 30f;    

    // domain randomization
    public bool useDomainRandomization = false;
    public PhysicMaterial trackMaterial;

    // virtual steering
    public float steeringSpeed = 10f; 
    public float currentTurnInput = 0f;
    private float previousTurnInput = 0f; // tracks steering changes

    // axes and refs
    public Vector3 forwardAxis = new Vector3(1, 0, 0); 
    public Vector3 turnAxis = new Vector3(0, 1, 0);    
    public Transform spawnPoint; 
    public GameObject[] rewardGates;

    private Rigidbody rb;
    public float currentActualSpeed; 
    private RaceManager raceManager;  

    public override void Initialize() 
    { 
        rb = GetComponent<Rigidbody>();
        raceManager = FindObjectOfType<RaceManager>();

        // force find checkpoints
        rewardGates = GameObject.FindGameObjectsWithTag("Checkpoint");
    }

    public override void OnEpisodeBegin() 
    { 
        // reset pos and rot
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        // clear physics
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        currentActualSpeed = 0f;
        currentTurnInput = 0f; 
        previousTurnInput = 0f;

        // reset race manager (it will auto-start the timer)
        if (raceManager != null) raceManager.ResetRaceOnCrash();
        
        // =======================================================
        // AVALUATION: friction block comented 
        // =======================================================
        /*
        if (useDomainRandomization && trackMaterial != null)
        {
            float randomFriction = Random.Range(0.3f, 0.85f);
            trackMaterial.dynamicFriction = randomFriction;
            trackMaterial.staticFriction = randomFriction;
        }
        else if (trackMaterial != null)
        {
            trackMaterial.dynamicFriction = 0.8f;
            trackMaterial.staticFriction = 0.8f;
        }
        */
        
        // reset gates
        if (rewardGates != null)
        {
            foreach (GameObject gate in rewardGates)
                if (gate != null) gate.SetActive(true);
        }
    }

    public override void CollectObservations(VectorSensor sensor) 
    { 
        // inject noise
        float noisySpeed = currentActualSpeed + Random.Range(-2f, 2f);
        sensor.AddObservation(noisySpeed);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0];
        float rawTurnInput = actions.ContinuousActions[1]; 
        
        // penalties default to 1 (no penalty)
        float speedPenalty = 1f;
        float turnPenalty = 1f;

        // 1. check floor before moving
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitFloor, 1.5f))
        {
            if (hitFloor.collider.CompareTag("Grass"))
            {
                AddReward(-0.002f);
                speedPenalty = 0.60f; 
                turnPenalty = 0.50f;  
            }
            else if (hitFloor.collider.CompareTag("Gravel"))
            {
                AddReward(-0.005f); 
                speedPenalty = 0.25f; 
                turnPenalty = 0.20f;  
            }

            // align floor
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, hitFloor.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, Time.deltaTime * 15f);
        }

        // 2. calculate inputs with penalties
        float targetTurnInput = rawTurnInput * rawTurnInput * rawTurnInput;
        if (Mathf.Abs(targetTurnInput) < 0.02f) targetTurnInput = 0f;

        // apply turn penalty
        currentTurnInput = Mathf.Lerp(currentTurnInput, targetTurnInput, Time.deltaTime * steeringSpeed) * turnPenalty;

        float speedMultiplier = (moveInput >= 0) ? moveSpeed : reverseSpeed;
        
        // apply speed penalty
        currentActualSpeed = moveInput * speedMultiplier * speedPenalty; 

        // 3. apply rotation
        transform.Rotate(turnAxis * currentTurnInput * turnSpeed * Time.deltaTime);

        // move with spherecast
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

        // 4. execute movement
        transform.Translate(localMovement);

        // time and jerk penalties
        AddReward(-0.0002f);
        float turnDifference = Mathf.Abs(targetTurnInput - previousTurnInput);
        if (turnDifference > 0.05f) AddReward(-turnDifference * 0.01f);
        previousTurnInput = targetTurnInput;

        // speed reward and anti-idle
        if (currentActualSpeed > 0.1f)
        {
            AddReward((currentActualSpeed / moveSpeed) * 0.002f);
        }
        else if (Mathf.Abs(currentActualSpeed) < 0.1f)
        {
            AddReward(-0.001f);
        }

        // fall check
        if (transform.position.y < spawnPoint.position.y - 300f) EndEpisode(); 
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

    private void OnTriggerEnter(Collider other)
    {
        // 1. ai checkpoints (give points, hide object)
        if (other.CompareTag("Checkpoint")) 
        {
            AddReward(1.0f); 
            other.gameObject.SetActive(false); 
        }
        // 2. telemetry sectors (only for timer)
        else if (other.CompareTag("Sector"))
        {
            if (raceManager != null) 
            {
                raceManager.CarPassedSector(); 
            }
        }
    }
}