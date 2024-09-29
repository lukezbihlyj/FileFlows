namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for the nodes page
/// </summary>
//[TestClass]
public class Nodes : TestBase
{
    /// <inheritdoc />
    protected override string PageName => "Nodes";
    /// <summary>
    /// Tests the intiial button states in the datalist
    /// </summary>
    [Test, Order(1)]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
    }

    /// <summary>
    /// Tests the help page
    /// </summary>
    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://fileflows.com/docs/webconsole/configuration/nodes");

    /// <summary>
    /// Tests a user can download the node zip file
    /// </summary>
    [Test]
    public async Task Download()
    {
        // Start the task of waiting for the download
        var waitForDownloadTask = Page.WaitForDownloadAsync();
        // Perform the action that initiates download
        await Page.Locator("a.btn >> text='Download Node'").ClickAsync();
        // Wait for the download process to complete
        var download = await waitForDownloadTask;
        Logger.ILog(await download.PathAsync() ?? "Path is null");
        // Save downloaded file somewhere
        string file = Path.GetTempFileName() + ".zip";
        await download.SaveAsAsync(file);
        ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Read);
        bool dll = zip.Entries.Any(x => x.Name == "FileFlows.Node.dll");
        ClassicAssert.IsTrue(dll);
    }
    
    /// <summary>
    /// Tests the internal node cannot be deleted
    /// </summary>
    [Test]
    public async Task CannotDeleteInternalNode()
    {
        await FileFlows.Table.Select("Internal Processing Node");
        await FileFlows.Table.ButtonClick("Delete");
        await FileFlows.MessageBox.Exist("Remove", "Are you sure you want to delete the selected item?");
        await FileFlows.MessageBox.ButtonClick("Yes");
        await FileFlows.Toast.Error("You cannot delete the internal processing node.");
    }
}