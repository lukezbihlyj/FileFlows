namespace FileFlowsTests.Helpers.UiComponents;

public class MessageBox : UiComponent
{
    public MessageBox(TestBase test) : base(test)
    {
    }

    public async Task Exist(string title, string message)
    {
        await Expect(Page.Locator($".flow-modal-title")).ToContainTextAsync(title);
        await Expect(Page.Locator($".flow-modal-body")).ToContainTextAsync(message);
    }

    public async Task ButtonClick(string name)
    {
        await Page.Locator($".flow-modal-footer button >> text='{name}'").ClickAsync();
        await Expect(Page.Locator(".flow-modal")).ToHaveCountAsync(0);
    }

}