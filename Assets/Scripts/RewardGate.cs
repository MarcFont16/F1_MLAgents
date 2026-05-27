using UnityEngine;

public class RewardGate : MonoBehaviour
{
    // points given per gate
    public float rewardAmount = 0.1f;

    private void OnTriggerEnter(Collider other)
    {
        // check if player and add reward
        if (other.CompareTag("Player"))
        {
            F1Agent agent = other.GetComponent<F1Agent>();
            if (agent != null)
            {
                agent.AddReward(rewardAmount);
                // hide gate to avoid infinite points
                gameObject.SetActive(false);
            }
        }
    }
}