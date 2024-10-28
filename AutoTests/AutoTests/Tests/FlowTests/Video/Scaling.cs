namespace FileFlowsTests.Tests.FlowTests.Video;

/// <summary>
/// Scaling video tests
/// </summary>
//[TestClass]
public class Scaling : VideoTest
{
    /// <summary>
    /// Downscales a video
    /// </summary>
    [Test, Order(1)]
    public async Task Downscale()
    {
        var outputFile = await Test(
            "Downscale",
            TestFiles.TestVideo1,
            "480P",
            "1920x1080",
            "640x360"
        );
        File.Move(outputFile, Path.Combine(TempPath, "downscaled.mkv"));
    }

    /// <summary>
    /// Upscales a video
    /// </summary>
    [Test, Order(2)]
    public async Task Upscale()
    {
        await Test(
            "Upscale",
            Path.Combine(TempPath, "downscaled.mkv"),
            "720P",
            "640x360",
            "1280x720",
            force: true
        );
    }

    private async Task<string> Test(string flowName, string input, string targetResolution, string inputResolution, string outputResolution, bool force = false)
    {
        flowName = RandomName(flowName);
        string libName = "Library For " + flowName;
        await CreateFlow(flowName, "Convert Video", new FlowField[]
        {
            new("Video Codec", "H.264", InputType.Select),
            new("Output File", "Save to Folder", InputType.Select),
            new("Destination Folder", TempPath),
            new("Downscale", targetResolution.ToLower(), InputType.Select)
        });

        if (force)
        {
            // await DoubleClickItem(flowName);
            await FlowPartEdit(targetResolution);
            await SetText("Name", "Scale Video");
            await SetToggle("Force", true);
            await ButtonClick("Save");

            await Task.Delay(500); // so editor can close and actual save button can be reached
            await Page.Locator(".flows-tab-button.active .fa-save").ClickAsync();
            await Task.Delay(1000); // give it a chance to save
        }

        string shortName = await CreateLibrary(input, new()
        {
            Name = libName,
            Flow = flowName,
            Template = "Video Library"
        }, scan: true);

        await GotoPage("Files");
        await SkyBox("Processed");
        await Task.Delay(5_000);
        DateTime end = DateTime.Now.AddMinutes(2);
        while (end > DateTime.Now)
        {
            await SkyBox("Processed");
            if (await ItemExists(shortName))
                break;
        
            await Task.Delay(1000);
        }
        
        await DoubleClickItem(shortName);

        string log = await DownloadLog();
        Logger.ILog(new string('-', 100) + Environment.NewLine + log);
        Logger.ILog(new string('-', 100));
        
        int count = 0;
        await Task.Delay(250);
        while (await TabExists("Output") == false)
        {
            if (++count > 5)
                throw new Exception("Failed to locate 'Output' tab");
            await ButtonClick("Close");
            await Task.Delay(5_000);
            await DoubleClickItem(shortName);
            await Task.Delay(250);
        }
        
        await SelectTab("Input");
        await Task.Delay(1000);
        await Expect(Page.Locator(".flow-tab.active .md-Resolution .value")).ToHaveTextAsync(inputResolution);
        await SelectTab("Output");
        await Task.Delay(250);
        await Expect(Page.Locator(".flow-tab.active .md-Resolution .value")).ToHaveTextAsync(outputResolution);

        return Path.Combine(TempPath, shortName.Replace(".webm", ".mkv"));
    }
}