namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for the settings page
/// </summary>
public class Settings() : TestBase("Settings")
{
    /// <summary>
    /// Tests the help URL
    /// </summary>
    [Test]
    public Task Help()
        => FileFlows.Help.TestButton("https://fileflows.com/docs/webconsole/admin/settings");
}