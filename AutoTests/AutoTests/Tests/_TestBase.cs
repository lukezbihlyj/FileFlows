using System.Text;
using FileFlowsTests.Helpers;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileFlowsTests.Tests;

//[Parallelizable(ParallelScope.All)]
public abstract class TestBase: PageTest
{
    /// <summary>
    /// Optional page name to go to when starting
    /// </summary>
    protected virtual string PageName => "";
    
    /// <summary>
    /// The test logger
    /// </summary>
    public readonly TestLogger Logger = new();
    /// <summary>
    /// Gest the FileFlows Helper
    /// </summary>
    protected Helpers.FileFlowsHelper FileFlows { get; private set; }

    // /// <summary>
    // /// Gets the browser context
    // /// </summary>
    // public IBrowserContext Context { get; private set; }
    // /// <summary>
    // /// Gets the browser instance
    // /// </summary>
    // public static IBrowser Browser { get; private set; }
    // /// <summary>
    // /// Gets the page intsance
    // /// </summary>
    // public IPage Page { get; private set; }

    /// <summary>
    /// Any console errors
    /// </summary>
    private readonly StringBuilder ConsoleErrors = new();

    private string _RecordingsDirectory;

    /// <summary>
    /// Gets or sets the recordings directory
    /// </summary>
    private string RecordingsDirectory
    {
        get
        {
            if(string.IsNullOrWhiteSpace(_RecordingsDirectory))
                _RecordingsDirectory = Path.Combine(TempPath, "recordings",
                    TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName);
            return _RecordingsDirectory;
        }
    }

    private string _TempPath;
    /// <summary>
    /// Gets the temporary path to use in the test
    /// </summary>
    protected string TempPath
    {
        get
        {
            if (_TempPath == null)
            {
                _TempPath = Environment.GetEnvironmentVariable("FF_TEMP_PATH")?.EmptyAsNull() ?? Path.GetTempPath();
            }

            return _TempPath;
        }
    }

    /// <summary>
    /// A random to use in the tests
    /// </summary>
    protected Random rand = new Random(DateTime.Now.Millisecond);

    /// <summary>
    /// Gets a random name to use
    /// </summary>
    /// <param name="prefix">a prefix on the name</param>
    /// <returns>the random name</returns>
    protected string RandomName(string prefix) => prefix + " " + rand.Next(1, 100_000);
    

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions()
        {
            ColorScheme = ColorScheme.Dark,
            ViewportSize = new ViewportSize
            {
                Width = 1920, 
                Height = 1080
            },
            RecordVideoDir = RecordingsDirectory + "/",
            RecordVideoSize = new RecordVideoSize() { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true,
            Permissions = new []
            {
                "clipboard-read", "clipboard-write"
            }
        };
    }
    
    /// <summary>
    /// Sets up the tests
    /// </summary>
    [TestInitialize]
    public async Task TestSetup()
    {
        await base.Setup();
        Logger.Writer = TestContext.WriteLine;
        var ffBaseUrl = Environment.GetEnvironmentVariable("FileFlowsUrl")?.EmptyAsNull()  ?? "http://localhost:5276/";
        Logger.ILog("FF Base URL: " + ffBaseUrl);
        Logger.ILog("Temp Path: " + TempPath);
        if (Directory.Exists(RecordingsDirectory) == false)
            Directory.CreateDirectory(RecordingsDirectory);
        Logger.ILog("Recordings Path: " + RecordingsDirectory);
        // if (Browser == null)
        // {
        //     if (Environment.GetEnvironmentVariable("DOCKER") == "1")
        //     {
        //         Logger.ILog("Running in Docker");
        //         Browser = await Playwright.Chromium.LaunchAsync();
        //     }
        //     else
        //     {
        //         Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        //         {
        //             Headless = false, // This makes the browser window visible
        //             Args = new[] { "--window-size=1920,1080" } // This sets the window size
        //         });
        //     }
        // }
        //
        // Context = await Browser.NewContextAsync(new()
        // {
        //     ViewportSize = new ViewportSize
        //     {
        //         Width = 1920, 
        //         Height = 1080
        //     },
        //     RecordVideoDir = RecordingsDirectory + "/",
        //     RecordVideoSize = new RecordVideoSize() { Width = 1920, Height = 1080 },
        //     IgnoreHTTPSErrors = true,
        //     Permissions = new []
        //     {
        //         "clipboard-read", "clipboard-write"
        //     }
        // });
        // Page = await Context.NewPageAsync();

        Page.Console += (_, msg) =>
        {
            if ("error".Equals(msg.Type))
            {
                Logger.ELog(msg.Text);
                ConsoleErrors.AppendLine(msg.Text);
            }
            else
                Logger.ILog(msg.Text);
        };
        FileFlows = new FileFlowsHelper(this, ffBaseUrl, Logger);
        await FileFlows.Open();
        if(string.IsNullOrWhiteSpace(PageName) == false)
            await FileFlows.GotoPage(PageName);
    }
    
