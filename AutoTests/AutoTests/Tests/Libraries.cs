namespace FileFlowsTests.Tests;

/// <summary>
/// Library page tests
/// </summary>
public class Libraries() : TestBase("Libraries")
{
    /// <summary>
    /// Tests the initial button states in the datalist
    /// </summary>
    [Test]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
        await FileFlows.Table.ButtonDisabled("Rescan");
    }

    /// <summary>
    /// Tests the help page
    /// </summary>
    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://fileflows.com/docs/webconsole/configuration/libraries");
}