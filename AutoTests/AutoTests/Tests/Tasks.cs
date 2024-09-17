namespace FileFlowsTests.Tests;

public class Tasks : TestBase
{
    public Tasks() : base("Tasks")
    {
    }

    [Test]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
    }

    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://docs.fileflows.com/tasks");

    [Test]
    public async Task AddEditDelete()
    {
        string testScript = Guid.NewGuid().ToString();
        await GotoPage("Scripts");
        await SkyBox("System Scripts");
        await TableButtonClick("Add");
        await SetText("Name", testScript);
        await ButtonClick("Save");
        await SelectItem(testScript);

        await GotoPage("Tasks");
        
        string name = Guid.NewGuid().ToString();
        await TableButtonClick("Add");
        await SetText("Name", name);
        await SetSelect("Script", testScript);
        await ButtonClick("Save");
        
        await DoubleClickItem(name);
        string newName = Guid.NewGuid().ToString();
        await SetText("Name", newName);
        await ButtonClick("Save");
        await Task.Delay(250);
        await SelectItem(newName);

        await TableButtonClick("Delete");
        await MessageBoxExists("Remove", "Are you sure you want to delete the selected item?");
        await MessageBoxButton("Yes");
        await ItemDoesNotExist(name);
        await ItemDoesNotExist(newName);
    }
}