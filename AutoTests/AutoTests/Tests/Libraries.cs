namespace FileFlowsTests.Tests;

public class Libraries : TestBase
{
    public Libraries() : base("Libraries")
    {
    }
    
    
    [Test]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
        await FileFlows.Table.ButtonDisabled("Rescan");
    }

    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://docs.fileflows.com/libraries");
}