    /// <summary>
    /// Tears down the tests/cleans it up
    /// </summary>
    [TestCleanup]
    public async Task TestBaseDown()
    {
        bool blazorError = false;
        try
        {
            if (TestOK == false)
                await Task.Delay(5000); // for recording padding
            
            if (Page != null)
            {
                try
                {
                    blazorError = await Page.Locator("#blazor-error-ui").IsVisibleAsync();
                }
                catch (Exception)
                {
                    // Ignored
                }

                try
                {
                    // Make sure to close, so that videos are saved.
                    await Page.CloseAsync();
                }
                catch (Exception ex)
                {
                    Logger.ELog("Page Closing failed: " + ex.Message);
                }
            }

            if (Context != null)
            {
                try
                {
                    await Context.CloseAsync();
                }
                catch (Exception ex)
                {
                    Logger.ELog("Context Closing failed: " + ex.Message);
                }
            }
            await base.Teardown();
            
            if (string.IsNullOrWhiteSpace(RecordingsDirectory) == false)
            {
                var outputVideo = Path.Combine(RecordingsDirectory,
                    TestContext.FullyQualifiedTestClassName + "." + TestContext.TestName + ".webm");
                if (outputVideo != TestFiles.TestVideo1 &&
                    Environment.GetEnvironmentVariable("KEEP_PASSED_VIDEOS") == "false" && TestOK &&
                    blazorError == false)
                {
                    try
                    {
                        Directory.Delete(RecordingsDirectory, true);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (Directory.Exists(RecordingsDirectory))
                {
                    var videoFile = Directory.GetFiles(RecordingsDirectory, "*.webm").FirstOrDefault();
                    if (videoFile == null)
                        return;
                    File.Move(videoFile, outputVideo, true);
                    Logger.ILog("Output Video: " + outputVideo);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.ELog("TearDown error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }

        if (TestOK && blazorError)
        {
            Assert.Fail("Blazor Error: " + ConsoleErrors);
        }
    }

    protected Task GotoPage(string name, bool forceLoad = false) => FileFlows.GotoPage(name, forceLoad);
    protected Task SkyBox(string name, bool waitFor = false) => FileFlows.SkyBox(name, waitFor);
    protected Task TableButtonClick(string name, bool sideEditor = false) => FileFlows.Table.ButtonClick(name, sideEditor: sideEditor);
    protected Task EditorTitle(string title) => FileFlows.Editor.Title(title);
    protected Task SetText(string name, string value) => FileFlows.Inputs.SetText(name, value);
    protected Task SetArray(string name, string[] values) => FileFlows.Inputs.SetArray(name, values);
    protected Task SetTextArea(string name, string value) => FileFlows.Inputs.SetTextArea(name, value);
    protected Task SetSelect(string name, string value) => FileFlows.Inputs.SetSelect(name, value);
    protected Task SetNumber(string name, int value) => FileFlows.Inputs.SetNumber(name, value);
    protected Task SetToggle(string name, bool value) => FileFlows.Inputs.SetToggle(name, value);
    protected Task SetCode(string code) => FileFlows.Inputs.SetCode(code);
    protected Task SetInputFile(string file) 
        => Page.Locator("input[type=file]").SetInputFilesAsync(file);
    protected Task ButtonClick(string name) => FileFlows.Editor.ButtonClick(name);
    protected Task SelectItem(string name, bool sideEditor = false) 
        => FileFlows.Table.Select(name, sideEditor: sideEditor);
    protected Task UnSelectItem(string name, bool sideEditor = false) 
        => FileFlows.Table.UnSelect(name, sideEditor: sideEditor);

    protected Task DoubleClickItem(string name)
        => FileFlows.Table.DoubleClick(name);
    protected Task ItemDoesNotExist(string name, bool sideEditor = false) 
        => FileFlows.Table.ItemDoesNotExist(name, sideEditor: sideEditor);

    protected Task<bool> ItemExists(string name, bool sideEditor = false)
        => FileFlows.Table.Exists(name, sideEditor);

    protected Task<bool> WaitForExists(string name, bool sideEditor = false, int timeout = 30000)
        => FileFlows.Table.WaitForExists(name, sideEditor, timeout);

    protected Task SelectTab(string name) => FileFlows.Tab.Click(name);

    protected Task<bool> TabExists(string name) => FileFlows.Tab.DoesExists(name);

    protected Task MessageBoxExists(string title, string message)
        => FileFlows.MessageBox.Exist(title, message);

    protected Task MessageBoxButton(string name)
        => FileFlows.MessageBox.ButtonClick(name);

    protected Task ToastError(string error)
        => FileFlows.Toast.Error(error);
}