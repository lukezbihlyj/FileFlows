namespace FileFlowsTests.Tests.FlowTests.Video;

/// <summary>
/// Tests for processing files
/// </summary>
[TestClass]
public class Processing : VideoTest
{
    [TestMethod]
    public async Task ProcessToH264()
    {
        const string FlowName = "Process To H264";
        await CreateFlow(FlowName, "Convert Video", new FlowField[]
        {
            new("Video Codec", "H.264", InputType.Select),
            new("Output File", "Save to Folder", InputType.Select),
            new("Destination Folder", TempPath)
        });
        
        const string LibraryName = "Library for " + FlowName;

        string shortName = await CreateLibrary(TestFiles.TestVideo1, new()
        {
            Name = LibraryName,
            Flow = FlowName,
            Path = TestFiles.TestPath,
            Filters = ["/" + Regex.Escape("InitialConfiguration.webm") + "/"],
            Template = "Video Library"
        }, scan: true);

        await Task.Delay(5);

        await GotoPage("Files");
        await ItemExists(shortName);
    }
}