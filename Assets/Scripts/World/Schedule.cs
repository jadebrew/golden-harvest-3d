using System;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    /// <summary>What a <see cref="Character"/> is supposed to be doing at a given time.</summary>
    public enum Activity
    {
        Sleep,
        Shipping,
        Breakfast,
        Work,
        Lunch,
        Dinner,
        Leisure,
        Party,
        Supernatural,
    }

    [Flags]
    public enum Days
    {
        None = 0,
        Monday = 1 << 0,
        Tuesday = 1 << 1,
        Wednesday = 1 << 2,
        Thursday = 1 << 3,
        Friday = 1 << 4,
        Saturday = 1 << 5,
        Sunday = 1 << 6,
        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
        Weekend = Saturday | Sunday,
        Everyday = Weekdays | Weekend,
    }

    [Serializable]
    public struct ScheduleBlock
    {
        public Days days;
        [Range(0f, 24f)] public float startHour;
        [Tooltip("Exclusive. Use 24 for midnight; a block may cross midnight (e.g. 22 -> 2).")]
        [Range(0f, 24f)] public float endHour;
        public Activity activity;

        public ScheduleBlock(Days days, float startHour, float endHour, Activity activity)
        {
            this.days = days;
            this.startHour = startHour;
            this.endHour = endHour;
            this.activity = activity;
        }
    }

    /// <summary>
    /// A weekly timetable of <see cref="Activity"/> blocks. Any time not covered by a block is <see cref="Activity.Sleep"/>.
    /// New assets start out with the village's default timetable.
    /// </summary>
    [CreateAssetMenu(fileName = "Schedule", menuName = "Scriptable Objects/Schedule")]
    public class Schedule : ScriptableObject
    {
        [Tooltip("Checked top to bottom; the first block that covers the time wins.")]
        [SerializeField] private List<ScheduleBlock> blocks = DefaultBlocks();

        public static Schedule CreateDefault()
        {
            var schedule = CreateInstance<Schedule>();
            schedule.name = "Default Schedule";
            return schedule;
        }

        public Activity GetActivity(DayOfWeek day, float hour)
        {
            var dayFlag = ToDays(day);
            foreach (var block in blocks)
            {
                if ((block.days & dayFlag) != 0 &&
                    DayNightCycle.IsWithinHours(hour, block.startHour, block.endHour))
                {
                    return block.activity;
                }
            }
            return Activity.Sleep;
        }

        public static Days ToDays(DayOfWeek day) =>
            day == DayOfWeek.Sunday ? Days.Sunday : (Days)(1 << ((int)day - 1));

        private static List<ScheduleBlock> DefaultBlocks() => new()
        {
            new(Days.Weekdays, 6, 7, Activity.Shipping),
            new(Days.Everyday, 7, 9, Activity.Breakfast),
            new(Days.Weekdays, 9, 12, Activity.Work),
            new(Days.Weekend, 9, 12, Activity.Leisure),
            new(Days.Everyday, 12, 13, Activity.Lunch),
            new(Days.Weekdays, 13, 17, Activity.Work),
            new(Days.Weekend, 13, 17, Activity.Leisure),
            new(Days.Everyday, 17, 18, Activity.Dinner),
            new(Days.Wednesday | Days.Friday | Days.Sunday, 18, 24, Activity.Party),
            new(Days.Everyday, 18, 24, Activity.Leisure),
            new(Days.Everyday, 3, 4, Activity.Supernatural),
        };
    }
}
