using NUnit.Framework.Legacy;

namespace FileFlowsTests.Helpers.UiComponents;

public class Table(TestBase test, ILogger logger) : UiComponent(test)
{
    private ILocator Button(string name, bool sideEditor = false) 
        => Page.Locator((sideEditor ? ".vi-container " : "") + $".flowtable-button >> text='{name}'");
    
    public Task ButtonClick(string name, bool sideEditor = false)
        => Button(name, sideEditor).ClickAsync();

    public async Task ButtonDisabled(string name, bool sideEditor = false)
        => ClassicAssert.IsTrue(await Button(name, sideEditor).IsDisabledAsync());
    public async Task ButtonEnabled(string name, bool sideEditor = false)
        => ClassicAssert.IsTrue(await Button(name, sideEditor).IsEnabledAsync());

    private ILocator ItemLocator(string name, bool sideEditor = false)
    {
        string locator = (sideEditor
            ? ".vi-container "
            : ".main > .vi-container ") +
              $".flowtable-row:has(span:text('{name}'))";
        return Page.Locator(locator).First;
        // return Page.Locator((sideEditor ? ".vi-container " : ".main > .vi-container ")
        //                  + $".flowtable-row:has(span:text('{name}')) ");
        // => 
    }

    // private ILocator ItemLocator(string name, bool sideEditor = false)
    //     => Page.Locator((sideEditor ? ".vi-container " : ".main > .vi-container ")
    //         + $".flowtable-row:has(span:text('{name}')) .flowtable-select input[type=checkbox]");

    public async Task Select(string name, bool sideEditor = false)
        => await SelectItem(name, true, sideEditor);

    public async Task UnSelect(string name, bool sideEditor = false)
        => await SelectItem(name, false, sideEditor);
    
    public async Task SelectItem(string name, bool selectIt, bool sideEditor = false)
    {
        var locator = ItemLocator(name, sideEditor: sideEditor);
        await locator.WaitForAsync(new()
        {
            Timeout = 5000,
            State = WaitForSelectorState.Attached
        });
        if (await locator.CountAsync() == 0)
        {
            logger.WLog("Table item not found: " + name);
            Assert.Fail("Table item not found: " + name);
        }

        await locator.HighlightAsync();
        // get the parent of the locator whose class is .flowtable-row
        // Use XPath to find the nearest ancestor with the class .flowtable-row
        // var flowtableRowLocator = locator.First.Locator("xpath=ancestor::div[contains(@class, 'flowtable-row')]");
        // await flowtableRowLocator.HighlightAsync();
        var checkbox = locator.Locator(".flowtable-select input[type=checkbox]").First;

        var chk = await checkbox.IsCheckedAsync();
        if(chk)
            await checkbox.ClickAsync();
        
        if(selectIt)
            await locator.ClickAsync();
        
    }

    public async Task<bool> Exists(string name, bool sideEditor = false)
    {
        try
        {
            logger.ILog("Check table item exists: " + name);
            var locator = ItemLocator(name, sideEditor);
            bool exists = await locator.CountAsync() > 0;
            if (exists)
            {
                await locator.First.HighlightAsync();
                logger.ILog("Table item exists: " + name);
            }
            else
                logger.ILog("Table item does not exist: " + name);
            return exists;
        }
        catch (Exception ex)
        {
            logger.WLog("Failed checking table item exists:" + ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Waits for a skybox item to exist by its name.
    /// </summary>
    /// <param name="name">The name of the item to check.</param>
    /// <param name="sideEditor">Whether to check within the side editor (optional).</param>
    /// <param name="timeout">The maximum time to wait for the item to exist (in milliseconds).</param>
    /// <returns>True if the item exists within the timeout, otherwise false.</returns>
    public async Task<bool> WaitForExists(string name, bool sideEditor = false, int timeout = 30000)
    {
        var locator = ItemLocator(name, sideEditor);

        try
        {
            // Wait for the item to be visible within the timeout (default is 30 seconds)
            await locator.WaitForAsync(new LocatorWaitForOptions { Timeout = timeout });
            return true; // Item exists
        }
        catch (TimeoutException)
        {
            // If the timeout is reached and the item is not found, return false
            return false;
        }
    }


    public Task ItemDoesNotExist(string name, bool sideEditor = false)
        => Expect(ItemLocator(name, sideEditor)).ToHaveCountAsync(0);

    public Task DoubleClick(string name)
        => ItemLocator(name, false).First.DblClickAsync();
}