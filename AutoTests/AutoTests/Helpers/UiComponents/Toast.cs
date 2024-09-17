namespace FileFlowsTests.Helpers.UiComponents;

/// <summary>
/// Toast helper
/// </summary>
/// <param name="test">the executing test</param>
public class Toast(TestBase test) : UiComponent(test)
{
    /// <summary>
    /// Tests the toast message is as expected
    /// </summary>
    /// <param name="text">the expected toast error message</param>
    /// <returns>a task to await</returns>
    public Task Error(string text)
        => Expect(Page.Locator($".ff-toast.Error.show .toast-message")).ToContainTextAsync(text);

    /// <summary>
    /// Gets the toast error message
    /// </summary>
    /// <returns>the error message</returns>
    public Task<string> GetError()
        => Page.Locator($".ff-toast.Error.show .toast-message").InnerTextAsync();

    /// <summary>
    /// Closes the toast
    /// </summary>
    public async Task Close()
    {
        var locator =Page.Locator($".toast-click");
        if (await locator.CountAsync() == 1)
            await locator.ClickAsync();
    }
}