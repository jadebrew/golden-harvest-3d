using UnityEngine;

public class NPC : MonoBehaviour
{
    public Personality person;

    float viewRange = 2f;
    float fov = 360f; // Total Field of View in degrees

    public bool isInFoV, isInRange, isNotHidden;

    public GameObject target;

    void FixedUpdate()
    {
        if (target == null) return;

        isInFoV = false;
        isInRange = false;
        isNotHidden = false; // Reset every frame so it recalculates properly

        // 1. Distance Check
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance < viewRange)
        {
            isInRange = true;
        }

        // 2. Line of Sight (Raycast) Check
        Vector3 direction = target.transform.position - transform.position;
        RaycastHit hit;
        if (isInRange && Physics.Raycast(transform.position, direction.normalized, out hit, direction.magnitude))
        {
            if (hit.transform == target.transform)
            {
                isNotHidden = true;
                hit.transform.gameObject.GetComponent<PlayerMovement>().SetTarget(this.gameObject.GetComponent<NPC>());
            }
        }

        // 3. Field of View (FoV) Check
        float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);

        // Check if the target is within half of the FOV range (left and right)
        if (isNotHidden && (Mathf.Abs(angle) <= fov * 0.5f))
        {
            isInFoV = true;
        }
    }

    public bool CanSee()
    {
        return isInFoV;
    }
}
