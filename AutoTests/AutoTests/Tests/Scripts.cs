namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for the script page
/// </summary>
[TestClass]
public class Scripts: TestBase
{
    /// <inheritdoc />
    protected override string PageName => "Scripts";
    /// <summary>
    /// Tests the intial button states of the datalist
    /// </summary>
    [TestMethod]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Repository");
        await FileFlows.Table.ButtonEnabled("Import");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
        await FileFlows.Table.ButtonDisabled("Update");
        await FileFlows.Table.ButtonDisabled("Export");
        await FileFlows.Table.ButtonDisabled("Duplicate");
        await FileFlows.Table.ButtonDisabled("Used By");
    }

    /// <summary>
    /// Tests the help button
    /// </summary>
    [TestMethod]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://fileflows.com/docs/webconsole/extensions/scripts");

    /// <summary>
    /// Tests importing and exporting a script
    /// </summary>
    [TestMethod]
    public async Task ImportExport()
    {
        string name = Guid.NewGuid().ToString();
        string tempFile = Path.Combine(Path.GetTempPath(), name + ".js");
        string js = $@"
/**
 * @name {name}
 * @description Description of this script
 * @param {{int}} NumberParameter Description of this input
 * @output Description of output 1
 * @output Description of output 2
 */
function Script(NumberParameter)
{{
    return 1;
}}
".Trim();
        await File.WriteAllTextAsync(tempFile, js);
        
        await TableButtonClick("Import");
        var btnImport = Page.Locator(".flow-modal-footer button >> text=Import");
        await Expect(btnImport).ToBeDisabledAsync();
        await SetInputFile(tempFile);
        await Expect(btnImport).ToBeEnabledAsync();
        await btnImport.ClickAsync();

        await SelectItem(name);
        
        // Start the task of waiting for the download
        var waitForDownloadTask = Page.WaitForDownloadAsync();
        // Perform the action that initiates download
        await TableButtonClick("Export");
        // Wait for the download process to complete
        var download = await waitForDownloadTask;
        Logger.ILog(await download.PathAsync() ?? "No path");
        // Save downloaded file somewhere
        string file = Path.GetTempFileName() + ".js";
        await download.SaveAsAsync(file);

        string dlContent = await File.ReadAllTextAsync(file);
        Assert.AreEqual(js, dlContent);
    }
    
    
    /// <summary>
    /// Tests an invalid script is rejected
    /// </summary>
    [TestMethod]
    public async Task InvalidScript()
    {
        await TableButtonClick("Add");
        await Page.Locator(".flow-modal .flow-modal-footer button >> text=Next").ClickAsync();
        await SetText("Name", "Invalid Script");
        await TestCode(@"
function NotValid() { 
    return 1; 
}", "Failed to locate comment section");

        await TestCode(@"
/**
 * Not valid
 */
function NotValid() { 
    return 1; 
}", "No comment parameters found");
        
        await TestCode(@"
/**
 * Not valid
 */
function NotValid() { 
    return 1; 
}", "No comment parameters found");

        async Task TestCode(string code, string error)
        {
            await SetCode(code.Trim());
            await ButtonClick("Save");
            await Expect(Page.Locator($".toast-message .message:has-text(\"{error}\")")).ToHaveCountAsync(1);
        }
    }

    /// <summary>
    /// Tests a basic script is accepted
    /// </summary>
    [TestMethod]
    public async Task BasicScript()
    {
        await TableButtonClick("Add");
        await Page.Locator(".flow-modal .flow-modal-footer button >> text=Next").ClickAsync();
        string name = Guid.NewGuid().ToString();
        await SetText("Name", name);
        await SetCode(@"
/**
 * Valid script
 * @output the output
 */
function Script() { 
    return 1; 
}");
        await ButtonClick("Save");
        await SelectItem(name);
    }
    
    /// <summary>
    /// Tests the script repository
    /// </summary>
    [TestMethod]
    public async Task ScriptRepository()
    {
        await TableButtonClick("Repository");
        await SelectItem("Video - Resolution", sideEditor: true);
        await TableButtonClick("Download", sideEditor: true);
        await FileFlows.Editor.ButtonClick("Close");
        await SelectItem("Video - Resolution");
    }
}