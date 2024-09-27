namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for the settings page
/// </summary>
[TestClass]
public class Settings : TestBase
{
    /// <inheritdoc />
    protected override string PageName => "Settings";
    /// <summary>
    /// Tests the help URL
    /// </summary>
    [TestMethod]
    public Task Help()
        => FileFlows.Help.TestButton("https://fileflows.com/docs/webconsole/admin/settings");
}