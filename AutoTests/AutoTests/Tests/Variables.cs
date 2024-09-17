namespace FileFlowsTests.Tests;

public class Variables : TestBase
{
    public Variables() : base("Variables")
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
        => FileFlows.Help.TestDatalistButton("https://docs.fileflows.com/variables");

    [Test]
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