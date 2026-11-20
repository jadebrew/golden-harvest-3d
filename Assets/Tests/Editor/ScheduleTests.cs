using System;
using NUnit.Framework;
using UnityEngine;
using World;
using Object = UnityEngine.Object;

public class ScheduleTests
{
    private Schedule _schedule;

    [SetUp]
    public void SetUp() => _schedule = Schedule.CreateDefault();

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_schedule);

    [TestCase(DayOfWeek.Monday, 6.5f, Activity.Shipping)]
    [TestCase(DayOfWeek.Monday, 7f, Activity.Breakfast)]
    [TestCase(DayOfWeek.Monday, 9f, Activity.Work)]
    [TestCase(DayOfWeek.Monday, 12.5f, Activity.Lunch)]
    [TestCase(DayOfWeek.Monday, 16.99f, Activity.Work)]
    [TestCase(DayOfWeek.Monday, 17f, Activity.Dinner)]
    [TestCase(DayOfWeek.Monday, 18f, Activity.Leisure)]
    [TestCase(DayOfWeek.Monday, 23.99f, Activity.Leisure)]
    [TestCase(DayOfWeek.Monday, 0f, Activity.Sleep)]
    [TestCase(DayOfWeek.Monday, 3.5f, Activity.Supernatural)]
    [TestCase(DayOfWeek.Monday, 5f, Activity.Sleep)]
    [TestCase(DayOfWeek.Friday, 10f, Activity.Work)]
    public void Weekday(DayOfWeek day, float hour, Activity expected)
    {
        Assert.AreEqual(expected, _schedule.GetActivity(day, hour));
    }

    [TestCase(DayOfWeek.Saturday, 6.5f, Activity.Sleep)]
    [TestCase(DayOfWeek.Saturday, 10f, Activity.Leisure)]
    [TestCase(DayOfWeek.Sunday, 12f, Activity.Lunch)]
    [TestCase(DayOfWeek.Sunday, 14f, Activity.Leisure)]
    [TestCase(DayOfWeek.Sunday, 3f, Activity.Supernatural)]
    public void Weekend(DayOfWeek day, float hour, Activity expected)
    {
        Assert.AreEqual(expected, _schedule.GetActivity(day, hour));
    }

    [TestCase(DayOfWeek.Wednesday, Activity.Party)]
    [TestCase(DayOfWeek.Friday, Activity.Party)]
    [TestCase(DayOfWeek.Sunday, Activity.Party)]
    [TestCase(DayOfWeek.Tuesday, Activity.Leisure)]
    [TestCase(DayOfWeek.Saturday, Activity.Leisure)]
    public void Evening(DayOfWeek day, Activity expected)
    {
        Assert.AreEqual(expected, _schedule.GetActivity(day, 20f));
    }

    [TestCase(DayOfWeek.Monday, Days.Monday)]
    [TestCase(DayOfWeek.Saturday, Days.Saturday)]
    [TestCase(DayOfWeek.Sunday, Days.Sunday)]
    public void ToDays_MapsEachDayToItsFlag(DayOfWeek day, Days expected)
    {
        Assert.AreEqual(expected, Schedule.ToDays(day));
    }
}
