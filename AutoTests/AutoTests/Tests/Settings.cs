namespace FileFlowsTests.Tests;

public class Settings : TestBase
{
    public Settings() : base("Settings")
    {
    }

    [Test]
    public Task Help()
        => FileFlows.Help.TestButton("https://docs.fileflows.com/settings");
}