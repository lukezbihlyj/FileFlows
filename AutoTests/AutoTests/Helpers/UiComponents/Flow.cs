namespace FileFlowsTests.Helpers.UiComponents;

/// <summary>
/// Helper for interacting with Flows
/// </summary>
/// <param name="test">the executing test</param>
public class Flow(TestBase test) : UiComponent(test)
{
    private ILocator FlowElement(string label)
        => Page.Locator($".flow-parts .flow-part:has-text(\"{label}\") .draggable");

    public Task Select(string label)
        => FlowElement(label).ClickAsync();

    public async Task Edit(string label)
    {
        var locator = FlowElement(label);
        await locator.DblClickAsync();
        await Task.Delay(500);
    }

    public async Task Delete(string label)
    {
        await FlowElement(label).ClickAsync();
        await Page.Keyboard.PressAsync("Delete");
    }

    /// <summary>
    /// Sets the title of a flow
    /// </summary>
    /// <param name="title">the title</param>
    /// <returns>the task to await</returns>
    public Task SetTitle(string title)
        => Page.Locator(".flows-tab-button.active input[type=text]").FillAsync(title);


    /// <summary>
    /// Saves the active flow
    /// </summary>
    /// <param name="waitForClean">if it should wait for this flow to be cleaned (fully saved)</param>
    /// <returns>the task to await</returns>
    public async Task Save(bool waitForClean = true)
    {
        await Page.Locator(".flows-tab-button.active .actions .fa-save").ClickAsync();
        if (waitForClean == false)
            return;
    
        // Wait for the save icon to disappear or become inactive
        await Page.WaitForSelectorAsync(".flows-tab-button.active .actions .fa-save", 
            new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Hidden // or WaitForSelectorState.Detached if you prefer
            });
    }

    /// <summary>
    /// Adds a flow element
    /// </summary>
    /// <param name="fullName">th full name of the flow element to add</param>
    /// <param name="xPos">the x position of the flow element</param>
    /// <param name="yPos">the y position of the flow element</param>
    public async Task AddFlowElement(string fullName, int xPos, int yPos)
    {
        var filter = Page.Locator(".flow-tab.active .flow-elements-filter input");
        await filter.FillAsync(fullName);
        await filter.PressAsync("Enter");
        var ele = Page.Locator($".flow-elements .flow-element.{fullName.Replace(".", "-")}");
        await ele.HighlightAsync();
        var canvas = Page.Locator(".flow-parts.show canvas");
        await ele.DragToAsync(canvas, new()
        {
            TargetPosition = new()
            {
                X = xPos + 50,
                Y = yPos + 30
            }
        });
    }

    /// <summary>
    /// Connects two flow elements together
    /// </summary>
    /// <param name="source">the source flow element, ie the one with the output connection</param>
    /// <param name="destination">the destination flow element, ie the one with the input connection</param>
    /// <param name="output">the output to connect</param>
    public async Task Connect(string source, string destination, int output = 1)
    {
        var eleSource = Page.Locator($".flow-parts.show .flow-part >> text={source}").Locator("..");
        var sourceOutput = eleSource.Locator($".output-{output} div");
        var destInput = Page.Locator($".flow-parts.show .flow-part >> text={destination}").Locator("..");
        // Get the bounding box of the source output and destination input
        var sourceBox = await sourceOutput.BoundingBoxAsync();
        var destBox = await destInput.BoundingBoxAsync();

        if (sourceBox == null || destBox == null)
        {
            throw new Exception("Could not find bounding boxes for either source output or destination input.");
        }

        // Simulate the drag from source output to destination input
        await Page.Mouse.MoveAsync(sourceBox.X + sourceBox.Width / 2, sourceBox.Y + sourceBox.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(destBox.X + destBox.Width / 2, destBox.Y + destBox.Height / 2, new MouseMoveOptions { Steps = 10 });
        await Page.Mouse.UpAsync();
    }
}