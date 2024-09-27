namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for the plugins page
/// </summary>
[TestClass]
public class Plugins : TestBase
{
    /// <inheritdoc />
    protected override string PageName => "Plugins";
    /// <summary>
    /// Tests the inital button states
    /// </summary>
    [TestMethod, Priority(1)]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
        await FileFlows.Table.ButtonDisabled("Update");
        await FileFlows.Table.ButtonDisabled("Used By");
        await FileFlows.Table.ButtonDisabled("About");
    }

    /// <summary>
    /// Tests the help page
    /// </summary>
    [TestMethod, Priority(2)]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://fileflows.com/docs/webconsole/extensions/plugins");

    /// <summary>
    /// Tests adding a plugin
    /// </summary>
    [TestMethod, Priority(3)]
    public async Task Add()
    {
        await TableButtonClick("Add");
        await FileFlows.Table.ButtonDisabled("View", sideEditor: true);
        await FileFlows.Table.ButtonDisabled("Download", sideEditor: true);
        await Expect(Page.Locator(".vi-container .title >> text='Plugin Browser'")).ToHaveCountAsync(1);
        await FileFlows.Table.Select("Pushover", sideEditor: true);
        await FileFlows.Table.ButtonEnabled("View", sideEditor: true);
        await FileFlows.Table.ButtonEnabled("Download", sideEditor: true);
        await FileFlows.Table.ButtonClick("View", sideEditor: true);
        await Expect(Page.Locator(".vi-container .title >> text='Pushover'")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".vi-container .input-value .pre-text:has-text('Lets you send Pushover messages to a server')")).ToHaveCountAsync(1);
        await FileFlows.Editor.ButtonClick("Close");
        await FileFlows.Table.ButtonClick("Download", sideEditor: true);
        await Expect(Page.Locator(".blocker")).ToHaveCountAsync(0, new() { Timeout = 20_000 });
        await FileFlows.Editor.ButtonClick("Close");
        await Expect(Page.Locator(".blocker")).ToHaveCountAsync(0);
        await SelectItem("Pushover");
        await TableButtonClick("Edit");
        await SetText("UserKey", Environment.GetEnvironmentVariable("PushoverUserKey")?.EmptyAsNull() ?? "192.158.1.4");
        await SetText("ApiToken", Environment.GetEnvironmentVariable("PushoverApiToken")?.EmptyAsNull() ?? "1025");
        await ButtonClick("Save");
    }
    
    /// <summary>
    /// Tests using the email task
    /// </summary>
    [TestMethod, Priority(4)]
    public async Task EmailTask()
    {
        string name = RandomName("EmailTask");
        await GotoPage("Tasks");
        await TableButtonClick("Add");
        await SetText("Name", name);
        await SetSelect("Type", "File Processed");
        await ButtonClick("Save");
    }
}