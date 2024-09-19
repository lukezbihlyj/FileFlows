namespace FileFlowsTests.Tests.FlowTests.Video;

/// <summary>
/// Tests for processing files
/// </summary>
public class Processing() : VideoTest
{
    [Test]
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

        await CreateLibrary(new()
        {
            Name = LibraryName,
            Flow = FlowName,
            Path = TestFiles.TestPath,
            Filters = ["/" + Regex.Escape("InitialConfiguration.webm") + "/"],
            Template = "Video Library"
        }, scan: true);

        await Task.Delay(5);

        await GotoPage("Files");
        await ItemExists(TestFiles.TestVideo1);
    }
}