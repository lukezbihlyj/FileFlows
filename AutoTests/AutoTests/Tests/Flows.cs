namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for the flows page
/// </summary>
//[TestClass]
public class Flows : TestBase
{
    /// <inheritdoc />
    protected override string PageName => "Flows";
    
    /// <summary>
    /// Tests the initial button states
    /// </summary>
    [Test]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Import");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
        await FileFlows.Table.ButtonDisabled("Duplicate");
        await FileFlows.Table.ButtonDisabled("Export");
        await FileFlows.Table.ButtonDisabled("Used By");
        await FileFlows.Table.ButtonDisabled("Revisions");
    }

    /// <summary>
    /// Tests the help
    /// </summary>
    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://fileflows.com/docs/webconsole/configuration/flows");
}