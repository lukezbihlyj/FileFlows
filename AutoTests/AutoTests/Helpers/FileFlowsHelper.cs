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
    public FileFlowsHelper(TestBase test)
    {
        BaseUrl = Environment.GetEnvironmentVariable("FileFlowsUrl")?.EmptyAsNull() ?? "http://localhost:5276/";
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
    public async Task GotoPage(string name)
    {
        await Page.Locator($".nav-item.{(name == "Files" ? "library-files" : name.ToLower())} a").ClickAsync();
        
        await Page.Locator(".top-row .title", new ()
        {
            HasTextString = name
        }).WaitForAsync();
        
        var dismissButton = Page.Locator(".update-available .dismiss");

        // Check if the element exists and click it if present
        if (await dismissButton.CountAsync() > 0)
        {
            await dismissButton.ClickAsync();
        }

        
    }

    /// <summary>
    /// Clicks a sky box item by its name
    /// </summary>
    /// <param name="name">the name of the skybox item to click</param>
    /// <returns>the task to await</returns>
    public Task SkyBox(string name)
        => Page.Locator($".skybox-item.{name.Dehumanize()} ").ClickAsync();
}