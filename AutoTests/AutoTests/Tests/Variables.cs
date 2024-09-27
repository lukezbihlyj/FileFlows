namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for the variables page
/// </summary>
[TestClass]
public class Variables : TestBase
{
    /// <inheritdoc />
    protected override string PageName => "Variables";
    /// <summary>
    /// Tests the initial button states
    /// </summary>
    [TestMethod]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
    }

    /// <summary>
    /// Tests the help
    /// </summary>
    [TestMethod]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://fileflows.com/docs/webconsole/extensions/variables");

    /// <summary>
    /// Tests adding/editing/deleting a variable
    /// </summary>
    [TestMethod]
    public async Task AddEditDelete()
    {
        string name = Guid.NewGuid().ToString();
        string value = Guid.NewGuid().ToString();
        await TableButtonClick("Add");
        await SetText("Name", name);
        await SetTextArea("Value", value);
        await ButtonClick("Save");
        await DoubleClickItem(name);
        await Expect(Page.Locator("div[x-id='Value'] textarea")).ToHaveValueAsync(value);
        string newName = Guid.NewGuid().ToString();
        await SetText("Name", newName);
        await ButtonClick("Save");
        await Task.Delay(1000);
        await SelectItem(newName);
        await TableButtonClick("Delete");
        await MessageBoxExists("Remove", "Are you sure you want to delete the selected item?");
        await MessageBoxButton("Yes");
        await ItemDoesNotExist(name);
        await ItemDoesNotExist(newName);
    }
}