namespace FileFlowsTests.Tests;

public class Nodes : TestBase
{
    public Nodes() : base("Nodes")
    {
    }

    [Test, Order(1)]
    public async Task InitialButtonStates()
    {
        await FileFlows.Table.ButtonEnabled("Add");
        await FileFlows.Table.ButtonEnabled("Help");
        await FileFlows.Table.ButtonDisabled("Edit");
        await FileFlows.Table.ButtonDisabled("Delete");
    }

    [Test]
    public Task Help()
        => FileFlows.Help.TestDatalistButton("https://docs.fileflows.com/nodes");

    [Test]
    public async Task Download()
    {
        // Start the task of waiting for the download
        var waitForDownloadTask = Page.WaitForDownloadAsync();
        // Perform the action that initiates download
        await Page.Locator("a.btn >> text='Download Node'").ClickAsync();
        // Wait for the download process to complete
        var download = await waitForDownloadTask;
        Console.WriteLine(await download.PathAsync());
        // Save downloaded file somewhere
        string file = Path.GetTempFileName() + ".zip";
        await download.SaveAsAsync(file);
        ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Read);
        bool dll = zip.Entries.Any(x => x.Name == "FileFlows.Node.dll");
        Assert.IsTrue(dll);
    }
    
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