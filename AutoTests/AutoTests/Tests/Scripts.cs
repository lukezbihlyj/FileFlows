namespace FileFlowsTests.Tests;

public class Scripts: TestBase
{
    public Scripts() : base("Scripts")
    {
    }

    [Test]
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

    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://docs.fileflows.com/scripts");

    [Test]
    public async Task ImportExport()
    {
        string name = Guid.NewGuid().ToString();
        string tempFile = Path.Combine(Path.GetTempPath(), name + ".js");
        string js = @"
/**
 * Description of this script
 * @param {int} NumberParameter Description of this input
 * @output Description of output 1
 * @output Description of output 2
 */
function Script(NumberParameter)
{
    return 1;
}
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
        Console.WriteLine(await download.PathAsync());
        // Save downloaded file somewhere
        string file = Path.GetTempFileName() + ".js";
        await download.SaveAsAsync(file);

        string dlContent = await File.ReadAllTextAsync(file);
        Assert.AreEqual(js, dlContent);
    }
    
    
    [Test]
    public async Task InvalidScript()
    {
        await TableButtonClick("Add");
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
}", "No output parameters found");
        
        await TestCode(@"
/**
 * Not valid
 */
function NotValid() { 
    return 1; 
}", "No output parameters found");

        async Task TestCode(string code, string error)
        {
            await SetCode(code.Trim());
            await ButtonClick("Save");
            await Expect(Page.Locator($".error-text:has-text(\"{error}\")")).ToHaveCountAsync(1);
        }
    }

    [Test]
    public async Task BasicScript()
    {
        await TableButtonClick("Add");
        string name = Guid.NewGuid().ToString();
        await SetText("Name", name);
        await SetCode(@"
/**
 * Not valid
 * @output the output
 */
function Script() { 
    return 1; 
}");
        await ButtonClick("Save");
        await SelectItem(name);
    }
    
    [Test]
    public async Task ScriptRepository()
    {
        await TableButtonClick("Repository");
        await SelectItem("Video - Resolution", sideEditor: true);
        await TableButtonClick("Download", sideEditor: true);
        await SelectItem("Video - Resolution");
    }
}