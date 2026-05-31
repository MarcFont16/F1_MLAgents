using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class F1Agent : Agent
{
    // speed settings
    public float moveSpeed = 100f;    
    public float reverseSpeed = 30f;  
    public float turnSpeed = 60f;     

    // domain randomization toggle
    public bool useDomainRandomization = false;
    public PhysicMaterial trackMaterial;

    // virtual steering wheel
    public float steeringSpeed = 10f; 
    public float currentTurnInput = 0f;
    private float previousTurnInput = 0f; // tracks steering changes for smooth driving penalties

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

        // force find all checkpoints on start, ignoring the unity inspector
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

        // reset race manager
        if (raceManager != null) raceManager.ResetRaceOnCrash();
        
        // apply random friction if enabled
        if (useDomainRandomization && trackMaterial != null)
        {
            // updated range: from extreme storm (0.3f) to optimal track (0.85f)
            float randomFriction = Random.Range(0.3f, 0.85f);
            trackMaterial.dynamicFriction = randomFriction;
            trackMaterial.staticFriction = randomFriction;
        }
        else if (trackMaterial != null)
        {
            // standard baseline friction (dry)
            trackMaterial.dynamicFriction = 0.8f;
            trackMaterial.staticFriction = 0.8f;
        }
        
        // reset reward gates
        if (rewardGates != null)
        {
            foreach (GameObject gate in rewardGates)
                if (gate != null) gate.SetActive(true);
        }
    }

    public override void CollectObservations(VectorSensor sensor) 
    { 
        // inject noise to simulate real-world sensor inaccuracy
        float noisySpeed = currentActualSpeed + Random.Range(-2f, 2f);
        sensor.AddObservation(noisySpeed);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0];
        float rawTurnInput = actions.ContinuousActions[1]; 
        
        // non-linear steering (cubic): makes small corrections tiny, keeps large turns powerful
        float targetTurnInput = rawTurnInput * rawTurnInput * rawTurnInput;

        // strict deadzone for pure straight lines
        if (Mathf.Abs(targetTurnInput) < 0.02f)
        {
            targetTurnInput = 0f;
        }

        // smooth steering execution
        currentTurnInput = Mathf.Lerp(currentTurnInput, targetTurnInput, Time.deltaTime * steeringSpeed);

        float speedMultiplier = (moveInput >= 0) ? moveSpeed : reverseSpeed;
        currentActualSpeed = moveInput * speedMultiplier; 

        // apply rotation
        transform.Rotate(turnAxis * currentTurnInput * turnSpeed * Time.deltaTime);

        // move with spherecast check
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

        // time penalty to encourage speed
        AddReward(-0.001f);

        // jerk penalty: heavily punish erratic left-right steering (volantazos)
        float turnDifference = Mathf.Abs(targetTurnInput - previousTurnInput);
        if (turnDifference > 0.05f)
        {
            AddReward(-turnDifference * 0.01f);
        }

        // save current input for next frame's comparison
        previousTurnInput = targetTurnInput;

        // speed reward
        AddReward((currentActualSpeed / moveSpeed) * 0.005f);

        // align with floor
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitFloor, 1.5f))
        {
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, hitFloor.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, Time.deltaTime * 15f);
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
        // ensure your checkpoints use the "Checkpoint" tag in unity
        if (other.CompareTag("Checkpoint")) 
        {
            // large reward for progressing forward
            AddReward(1.0f); 
            
            // disable checkpoint to prevent infinite point farming by reversing
            other.gameObject.SetActive(false); 
        }
    }
}