using UnityEngine;

namespace Player
{
    /// <summary>
    /// Moves the camera smoothly towards a follow point and keeps it looking at a target.
    /// </summary>
    /// <remarks>
    /// Runs in FixedUpdate on purpose: the player's Rigidbody isn't interpolated, so moving
    /// the camera in LateUpdate instead would make the player look jittery.
    /// </remarks>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private GameObject lookTarget;
        private void FixedUpdate()
        {
            transform.LookAt(lookTarget.transform.position);
        }
    }
}
