namespace FileFlowsTests.Helpers.UiComponents;

/// <summary>
/// Helper for interacting with Flows
/// </summary>
/// <param name="test">the executing test</param>
public class Flow(TestBase test) : UiComponent(test)
{
    private ILocator FlowElement(string label)
        => Page.Locator($"#flow-parts .flow-part:has-text(\"{label}\") .draggable");

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
}