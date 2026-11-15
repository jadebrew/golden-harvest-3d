using UnityEditor;
using UnityEngine;
using World;

namespace Editor
{
    /// <summary>
    /// Debug window for scrubbing the <see cref="DayNightCycle"/> to any hour while in play mode.
    /// </summary>
    public class TimeOfDayDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Debug/Time of Day")]
        private static void Open() => GetWindow<TimeOfDayDebugWindow>("Time of Day");

        // Keep the displayed real-time clock ticking while the window is open.
        private void Update()
        {
            if (EditorApplication.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter play mode to change the time of day.", MessageType.Info);
                return;
            }

            var cycle = FindAnyObjectByType<DayNightCycle>();
            if (!cycle)
            {
                EditorGUILayout.HelpBox("No DayNightCycle found in the scene.", MessageType.Warning);
                return;
            }

            var current = cycle.CurrentHour;
            EditorGUILayout.LabelField("Current Time", FormatHour(current), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var hour = EditorGUILayout.Slider("Hour", current, 0f, 24f);
            if (EditorGUI.EndChangeCheck())
                cycle.ClockHours = hour;

            EditorGUILayout.Space();

            var overridden = cycle.ClockHours.HasValue;
            EditorGUILayout.LabelField("Mode", overridden ? "Overridden" : "Real time");

            using (new EditorGUI.DisabledScope(!overridden))
            {
                if (GUILayout.Button("Resume Real Time"))
                    cycle.ClockHours = null;
            }
        }

        private static string FormatHour(float hours)
        {
            var totalMinutes = Mathf.FloorToInt(hours * 60f) % (24 * 60);
            return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
        }
    }
}
