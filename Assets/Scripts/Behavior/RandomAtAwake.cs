using UnityEngine;

public class RandomAtAwake : MonoBehaviour
{
    private Vector3 positionRange = new Vector3(0.1f, 0, 0.1f);

    [Header("Random Vertical Rotation")]
    public float rotationRange = 360f;

    private void Start()
    {
        // Random displacement on X, Y and Z
        Vector3 offset = new Vector3(
            Random.Range(-positionRange.x, positionRange.x),
                                     Random.Range(-positionRange.y, positionRange.y),
                                     Random.Range(-positionRange.z, positionRange.z)
        );

        transform.position += offset;

        // Random rotation around the vertical (Y) axis
        float randomY = Random.Range(-rotationRange, rotationRange);
        transform.Rotate(0f, randomY, 0f, Space.Self);
    }
}
