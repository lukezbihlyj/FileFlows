namespace FileFlowsTests.Tests.FlowTests.Video;

/// <summary>
/// Tests vidoe scripting
/// </summary>
public class VideoScripting : VideoTest
{
    [Test]
    public async Task VideoVariablesInFunctionTest()
    {
        string fileOutput = Path.Combine(TempPath, $"video-variables-in-function-output-{Guid.NewGuid()}.log");
        string FlowName = RandomName("Video Variables In Function");
        await CreateFlow(FlowName, "Video File", []);
       
        await FileFlows.Flow.AddFlowElement("FileFlows.BasicNodes.Functions.Function", 450, 340);
        await SetCode($@"
System.IO.File.AppendAllText(""{fileOutput}"", ""Video Codec: "" + Variables.vi.VideoInfo.VideoStreams[0].Codec + ""\n"");
System.IO.File.AppendAllText(""{fileOutput}"", ""Audio Streams: "" + Variables.vi.VideoInfo.AudioStreams.Length + ""\n"");
return 1");
        await ButtonClick("Save");
        await Task.Delay(250);

        await FileFlows.Flow.Connect("Video File", "Function");

        await FileFlows.Flow.Save();
        
        string LibraryName = "Library for " + FlowName;

        await CreateLibrary(TestFiles.TestVideo1, new()
        {
            Name = LibraryName,
            Flow = FlowName,
            Path = TestFiles.TestPath,
            Template = "Video Library"
        }, scan: true);

        
        DateTime start = DateTime.Now;
        while (DateTime.Now.Subtract(start).TotalSeconds < 60)
        {
            if (File.Exists(fileOutput))
                break;
            await Task.Delay(250);
        }
        
        Assert.IsTrue(File.Exists(fileOutput), "Output file does not exist");
        var output = await File.ReadAllTextAsync(fileOutput);
        Logger.ILog("Output: " + Environment.NewLine + output);
        Assert.IsTrue(output.Contains("Video Codec: h264"), "Wrong video codec detected");
        Assert.IsTrue(output.Contains("Audio Streams: 0"), "Wrong number of audio streams detected");
    }
}