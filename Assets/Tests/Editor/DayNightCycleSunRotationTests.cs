using NUnit.Framework;
using UnityEngine;
using World;

public class DayNightCycleSunRotationTests
{
    private const float DefaultTilt = 30f;
    private const float DefaultYaw = -37f;
    private const float AngleTolerance = 0.01f;

    private static Vector3 LightDirection(float hours, float tilt = DefaultTilt, float yaw = DefaultYaw) =>
        DayNightCycle.SunRotation(hours, tilt, yaw) * Vector3.forward;

    // Vector3.Angle uses acos, which loses some float precision near 0° and 180°, atan2 does not.
    private static float AngleBetween(Vector3 a, Vector3 b) =>
        Mathf.Atan2(Vector3.Cross(a, b).magnitude, Vector3.Dot(a, b)) * Mathf.Rad2Deg;

    // Degrees the sun is above the horizon. The light shines from the sun, so a light
    // pointing down (negative y) means the sun is up.
    private static float SunElevation(float hours, float tilt = DefaultTilt, float yaw = DefaultYaw)
    {
        var d = LightDirection(hours, tilt, yaw);
        return Mathf.Atan2(-d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
    }

    [TestCase(6f)]
    [TestCase(18f)]
    public void SunriseAndSunset_AreOnTheHorizon(float hours)
    {
        Assert.AreEqual(0f, SunElevation(hours), AngleTolerance);
    }

    [Test]
    public void SunriseAndSunset_AreOnOppositeSides()
    {
        var sunrise = LightDirection(6f);
        var sunset = LightDirection(18f);
        Assert.AreEqual(180f, AngleBetween(sunrise, sunset), AngleTolerance);
    }

    [TestCase(0f)]
    [TestCase(15f)]
    [TestCase(30f)]
    [TestCase(45f)]
    [TestCase(60f)]
    public void Noon_ElevationIsNinetyMinusTilt(float tilt)
    {
        Assert.AreEqual(90f - tilt, SunElevation(12f, tilt), AngleTolerance);
    }

    [Test]
    public void Noon_WithNoTilt_PointsStraightDown()
    {
        Assert.AreEqual(0f, AngleBetween(Vector3.down, LightDirection(12f, tilt: 0f)), AngleTolerance);
    }

    [Test]
    public void Midnight_SunIsMirroredBelowTheHorizon()
    {
        Assert.AreEqual(-(90f - DefaultTilt), SunElevation(0f), AngleTolerance);
    }

    [TestCase(7f)]
    [TestCase(9.5f)]
    [TestCase(14f)]
    [TestCase(17f)]
    public void Daytime_SunIsAboveHorizon(float hours)
    {
        Assert.Greater(SunElevation(hours), 0f);
    }

    [TestCase(19f)]
    [TestCase(22.5f)]
    [TestCase(2f)]
    [TestCase(5f)]
    public void Nighttime_SunIsBelowHorizon(float hours)
    {
        Assert.Less(SunElevation(hours), 0f);
    }

    [Test]
    public void FullDay_WrapsWithoutJump()
    {
        var midnight = DayNightCycle.SunRotation(0f, DefaultTilt, DefaultYaw);
        var endOfDay = DayNightCycle.SunRotation(24f, DefaultTilt, DefaultYaw);
        Assert.AreEqual(0f, Quaternion.Angle(midnight, endOfDay), AngleTolerance);
    }

    [Test]
    public void Rotation_MovesSmoothlyFifteenDegreesPerHour()
    {
        // 360° / 24h = 15°/h, i.e. 0.25° per minute. Check every minute of the day.
        const float minute = 1f / 60f;
        for (var m = 0; m < 24 * 60; m++)
        {
            var hours = m * minute;
            var a = LightDirection(hours);
            var b = LightDirection(hours + minute);
            var step = AngleBetween(a, b);
            Assert.AreEqual(0.25f, step, AngleTolerance, $"Step at {hours:F2}h");
        }
    }

    [TestCase(6f)]
    [TestCase(12f)]
    [TestCase(21f)]
    public void Yaw_RotatesOrbitAroundVerticalWithoutChangingElevation(float hours)
    {
        var unrotated = LightDirection(hours, yaw: 0f);
        var rotated = LightDirection(hours, yaw: 90f);

        Assert.AreEqual(SunElevation(hours, yaw: 0f), SunElevation(hours, yaw: 90f), AngleTolerance);
        Assert.AreEqual(0f, AngleBetween(Quaternion.AngleAxis(90f, Vector3.up) * unrotated, rotated), AngleTolerance);
    }
}
