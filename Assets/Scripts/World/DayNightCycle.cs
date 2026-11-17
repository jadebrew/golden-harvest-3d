using System;
using UnityEngine;

namespace World
{
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

        private Material _skybox;
        private Light _sun;

        /// <summary>Replaces the real-time clock with a fixed hour (0-24). Use this instead of using DateTime directly! Otherwise debug tools won't work.</summary>
        public float? ClockHours { get; set; }

        /// <summary>True while a debug tool is dragging the clock; listeners should hold off on reacting to hour changes.</summary>
        public bool IsScrubbing { get; set; }

        /// <summary>The hour of day currently being displayed, wrapped to [0, 24).</summary>
        public float CurrentHour =>
            Mathf.Repeat(ClockHours ?? ((float)DateTime.Now.TimeOfDay.TotalHours), 24f);

        /// <summary>The cycle in the current scene, or null if there is none.</summary>
        public static DayNightCycle Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// True if <paramref name="hour"/> lies in [start, end). Ranges that cross midnight
        /// (e.g. 22 -> 6) wrap around; start == end is an empty range.
        /// </summary>
        public static bool IsWithinHours(float hour, float start, float end)
        {
            hour = Mathf.Repeat(hour, 24f);
            return start <= end
                ? hour >= start && hour < end
                : hour >= start || hour < end;
        }

        void Start()
        {
            _sun = GetComponent<Light>();

            // Work on a runtime copy so play mode doesn't write tint changes back into the material asset.
            if (RenderSettings.skybox == null) return;
        
            _skybox = new Material(RenderSettings.skybox);
            RenderSettings.skybox = _skybox;
        }

        void Update()
        {
            var hours = CurrentHour;

            transform.rotation = SunRotation(hours, orbitTilt, orbitYaw);

            if (!skyGradient) return;

            var tint = SampleSkyTint(skyGradient, hours);
            _sun.color = tint;
            if (_skybox)
                _skybox.SetColor(SkyTintId, tint);
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
}
