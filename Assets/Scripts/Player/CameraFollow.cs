using UnityEngine;

/// <summary>
/// Moves the camera smoothly towards a follow point and keeps it looking at a target.
/// </summary>
/// <remarks>
/// Runs in FixedUpdate on purpose: the player's Rigidbody isn't interpolated, so moving
/// the camera in LateUpdate instead would make the player look jittery.
/// </remarks>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Point the camera moves towards.")]
    [SerializeField] private GameObject followPoint;
    [Tooltip("What the camera looks at.")]
    [SerializeField] private GameObject lookTarget;
    [SerializeField] private float followSpeed;
    [Tooltip("The camera only moves when it is further than this from the follow point.")]
    [SerializeField] private float deadZone;

    private void FixedUpdate()
    {
        var position = transform.position;
        var goal = followPoint.transform.position;
        if ((goal - position).sqrMagnitude > deadZone * deadZone)
        {
            transform.position = Vector3.Lerp(position, goal, Time.fixedDeltaTime * followSpeed);
        }

        transform.LookAt(lookTarget.transform.position);
    }
}
