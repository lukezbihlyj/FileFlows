using FileFlows.Plugin.Helpers;

namespace FileFlowTests.Tests;

/// <summary>
/// Math Helper tests
/// </summary>
[TestClass]
public class MathHelperTests : TestBase
{
    private MathHelper _mathHelper;

    /// <inheritdoc />
    protected override void TestStarting()
    {
        _mathHelper = new MathHelper(Logger);
    }


    /// <summary>
    /// Basic equals test
    /// </summary>
    [TestMethod]
    public void Test_BasicEqual()
    {
        Assert.IsTrue(_mathHelper.IsTrue("=2", 2));
        Assert.IsFalse(_mathHelper.IsTrue("=2", 3));
    }

    /// <summary>
    /// Less than test
    /// </summary>
    [TestMethod]
    public void Test_LessThan()
    {
        Assert.IsTrue(_mathHelper.IsTrue("<3", 2));
        Assert.IsFalse(_mathHelper.IsTrue("<3", 4));
        Assert.IsFalse(_mathHelper.IsTrue("<3", 3));
    }

    /// <summary>
    /// Greater than test
    /// </summary>
    [TestMethod]
    public void Test_GreaterThan()
    {
        Assert.IsTrue(_mathHelper.IsTrue(">2", 3));
        Assert.IsFalse(_mathHelper.IsTrue(">2", 1));
        Assert.IsFalse(_mathHelper.IsTrue(">2", 2));
    }

    /// <summary>
    /// Less than or equal test
    /// </summary>
    [TestMethod]
    public void Test_LessThanOrEqual()
    {
        Assert.IsTrue(_mathHelper.IsTrue("<=5", 5));
        Assert.IsTrue(_mathHelper.IsTrue("<=5", 4));
        Assert.IsFalse(_mathHelper.IsTrue("<=5", 6));
    }

    /// <summary>
    /// Greater than or equal test
    /// </summary>
    [TestMethod]
    public void Test_GreaterThanOrEqual()
    {
        Assert.IsTrue(_mathHelper.IsTrue(">=5", 5));
        Assert.IsTrue(_mathHelper.IsTrue(">=5", 6));
        Assert.IsFalse(_mathHelper.IsTrue(">=5", 4));
    }

    /// <summary>
    /// Between test
    /// </summary>
    [TestMethod]
    public void Test_Between()
    {
        Assert.IsTrue(_mathHelper.IsTrue("1><5", 3));
        Assert.IsFalse(_mathHelper.IsTrue("1><5", 0));
    }

    /// <summary>
    /// Not between test
    /// </summary>
    [TestMethod]
    public void Test_NotBetween()
    {
        Assert.IsTrue(_mathHelper.IsTrue("1<>5", 0));
        Assert.IsFalse(_mathHelper.IsTrue("1<>5", 3));
    }

    /// <summary>
    /// Not equal test
    /// </summary>
    [TestMethod]
    public void Test_NotEqual()
    {
        Assert.IsTrue(_mathHelper.IsTrue("!=5", 4));
        Assert.IsFalse(_mathHelper.IsTrue("!=5", 5));
    }

    /// <summary>
    /// Test with tolerance
    /// </summary>
    [TestMethod]
    public void Test_EqualWithTolerance()
    {
        Assert.IsTrue(_mathHelper.IsTrue("==5", 5.005)); // within tolerance of 0.01
        Assert.IsFalse(_mathHelper.IsTrue("==5", 5.02)); // outside tolerance
    }

    /// <summary>
    /// Test comparison string with units
    /// </summary>
    [TestMethod]
    public void Test_AdjustComparisonValueWithUnits()
    {
        Assert.IsTrue(_mathHelper.IsTrue("=500mbps", 500_000_000));
        Assert.IsTrue(_mathHelper.IsTrue("=5mb", 5_000_000));
    }
}