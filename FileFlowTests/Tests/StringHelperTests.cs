using FileFlows.Plugin.Helpers;

namespace FileFlowTests.Tests;

/// <summary>
/// Contains unit tests for the <see cref="StringHelper"/> class.
/// </summary>
[TestClass]
public class StringHelperTests : TestBase
{
    /// <summary>
    /// The string helper instance to use
    /// </summary>
    private StringHelper _stringHelper;

    /// <inheritdoc />
    protected override void TestStarting()
    {
        _stringHelper = new StringHelper(Logger);
    }

    /// <summary>
    /// Test method for exact match.
    /// </summary>
    [TestMethod]
    public void Matches_ExactMatch_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("abc", "abc"));
        Assert.IsFalse(_stringHelper.Matches("abc", "xyz"));
    }

    /// <summary>
    /// Test method for contains.
    /// </summary>
    [TestMethod]
    public void Matches_Contains_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("*abc*", "xyzabcxyz"));
        Assert.IsFalse(_stringHelper.Matches("*abc*", "xyzxyz"));
    }

    /// <summary>
    /// Test method for starts with.
    /// </summary>
    [TestMethod]
    public void Matches_StartsWith_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("abc*", "abcdef"));
        Assert.IsFalse(_stringHelper.Matches("abc*", "xyzabc"));
    }

    /// <summary>
    /// Test method for ends with.
    /// </summary>
    [TestMethod]
    public void Matches_EndsWith_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("*abc", "xyzabc"));
        Assert.IsFalse(_stringHelper.Matches("*abc", "abcxyz"));
    }

    /// <summary>
    /// Test method for negated exact match.
    /// </summary>
    [TestMethod]
    public void Matches_NegatedExactMatch_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("!abc", "xyz"));
        Assert.IsFalse(_stringHelper.Matches("!abc", "abc"));
    }

    /// <summary>
    /// Test method for negated contains.
    /// </summary>
    [TestMethod]
    public void Matches_NegatedContains_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("!*abc*", "xyzxyz"));
        Assert.IsFalse(_stringHelper.Matches("!*abc*", "xyzabcxyz"));
    }

    /// <summary>
    /// Test method for negated starts with.
    /// </summary>
    [TestMethod]
    public void Matches_NegatedStartsWith_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("!*abc", "xyzxyz"));
        Assert.IsFalse(_stringHelper.Matches("!abc*", "abcdef"));
    }

    /// <summary>
    /// Test method for negated ends with.
    /// </summary>
    [TestMethod]
    public void Matches_NegatedEndsWith_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("!*abc", "abcxyz"));
        Assert.IsFalse(_stringHelper.Matches("!*abc", "xyzabc"));
    }

    /// <summary>
    /// Test method for regex match.
    /// </summary>
    [TestMethod]
    public void Matches_RegexMatch_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("/^abc$/", "abc")); // Regex pattern
        Assert.IsFalse(_stringHelper.Matches("/[invalid/", "abc")); // Invalid regex pattern
    }

    /// <summary>
    /// Test method for regex negation.
    /// </summary>
    [TestMethod]
    public void Matches_RegexNegation_ReturnsTrue()
    {
        Assert.IsTrue(_stringHelper.Matches("!/^abc$/", "xyz")); // Regex negation
        Assert.IsFalse(_stringHelper.Matches("!/^abc$/", "abc")); // Regex negation
    }
}