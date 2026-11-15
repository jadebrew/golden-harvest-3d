using NUnit.Framework;
using World;

public class DayNightCycleHourRangeTests
{
    [TestCase(0f, true)]
    [TestCase(3.5f, true)]
    [TestCase(7.99f, true)]
    [TestCase(8f, false)]
    [TestCase(12f, false)]
    [TestCase(23.99f, false)]
    public void MidnightToEight(float hour, bool expected)
    {
        Assert.AreEqual(expected, DayNightCycle.IsWithinHours(hour, 0f, 8f));
    }

    [TestCase(21.9f, false)]
    [TestCase(22f, true)]
    [TestCase(23.5f, true)]
    [TestCase(0f, true)]
    [TestCase(5.99f, true)]
    [TestCase(6f, false)]
    [TestCase(12f, false)]
    public void RangeCrossingMidnight_Wraps(float hour, bool expected)
    {
        Assert.AreEqual(expected, DayNightCycle.IsWithinHours(hour, 22f, 6f));
    }

    [TestCase(24f, true)]
    [TestCase(26f, true)]
    [TestCase(-1f, false)]
    public void HoursOutsideTheDay_AreWrapped(float hour, bool expected)
    {
        Assert.AreEqual(expected, DayNightCycle.IsWithinHours(hour, 0f, 8f));
    }

    [Test]
    public void EqualStartAndEnd_IsEmpty()
    {
        Assert.IsFalse(DayNightCycle.IsWithinHours(8f, 8f, 8f));
    }
}
