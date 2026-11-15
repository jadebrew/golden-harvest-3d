using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using World;

public class DayNightCycleSkyTintTests
{
    private const string GradientPath = "Assets/Art/skybox/skybox_gradient.png";
    private const float Tolerance = 1e-4f;

    private Texture2D gradient;

    [SetUp]
    public void SetUp()
    {
        // Synthetic 24x1 gradient where pixel h has a unique, easily predicted color.
        // Float format so no 8-bit quantization muddies exact comparisons.
        gradient = new Texture2D(24, 1, TextureFormat.RGBAFloat, false, true);
        for (var h = 0; h < 24; h++)
            gradient.SetPixel(h, 0, HourColor(h));
        gradient.Apply();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gradient);
    }

    private static Color HourColor(int hour) => new Color(hour / 24f, 1f - hour / 24f, 0.5f, 1f);

    private static void AssertColor(Color expected, Color actual)
    {
        Assert.AreEqual(expected.r, actual.r, Tolerance, "r");
        Assert.AreEqual(expected.g, actual.g, Tolerance, "g");
        Assert.AreEqual(expected.b, actual.b, Tolerance, "b");
        Assert.AreEqual(expected.a, actual.a, Tolerance, "a");
    }

    [Test]
    public void WholeHour_ReturnsThatHoursPixel([NUnit.Framework.Range(0, 23)] int hour)
    {
        AssertColor(HourColor(hour), DayNightCycle.SampleSkyTint(gradient, hour));
    }

    [TestCase(0.5f, 0, 1, 0.5f)]
    [TestCase(6.25f, 6, 7, 0.25f)]
    [TestCase(12.75f, 12, 13, 0.75f)]
    [TestCase(18.5f, 18, 19, 0.5f)]
    public void BetweenHours_BlendsWithNextHour(float hours, int from, int to, float t)
    {
        var expected = Color.Lerp(HourColor(from), HourColor(to), t);
        AssertColor(expected, DayNightCycle.SampleSkyTint(gradient, hours));
    }

    [Test]
    public void LateEvening_WrapsAroundToMidnight()
    {
        var expected = Color.Lerp(HourColor(23), HourColor(0), 0.5f);
        AssertColor(expected, DayNightCycle.SampleSkyTint(gradient, 23.5f));
    }

    [Test]
    public void TwentyFourHours_EqualsMidnight()
    {
        AssertColor(HourColor(0), DayNightCycle.SampleSkyTint(gradient, 24f));
    }

    [Test]
    public void RealGradientAsset_IsReadableAndMatchesPixelPerHour()
    {
        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(GradientPath);
        Assert.IsNotNull(asset, $"Missing {GradientPath}");
        Assert.IsTrue(asset.isReadable, "Gradient must have Read/Write enabled to be sampled at runtime (linux)");
        Assert.AreEqual(24, asset.width, "Gradient should be 24 pixels wide (one per hour)");
        Assert.AreEqual(1, asset.height, "Gradient should be 1 pixel tall");

        for (var hour = 0; hour < 24; hour++)
            AssertColor(asset.GetPixel(hour, 0), DayNightCycle.SampleSkyTint(asset, hour));
    }
}
