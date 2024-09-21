using FileFlowsTests.Tests;
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
    /// Constructs a new instance of the FileFlows Helper
    /// </summary>
    /// <param name="test">the test using this helper</param>
    /// <param name="baseUrl">the base URL of the FileFlows server</param>
    public FileFlowsHelper(TestBase test, string baseUrl)
    {
        BaseUrl = baseUrl;
        if (BaseUrl.EndsWith('/') == false)
            BaseUrl += '/';
        this.Page = test.Page;
        this.InitialConfiguration = new(test);
        this.Tab = new (test);
        this.Table = new (test);
        this.Toast = new (test);
        this.MessageBox = new (test);
        this.Inputs = new (test);
        this.Editor = new (test);
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
    }

    /// <summary>
    /// Goes to a specific page by its nav menu link
    /// </summary>
    /// <param name="name">the name of the page</param>
    public async Task GotoPage(string name, bool forceLoad = false)
    {
        await Page.Locator($".nav-item.{(name == "Files" ? "library-files" : name.ToLower())} a").ClickAsync();
        await Task.Delay(250);
        if (forceLoad)
            await Page.ReloadAsync();
        
        await Page.Locator(".blocker").WaitForAsync(new LocatorWaitForOptions()
        {
            State = WaitForSelectorState.Detached
        });
        
        await Page.Locator(".top-row .title", new ()
        {
            HasTextString = name
        }).WaitForAsync();

        var dismissButton = Page.Locator(".update-available .dismiss");

        try
        {
            // Wait for up to 500ms for the dismiss button to become visible
            await dismissButton.WaitForAsync(new LocatorWaitForOptions()
            {
                Timeout = 500,
                State = WaitForSelectorState.Visible
            });

            // If the button becomes visible, click it
            await dismissButton.ClickAsync();
        }
        catch (TimeoutException)
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
        var locator = Page.Locator($".skybox-item.{name.Dehumanize()}");
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
    }
}