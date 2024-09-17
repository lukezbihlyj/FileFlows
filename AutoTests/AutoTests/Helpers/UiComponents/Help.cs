namespace FileFlowsTests.Helpers.UiComponents;

public class Help:UiComponent
{
    public Help(TestBase test) : base(test)
    {
    }

    public Task TestButton(string target)
        => TestLocator(Page.Locator("button >> text=Help"), target);
    public Task TestDatalistButton(string target)
        => TestLocator(Page.Locator("button.flowtable-button >> text=Help"), target);

    private async Task TestLocator(ILocator locator, string target)
    {
        var newPage = await Context.RunAndWaitForPageAsync(async () =>
        {
            await locator.ClickAsync();
        });
        await newPage.WaitForLoadStateAsync();
        Assert.AreEqual(target, newPage.Url);
    }
}