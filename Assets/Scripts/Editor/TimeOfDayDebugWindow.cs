using System;
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

            var cycle = DayNightCycle.Instance;
            if (!cycle)
            {
                EditorGUILayout.HelpBox("No DayNightCycle found in the scene.", MessageType.Warning);
                return;
            }

            var current = cycle.CurrentHour;
            EditorGUILayout.LabelField("Current Time", FormatHour(current), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var hour = EditorGUILayout.Slider("Hour", current, 0f, 24f);

            // The slider consumes MouseDown/MouseUp, so track hotControl instead: it stays
            // non-zero for the whole drag, even if the mouse is released outside the window.
            cycle.IsScrubbing = GUIUtility.hotControl != 0;

            if (EditorGUI.EndChangeCheck())
                cycle.ClockHours = hour;

            EditorGUI.BeginChangeCheck();
            var day = (DayOfWeek)EditorGUILayout.EnumPopup("Day", cycle.CurrentDay);
            if (EditorGUI.EndChangeCheck())
                cycle.ClockDay = day;

            EditorGUILayout.Space();

            var overridden = cycle.ClockHours.HasValue || cycle.ClockDay.HasValue;
            EditorGUILayout.LabelField("Mode", overridden ? "Overridden" : "Real time");

            using (new EditorGUI.DisabledScope(!overridden))
            {
                if (GUILayout.Button("Resume Real Time"))
                {
                    cycle.ClockHours = null;
                    cycle.ClockDay = null;
                }
            }
        }

        private static string FormatHour(float hours)
        {
            var totalMinutes = Mathf.FloorToInt(hours * 60f) % (24 * 60);
            return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
        }
    }
}
