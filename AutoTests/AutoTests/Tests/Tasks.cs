namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for the tasks page
/// </summary>
public class Tasks() : TestBase("Tasks")
{
    /// <summary>
    /// Tests the initial button states
    /// </summary>
    [Test]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
    }

    /// <summary>
    /// Tests the help page
    /// </summary>
    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://fileflows.com/docs/webconsole/system/tasks");

    /// <summary>
    /// Tests adding/editing/deleting a task
    /// </summary>
    [Test]
    public async Task AddEditDelete()
    {
        string testScript = Guid.NewGuid().ToString();
        await GotoPage("Scripts");
        await SkyBox("System Scripts");
        await TableButtonClick("Add");
        await Page.Locator(".flow-modal .flow-modal-footer button >> text=Next").ClickAsync();
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