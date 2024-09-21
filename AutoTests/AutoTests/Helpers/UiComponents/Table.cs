namespace FileFlowsTests.Helpers.UiComponents;

public class Table : UiComponent
{
    public Table(TestBase test) : base(test)
    {
    }

    private ILocator Button(string name, bool sideEditor = false) 
        => Page.Locator((sideEditor ? ".vi-container " : "") + $".flowtable-button >> text='{name}'");
    
    public Task ButtonClick(string name, bool sideEditor = false)
        => Button(name, sideEditor).ClickAsync();

    public async Task ButtonDisabled(string name, bool sideEditor = false)
        => Assert.IsTrue(await Button(name, sideEditor).IsDisabledAsync());
    public async Task ButtonEnabled(string name, bool sideEditor = false)
        => Assert.IsTrue(await Button(name, sideEditor).IsEnabledAsync());

    private ILocator ItemLocator(string name, bool sideEditor = false)
        => Page.Locator((sideEditor ? ".vi-container " : ".main > .vi-container ")
            + $".flowtable-row:has(span:text('{name}')) ");

    // private ILocator ItemLocator(string name, bool sideEditor = false)
    //     => Page.Locator((sideEditor ? ".vi-container " : ".main > .vi-container ")
    //         + $".flowtable-row:has(span:text('{name}')) .flowtable-select input[type=checkbox]");

    public async Task Select(string name, bool sideEditor = false)
        => await SelectItem(name, true, sideEditor);

    public async Task UnSelect(string name, bool sideEditor = false)
        => await SelectItem(name, false, sideEditor);
    
    public async Task SelectItem(string name, bool selectIt, bool sideEditor = false)
    {
        var locator = Page.Locator((sideEditor ? ".vi-container " : ".main > .vi-container ")
                                   + $".flowtable-row:has(span:text('{name}')) .flowtable-select input[type=checkbox]");

        var chk = await locator.IsCheckedAsync();
        if(chk)
            await locator.First.ClickAsync();
        
        if(selectIt)
            await locator.First.ClickAsync();
        
    }

    public async Task<bool> Exists(string name, bool sideEditor = false)
        => await ItemLocator(name, sideEditor).CountAsync() > 0; 

    public Task ItemDoesNotExist(string name, bool sideEditor = false)
        => Expect(ItemLocator(name, sideEditor)).ToHaveCountAsync(0);

    public Task DoubleClick(string name)
        => ItemLocator(name, false).First.DblClickAsync();
}