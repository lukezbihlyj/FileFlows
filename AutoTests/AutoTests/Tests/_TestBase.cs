using System.Text;
using FileFlowsTests.Helpers;
using NUnit.Framework.Interfaces;

namespace FileFlowsTests.Tests;

//[Parallelizable(ParallelScope.All)]
public abstract class TestBase(string PageName): PlaywrightTest()
{
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
    public IBrowser Browser { get; private set; }
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
    /// Sets up the tests
    /// </summary>
    [SetUp]
    public async Task Setup()
    {
        Logger.Writer = TestContext.WriteLine;
        TempPath = TestContext.Parameters.Get("FF_TEMP_PATH", Environment.GetEnvironmentVariable("FF_TEMP_PATH"))?.EmptyAsNull() ?? Path.GetTempPath();
        var ffBaseUrl = TestContext.Parameters.Get("FileFlowsUrl", Environment.GetEnvironmentVariable("FileFlowsUrl"))?.EmptyAsNull()  ?? "http://localhost:5276/";
        Logger.ILog("FF Base URL: " + ffBaseUrl);
        Logger.ILog("Temp Path: " + ffBaseUrl);
        RecordingsDirectory = Path.Combine(TempPath, "recordings", TestContext.CurrentContext.Test.FullName);
        if (Directory.Exists(RecordingsDirectory) == false)
            Directory.CreateDirectory(RecordingsDirectory);
        Logger.ILog("Recordings Path: " + RecordingsDirectory);
        #if(DEBUG)
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false, // This makes the browser window visible
            Args = new[] { "--window-size=1920,1080" } // This sets the window size
        });
        #else
        Browser = await Playwright.Chromium.LaunchAsync();
        #endif
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
        FileFlows = new FileFlowsHelper(this, ffBaseUrl);
        await FileFlows.Open();
        if(string.IsNullOrWhiteSpace(PageName) == false)
            await FileFlows.GotoPage(PageName);
    }
    
    /// <summary>
    /// Tears down the tests/cleans it up
    /// </summary>
    [TearDown]
    public async Task TearDown()
    {
        bool failed = TestContext.CurrentContext.Result.Outcome == ResultState.Failure ||
                      TestContext.CurrentContext.Result.Outcome == ResultState.Error;
        bool blazorError = await Page.Locator("#blazor-error-ui").IsVisibleAsync();
        try
        {

            if (failed)
                await Task.Delay(5000); // for recording padding

            // Make sure to close, so that videos are saved.
            await Page.CloseAsync();
            await Context.CloseAsync();
            await Browser.CloseAsync();

            if (Environment.GetEnvironmentVariable("KEEP_PASSED_VIDEOS") == "false" && failed == false &&
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
                File.Move(videoFile,
                    Path.Combine(RecordingsDirectory, TestContext.CurrentContext.Test.FullName + ".webm"), true);
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

    protected Task GotoPage(string name) => FileFlows.GotoPage(name);
    protected Task SkyBox(string name) => FileFlows.SkyBox(name);
    protected Task TableButtonClick(string name, bool sideEditor = false) => FileFlows.Table.ButtonClick(name, sideEditor: sideEditor);
    protected Task EditorTitle(string title) => FileFlows.Editor.Title(title);
    protected Task SetText(string name, string value) => FileFlows.Inputs.SetText(name, value);
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

    protected Task SelectTab(string name) => FileFlows.Tab.Click(name);

    protected Task<bool> TabExists(string name) => FileFlows.Tab.DoesExists(name);

    protected Task MessageBoxExists(string title, string message)
        => FileFlows.MessageBox.Exist(title, message);

    protected Task MessageBoxButton(string name)
        => FileFlows.MessageBox.ButtonClick(name);

    protected Task ToastError(string error)
        => FileFlows.Toast.Error(error);
}