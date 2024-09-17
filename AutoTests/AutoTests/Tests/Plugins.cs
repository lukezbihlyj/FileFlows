namespace FileFlowsTests.Tests;

public class Plugins : TestBase
{
    public Plugins() : base("Plugins")
    {
    }

    [Test]
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

    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://docs.fileflows.com/plugins");

    [Test, Order(1)]
    public async Task Add()
    {
        await TableButtonClick("Add");
        await FileFlows.Table.ButtonDisabled("View", sideEditor: true);
        await FileFlows.Table.ButtonDisabled("Download", sideEditor: true);
        await Expect(Page.Locator(".vi-container .title >> text='Plugin Browser'")).ToHaveCountAsync(1);
        await FileFlows.Table.Select("Email", sideEditor: true);
        await FileFlows.Table.ButtonEnabled("View", sideEditor: true);
        await FileFlows.Table.ButtonEnabled("Download", sideEditor: true);
        await FileFlows.Table.ButtonClick("View", sideEditor: true);
        await Expect(Page.Locator(".vi-container .title >> text='Email'")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".vi-container .input-value .pre-text:has-text('This plugin allows you to send an email while executing a Flow')")).ToHaveCountAsync(1);
        await FileFlows.Editor.ButtonClick("Close");
        await FileFlows.Table.ButtonClick("Download", sideEditor: true);
        await Expect(Page.Locator(".blocker")).ToHaveCountAsync(0, new() { Timeout = 20_000 });
        await FileFlows.Editor.ButtonClick("Close");
        await Expect(Page.Locator(".blocker")).ToHaveCountAsync(0);
        await SelectItem("Email");
        await TableButtonClick("Edit");
        await SetText("SmtpServer", Environment.GetEnvironmentVariable("SmtpServer")?.EmptyAsNull() ?? "192.158.1.4");
        await SetNumber("SmtpPort", int.Parse(Environment.GetEnvironmentVariable("SmtpPort")?.EmptyAsNull() ?? "1025"));
        await SetText("Sender", "auto@fileflows.test");
        await ButtonClick("Save");
    }
    
    
    [Test, Order(2)]
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