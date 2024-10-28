using System.Text;
using FileFlowsTests.Helpers;
using NUnit.Framework.Interfaces;


namespace FileFlowsTests.Tests;

//[Parallelizable(ParallelScope.All)]
public abstract class TestBase : PlaywrightTest
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

    /// <summary>
    /// Gets the browser context
    /// </summary>
    public IBrowserContext Context { get; private set; }
    /// <summary>
    /// Gets the browser instance
    /// </summary>
    public static IBrowser Browser { get; private set; }
    /// <summary>
    /// Gets the page intsance
    /// </summary>
    public IPage Page { get; private set; }

    /// <summary>
    /// Any console errors
    /// </summary>
    private readonly StringBuilder ConsoleErrors = new();
    
    /// <summary>
    /// Gets or sets the recordings directory
    /// </summary>
    private string RecordingsDirectory { get; set; }

    /// <summary>
    /// Gets the temporary path to use in the test
    /// </summary>
    protected string TempPath { get; private set; }

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

    /// <summary>
    /// Called at the start of the test setup
    /// </summary>
    /// <returns></returns>
    protected virtual bool SetupStart() => true;
    
    /// <summary>
    /// Gest the base url for the FileFlows
    /// </summary>
    protected string FileFlowsBaseUrl { get; private set; }
    
    /// <summary>
    /// Sets up the tests
    /// </summary>
    [SetUp]
    public async Task Setup()
    {
        Logger.Writer = TestContext.WriteLine;
        if (SetupStart() == false)
            Assert.Fail("Test Setup Failed");
        
        TempPath = TestContext.Parameters.Get("FF_TEMP_PATH", Environment.GetEnvironmentVariable("FF_TEMP_PATH"))?.EmptyAsNull() ?? Path.GetTempPath();
        FileFlowsBaseUrl = TestContext.Parameters.Get("FileFlowsUrl", Environment.GetEnvironmentVariable("FileFlowsUrl"))?.EmptyAsNull()  ?? "http://localhost:5276/";
        if(FileFlowsBaseUrl.EndsWith('/') == false)
            FileFlowsBaseUrl += "/";
        Logger.ILog("FF Base URL: " + FileFlowsBaseUrl);
        Logger.ILog("Temp Path: " + TempPath);
        RecordingsDirectory = Path.Combine(TempPath, "recordings", TestContext.CurrentContext.Test.FullName);
        if (Directory.Exists(RecordingsDirectory) == false)
            Directory.CreateDirectory(RecordingsDirectory);
        Logger.ILog("Recordings Path: " + RecordingsDirectory);
        if (Browser == null)
        {
            if (Environment.GetEnvironmentVariable("DOCKER") == "1")
            {
                Logger.ILog("Running in Docker");
                Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions()
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--disable-gpu",
                        "--no-sandbox"
                    }
                });
            }
            else
            {
                Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false, // This makes the browser window visible
                    Args = new[] { "--window-size=1920,1080" } // This sets the window size
                });
            }
        }

        Context = await Browser.NewContextAsync(new()
        {
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
        });
        Page = await Context.NewPageAsync();

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
        FileFlows = new FileFlowsHelper(this, FileFlowsBaseUrl, Logger);
        await FileFlows.Open();
        if(string.IsNullOrWhiteSpace(PageName) == false)
            await FileFlows.GotoPage(PageName);
    }

    /// <summary>
    /// Called when the test ends
    /// </summary>
    /// <param name="result">the result of the test</param>
    protected virtual void TestEnded(ResultState result)
    {
    }

    /// <summary>
    /// Tears down the tests/cleans it up
    /// </summary>
    [TearDown]
    public async Task TearDown()
    {
        TestEnded(TestContext.CurrentContext.Result.Outcome);
        bool passed = TestContext.CurrentContext.Result.Outcome == ResultState.Success ||
                      TestContext.CurrentContext.Result.Outcome == ResultState.NotRunnable;
        bool failed = !passed;
        bool blazorError = false;
        try
        {
            if (failed)
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

            // if (Context != null)
            // {
            //     try
            //     {
            //         await Context.CloseAsync();
            //     }
            //     catch (Exception ex)
            //     {
            //         Logger.ELog("Context Closing failed: " + ex.Message);
            //     }
            // }

            var outputVideo = Path.Combine(RecordingsDirectory, TestContext.CurrentContext.Test.FullName + ".webm"); 
            if (outputVideo != TestFiles.TestVideo1 && Environment.GetEnvironmentVariable("KEEP_PASSED_VIDEOS") == "false" && failed == false &&
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
        catch (Exception ex)
        {
            Logger.ELog("TearDown error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }

        if (failed == false && blazorError)
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