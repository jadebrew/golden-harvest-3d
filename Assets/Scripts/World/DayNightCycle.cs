using System;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class DayNightCycle : MonoBehaviour
{
    private static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");

    [Tooltip("Tilts the sun's orbit away from straight overhead so noon shadows aren't directly underneath objects.")]
    [Range(0f, 60f)]
    [SerializeField] private float orbitTilt = 30f;

    [Tooltip("Compass direction (around world Y) of the sun's orbit plane.")]
    [SerializeField] private float orbitYaw = -37f;

    [Tooltip("24x1 gradient; pixel N is the sky tint at hour N. Must be Read/Write enabled.")]
    [SerializeField] private Texture2D skyGradient;

    private Material skybox;
    private Light sun;

    public float timeOffset = 8;

    /// <summary>When set, replaces the real-time clock with a fixed hour (0-24). Used by debug tooling.</summary>
    public float? HourOverride { get; set; }

    /// <summary>The hour of day currently being displayed, wrapped to [0, 24).</summary>
    public float CurrentHour =>
        Mathf.Repeat(HourOverride ?? ((float)DateTime.Now.TimeOfDay.TotalHours + timeOffset), 24f);

    void Start()
    {
        sun = GetComponent<Light>();

        // Work on a runtime copy so play mode doesn't write tint changes back into the material asset.
        if (RenderSettings.skybox == null) return;
        
        skybox = new Material(RenderSettings.skybox);
        RenderSettings.skybox = skybox;
    }

    void Update()
    {
        var hours = CurrentHour;

        transform.rotation = SunRotation(hours, orbitTilt, orbitYaw);

        if (!skyGradient) return;

        var tint = SampleSkyTint(skyGradient, hours);
        sun.color = tint;
        if (skybox)
            skybox.SetColor(SkyTintId, tint);
    }

    public static Quaternion SunRotation(float hours, float tilt, float yaw)
    {
        // 0° at 06:00 (horizon), 90° at 12:00 (overhead), 180° at 18:00, 270° at midnight.
        var sunAngle = (hours / 24f) * 360f - 90f;

        return
            Quaternion.AngleAxis(yaw, Vector3.up) *
            Quaternion.AngleAxis(tilt, Vector3.forward) *
            Quaternion.AngleAxis(sunAngle, Vector3.right);
    }

    public static Color SampleSkyTint(Texture2D gradient, float hours)
    {
        // Blend between the current hour's pixel and the next, wrapping 23:00 -> 00:00.
        var width = gradient.width;
        var x = hours / 24f * width;
        var i = Mathf.FloorToInt(x) % width;
        var next = (i + 1) % width;
        return Color.Lerp(gradient.GetPixel(i, 0), gradient.GetPixel(next, 0), x - Mathf.Floor(x));
    }
}
