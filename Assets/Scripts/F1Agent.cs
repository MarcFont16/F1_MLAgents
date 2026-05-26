using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class F1Agent : Agent
{
    // speed variables updated for friction
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

        // 1. rotation (always allowed)
        transform.Rotate(turnAxis * turnInput * turnSpeed * Time.deltaTime);

        // 2. forward / backward movement with STRICT frontal anti-ghosting
        Vector3 localMovement = forwardAxis * currentActualSpeed * Time.deltaTime;
        Vector3 worldMovement = transform.TransformDirection(localMovement);
        
        // spherecast ahead to stop frontal tunneling
        if (Physics.SphereCast(transform.position, 1.0f, worldMovement.normalized, out RaycastHit hit, worldMovement.magnitude))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                // FRONT CRASH: Strict terminal reset
                AddReward(-1.0f); 
                currentActualSpeed = 0f;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                Debug.LogWarning("➔ Xoc frontal massiu! Reiniciant episodi.");
                EndEpisode(); 
                return; // crucial: stops the rest of the code so it doesn't get stuck or glitch
            }
        }

        // if the path is clear, move normally
        transform.Translate(localMovement);

        // 3. track alignment (slope adaptation)
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitFloor, 1.5f))
        {
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, hitFloor.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, Time.deltaTime * 15f);
        }

        // fall safety net (terminal)
        if (transform.position.y < spawnPoint.position.y - 300f)
        {
            Debug.LogWarning("➔ reset due to fall!");
            EndEpisode(); 
        }
    }

    // lateral scrape detection
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // LATERAL SCRAPE: Heavy penalty and almost total speed loss, but no reset
            AddReward(-0.5f); 
            currentActualSpeed *= 0.2f; // leaves you with only 20% speed
            Debug.Log("➔ Rascada al mur! Pèrdua massiva de temps.");
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