namespace FileFlowsTests.Tests;

public class Flows : TestBase
{
    public Flows() : base("Flows")
    {
    }
    
    
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

    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://docs.fileflows.com/flows");
}