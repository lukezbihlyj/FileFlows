using Humanizer;

namespace FileFlowsTests.Helpers;

using FileFlowsTests.Helpers.UiComponents;
public class FileFlowsHelper
{
    /// <summary>
    /// Gets the tab helper
    /// </summary>
    public Tab Tab { get; }
    /// <summary>
    /// Gets the Initial Configuration helper
    /// </summary>
    public InitialConfiguration InitialConfiguration { get; }
    /// <summary>
    /// Gets the table helper
    /// </summary>
    public Table Table { get; }
    /// <summary>
    /// Gets the toast helper
    /// </summary>
    public Toast Toast { get; }
    /// <summary>
    /// Gets the message box helper
    /// </summary>
    public MessageBox MessageBox { get; }
    /// <summary>
    /// Gets the editor helper
    /// </summary>
    public Editor Editor { get; }
    /// <summary>
    /// Gets the inputs helper
    /// </summary>
    public Inputs Inputs { get; }
    /// <summary>
    /// Gets the help helper
    /// </summary>
    public Help Help { get; }
    
    /// <summary>
    /// Gets the flow helper
    /// </summary>
    public Flow Flow { get; }
    
    /// <summary>
    /// Gets the flow template dialog helper
    /// </summary>
    public FlowTemplateDialog FlowTemplateDialog { get; }

    /// <summary>
    /// Gets the page
    /// </summary>
    private IPage Page { get; }
    
    /// <summary>
    /// Gets the base URL for FileFlows eg http://192.168.1.10:19200/
    /// </summary>
    public string BaseUrl { get;init; }
    
    /// <summary>
    /// Gets the logger to use
    /// </summary>
    public ILogger Logger { get; init; }

    /// <summary>
    /// Constructs a new instance of the FileFlows Helper
    /// </summary>
    /// <param name="test">the test using this helper</param>
    /// <param name="baseUrl">the base URL of the FileFlows server</param>
    public FileFlowsHelper(TestBase test, string baseUrl, ILogger logger)
    {
        BaseUrl = baseUrl;
        Logger = logger;
        if (BaseUrl.EndsWith('/') == false)
            BaseUrl += '/';
        this.Page = test.Page;
        this.InitialConfiguration = new(test);
        this.Tab = new (test);
        this.Table = new (test, logger);
        this.Toast = new (test);
        this.MessageBox = new (test);
        this.Inputs = new (test);
        this.Editor = new (test, this);
        this.Help = new (test);
        this.Flow = new (test);
        this.FlowTemplateDialog = new(test);
    }

    /// <summary>
    /// Opens the FileFlows web console
    /// </summary>
    public async Task Open()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForSelectorAsync(".loading-background", new PageWaitForSelectorOptions
        {
            State =  WaitForSelectorState.Detached
        });
        await WaitForBlockerToDisappear();
    }

    /// <summary>
    /// Goes to a specific page by its nav menu link
    /// </summary>
    /// <param name="name">the name of the page</param>
    /// <param name="contentTitle">optional content title to wait for, if different from name</param>
    public async Task GotoPage(string name, bool forceLoad = false, string? contentTitle = null)
    {
        Logger.ILog("GotoPage: " + name);
        try
        {
            ILocator? locator = null;
            string selector = $".nav-item.{(name == "Files" ? "library-files" : name.ToLower())} a";
            Logger.ILog("Selector: " + selector);
            locator = Page.Locator(selector);
            await locator.ClickAsync();
        }
        catch (Exception ex)
        {
            Logger.WLog("Failed to click page link: " + name + " " + ex.Message);
            var selector = $"#ul-nav-menu .nav-item a >> text='{name}'";  
            Logger.WLog("Using secondary selector: " + selector);
            var locator = Page.Locator(selector);
            await locator.ClickAsync();
        }

        Logger.ILog("Clicked Page Link: " + name);
        await Task.Delay(250);
        if (forceLoad)
            await Page.ReloadAsync();
        await WaitForBlockerToDisappear();

        contentTitle = contentTitle?.EmptyAsNull() ?? name;
        
        Logger.ILog("Waiting for top row text: " + contentTitle);
        await Page.Locator(".top-row .title", new ()
        {
            HasTextString = contentTitle
        }).WaitForAsync();

        Logger.ILog("Dismissing update available if present");
        await DismissUpdateAvailable();
        Logger.ILog("GotoPage complete: " + name);
    }

    /// <summary>
    /// Dismisses the update available banner if it is present
    /// </summary>
    public async Task DismissUpdateAvailable()
    {
        try
        {
            var dismissButton = Page.Locator(".update-available .dismiss");
            // Wait for up to 500ms for the dismiss button to become visible
            await dismissButton.WaitForAsync(new LocatorWaitForOptions()
            {
                Timeout = 500,
                State = WaitForSelectorState.Visible
            });

            // If the button becomes visible, click it
            await dismissButton.ClickAsync();
        }
        catch (Exception)
        {
            // If the timeout occurs, it means the button didn't become visible
            // Do nothing, since we don't want to throw an exception
        }
    }

    /// <summary>
    /// Clicks a sky box item by its name
    /// </summary>
    /// <param name="name">the name of the skybox item to click</param>
    /// <param name="waitFor">If it should be waited for</param>
    /// <returns>the task to await</returns>
    public async Task SkyBox(string name, bool waitFor = false)
    {
        var locator = Page.Locator($".skybox-item.{name.Replace(" ", "")}");
        if (waitFor)
        {
            // Wait for the item to appear, retrying every 500ms for up to 30 seconds.
            var maxWaitTime = TimeSpan.FromSeconds(30);
            var retryInterval = TimeSpan.FromMilliseconds(500);
            var startTime = DateTime.Now;

            while (DateTime.Now - startTime < maxWaitTime)
            {
                // Check if the item exists
                if (await locator.CountAsync() > 0)
                {
                    break; // Item exists, proceed to click
                }
                else
                {
                    // If it doesn't exist, click the first .skybox-item to refresh the list
                    var firstItemLocator = Page.Locator(".skybox-item").First;
                    await firstItemLocator.ClickAsync();

                    // Wait for a short period before retrying
                    await Task.Delay(retryInterval);
                }
            }

            // If the skybox item still doesn't exist after the max wait time, throw an exception
            if (await locator.CountAsync() == 0)
            {
                throw new Exception($"Skybox item '{name}' not found after {maxWaitTime.TotalSeconds} seconds.");
            }
        }

        await locator.ClickAsync();
        await WaitForBlockerToDisappear();
    }
    
    /// <summary>
    /// Waits for a blocker to disappear
    /// </summary>
    /// <param name="timeout">the timeout</param>
    /// <exception cref="Exception">if the blocker doesnt disappear</exception>
    public async Task WaitForBlockerToDisappear(int timeout = 30000)
    {
        try
        {
            // Wait until the blocker is hidden or removed
            await Task.Delay(100);
            var blockerLocator = Page.Locator(".blocker");
            await blockerLocator.WaitForAsync(new()
            {
                State = WaitForSelectorState.Detached,
                Timeout = timeout
            });
        }
        catch (TimeoutException)
        {
            // Blocker didn't disappear within the timeout
            throw new Exception("Blocker did not disappear within the timeout.");
        }
    }

}