using UnityEngine;

public class TopDownFollow : MonoBehaviour
{
    [Header("Tracking Target")]
    public Transform target; // car to follow
    
    [Header("Camera Settings")]
    public float height = 150f; // height above the car

    private bool hasAligned = false;

    // runs after physics update
    void LateUpdate()
    {
        if (target != null)
        {
            // follow car keeping relative height above it
            transform.position = new Vector3(target.position.x, target.position.y + height, target.position.z);

            // align rotation only once
            if (!hasAligned)
            {
                transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
                hasAligned = true;
            }
        }
    }
}