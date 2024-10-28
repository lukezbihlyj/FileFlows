using Humanizer;

namespace FileFlowsTests.Helpers.UiComponents;

/// <summary>
/// Tab helper
/// </summary>
/// <param name="test">the test running</param>
public class Tab(TestBase test) : UiComponent(test)
{
    public Task Click(string name)
        => Page.Locator($".tab-button.tb-{name.Dehumanize()}").ClickAsync();
        
    public Task Exists(string name) 
        => Expect(Page.Locator($".tab-button.tb-{name.Dehumanize()}")).ToHaveCountAsync(1);

    public async Task<bool> DoesExists(string name)
        => await Page.Locator($".tab-button.tb-{name.Dehumanize()}").CountAsync() > 0;
        
    public Task DoesntExists(string name) 
        => Expect(Page.Locator($".tab-button.tb-{name.Dehumanize()}")).ToHaveCountAsync(0);
}