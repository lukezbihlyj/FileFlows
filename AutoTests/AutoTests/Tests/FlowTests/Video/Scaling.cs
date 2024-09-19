using System.Diagnostics;
using System.Net;
using NUnit.Framework.Internal;

namespace FileFlowsTests.Tests.FlowTests.Video;

/// <summary>
/// Scaling video tests
/// </summary>
public class Scaling:VideoTest
{
    /// <summary>
    /// Downscales a video
    /// </summary>
    [Test]
    public async Task Downscale()
    {
        await Test(
            "Downscale",
            TestFiles.AnimeSampleMkv,
            "480p",
            "1280x960",
            "640x480"
        );
    }

    /// <summary>
    /// Upscales a video
    /// </summary>
    [Test]
    public async Task Upscale()
    {
        await Test(
            "Upscale",
            TestFiles.BigBuckBunny480p30sMp4,
            "720p",
            "854x480",
            "1280x720",
            force: true
        );
    }

    private async Task Test(string flowName, string input, string targetResolution, string inputResolution, string outputResolution, bool force = false)
    {
        string shortName = input[(input.LastIndexOf('/') + 1)..];
        flowName = RandomName(flowName);
        string libName = "Library For " + flowName;
        await CreateFlow(flowName, "Convert Video", new FlowField[]
        {
            new("Video Codec", "H.264", InputType.Select),
            new("Output File", "Save to Folder", InputType.Select),
            new("Destination Folder", TempPath),
            new("Downscale", targetResolution, InputType.Select)
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

        await CreateLibrary(new()
        {
            Name = libName,
            Flow = flowName,
            Path = TestFiles.TestPath,
            Filter = Regex.Escape(input),
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
        await Expect(Page.Locator(".flow-tab.active .md-Resolution .value")).ToHaveTextAsync(inputResolution);
        await SelectTab("Output");
        await Expect(Page.Locator(".flow-tab.active .md-Resolution .value")).ToHaveTextAsync(outputResolution);
    }
}