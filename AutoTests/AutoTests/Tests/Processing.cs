namespace FileFlowsTests.Tests;

public class Processing : TestBase
{
    public Processing() : base("Flows")
    {
    }
    
    
    [Test]
    public async Task ProcesssToH264()
    {
        const string FlowName = "Process To H264";
        const string LibraryName = "Library for " + FlowName;
        await TableButtonClick("Add");
        await EditorTitle("Add Flow");
        await SetText("Name", FlowName);
        await SetSelect("Template", "Convert Video");
        await SetSelect("VideoCodec", "H.264");
        await SetSelect("OutputFile", "Save to Folder");
        await SetText("Destination", "/media/converted");
        await ButtonClick("Save");

        await GotoPage("Libraries");
        await TableButtonClick("Add");
        await EditorTitle("Library");
        await SetSelect("Template", "Video Library");
        await SetText("Name", LibraryName);
        await SetText("Path", TestFiles.TestPath);
        await SetSelect("Flow", FlowName);
        await SelectTab("Advanced");
        await SetText("Filter", @"basic\.mkv$");
        await SetToggle("UseFingerprinting", false);
        await ButtonClick("Save");

        await SelectItem(LibraryName);
        await TableButtonClick("Rescan");

        await Task.Delay(5);

        await GotoPage("Files");
        await ItemExists(TestFiles.BasicMkv);
    }
}