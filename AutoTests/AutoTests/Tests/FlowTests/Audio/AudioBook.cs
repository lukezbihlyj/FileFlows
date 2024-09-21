namespace FileFlowsTests.Tests.FlowTests.Audio;

public class AudioBook : AudioTest
{
    /// <summary>
    /// Manually creates an audiobook flow and tests it
    /// </summary>
    [Test, Order(1)]
    public async Task CreateBook()
    {
        // create flow
        var flowName = RandomName("Create Audio Book");
        const string BOOK_NAME = "How To Use FileFlows";
        string OUTPUT_PATH = Path.Combine(TempPath, flowName, BOOK_NAME + ".m4b");
            
        string libName = "Library For " + flowName;
        await CreateFlow(flowName, "Folder", []);

        await FileFlows.Flow.AddFlowElement("FileFlows.AudioNodes.CreateAudioBook", 250, 220);

        await SetText("DestinationPath", Path.Combine(TempPath, flowName, "{folder.Name}.m4b"));
        await ButtonClick("Save");

        await FileFlows.Flow.Connect("Input Folder", "Create Audio Book");

        await FileFlows.Flow.Save();

        // Create library
        string libPath = Path.Combine(TempPath, "lib-audio-book-" + Guid.NewGuid());
        var bookFolder = Path.Combine(libPath, BOOK_NAME);
        Directory.CreateDirectory(bookFolder);
        await GenerateRandomAudio(Path.Combine(bookFolder, "01. Introduction.mp3"));
        await GenerateRandomAudio(Path.Combine(bookFolder, "02. Flows.mp3"));
        await GenerateRandomAudio(Path.Combine(bookFolder, "03. Libraries.mp3"));
        await GenerateRandomAudio(Path.Combine(bookFolder, "04. Variables.mp3"));
        await GenerateRandomAudio(Path.Combine(bookFolder, "05. Conclusion.mp3"));
        
        await CreateFolderLibrary(libName, flowName, libPath: libPath, scan: true);
        
        
        // Test processing
        await GotoPage("Files");
        await SkyBox("Processed");
        await Task.Delay(5_000);
        DateTime end = DateTime.Now.AddMinutes(2);
        while (end > DateTime.Now)
        {
            await SkyBox("Processed");
            if (await ItemExists(BOOK_NAME))
                break;
        
            await Task.Delay(1000);
        }
        
        await DoubleClickItem(BOOK_NAME);

        string log = await DownloadLog();
        Logger.ILog(new string('-', 100) + Environment.NewLine + log);
        Logger.ILog(new string('-', 100));

        Assert.IsTrue(File.Exists(OUTPUT_PATH), "Book failed to be created: " + OUTPUT_PATH);
    }
    
}