using System;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class DayNightCycle : MonoBehaviour
{
    [Tooltip("Tilts the sun's orbit away from straight overhead so noon shadows aren't directly underneath objects.")]
    [Range(0f, 60f)]
    [SerializeField] private float orbitTilt = 30f;

    [Tooltip("Compass direction (around world Y) of the sun's orbit plane.")]
    [SerializeField] private float orbitYaw = -37f;

    void Update()
    {
        var hours = (float)DateTime.Now.TimeOfDay.TotalHours;

        // 0° at 06:00 (horizon), 90° at 12:00 (overhead), 180° at 18:00, 270° at midnight.
        var sunAngle = (hours / 24f) * 360f - 90f;

        transform.rotation =
            Quaternion.AngleAxis(orbitYaw, Vector3.up) *
            Quaternion.AngleAxis(orbitTilt, Vector3.forward) *
            Quaternion.AngleAxis(sunAngle, Vector3.right);
    }
}
