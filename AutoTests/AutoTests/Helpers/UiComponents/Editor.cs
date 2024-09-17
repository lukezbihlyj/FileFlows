namespace FileFlowsTests.Helpers.UiComponents;

public class Editor : UiComponent
{
    public Editor(TestBase test) : base(test)
    {
    }

    private ILocator Button(string name) 
        => Page.Locator($".vi-container .top-row button >> text='{name}'").Last;
    
    public Task ButtonClick(string name)
        => Button(name).ClickAsync();

    public async Task ButtonDisabled(string name)
        => Assert.IsTrue(await Button(name).IsDisabledAsync());
    public async Task ButtonEnabled(string name)
        => Assert.IsTrue(await Button(name).IsEnabledAsync());

    public Task Title(string title)
        => Expect(Page.Locator($".vi-container .top-row .title >> text='{title}'")).ToHaveCountAsync(1);
}