namespace FileFlowsTests.Helpers.UiComponents;

/// <summary>
/// Initial Configuration helper
/// </summary>
/// <param name="test">the test running</param>
public class InitialConfiguration(TestBase test) : UiComponent(test)
{
    /// <summary>
    /// Gets if the initial configuration page is shown
    /// </summary>
    /// <returns>true if it is shown, otherwise false</returns>
    public async Task<bool> Shown()
    {
        var ele  = await Page.WaitForSelectorAsync(".initial-config .flow-wizard");
        return ele != null;
    }

    /// <summary>
    /// Gets the wizard page title
    /// </summary>
    /// <returns>the title</returns>
    public Task<string?> GetPageTitle()
        => Page.Locator(".flow-wizard-content .page-description").TextContentAsync();

    /// <summary>
    /// Gets if a page is enabled
    /// </summary>
    /// <param name="title">the page title</param>
    /// <returns>true if enabled, otherwise false</returns>
    public async Task<bool> PageEnabled(string title)
        => await Page.Locator($".initial-config .flow-wizard-buttons .wb-{title}:not(.disabled)").CountAsync() == 1;
    
    /// <summary>
    /// Clicks the next button
    /// </summary>
    /// <returns>the task to await</returns>
    public Task NextClick()
        => Page.Locator(".flow-wizard-navigation-buttons .next").ClickAsync();
    
    /// <summary>
    /// Clicks the previous button
    /// </summary>
    /// <returns>the task to await</returns>
    public Task PreviousClick()
        => Page.Locator(".flow-wizard-navigation-buttons .previous").ClickAsync();
    
    /// <summary>
    /// Clicks the finish button
    /// </summary>
    /// <returns>the task to await</returns>
    public Task FinishClick()
        => Page.Locator(".flow-wizard-navigation-buttons .finish").ClickAsync();

    /// <summary>
    /// Gets if the next button is disabled
    /// </summary>
    /// <returns>true if disabled, otherwise false</returns>
    public async Task<bool> NextDisabled()
        => await Page.Locator(".flow-wizard-navigation-buttons .next.disabled").CountAsync() == 1;

    /// <summary>
    /// Gets if the previous button is disabled
    /// </summary>
    /// <returns>true if disabled, otherwise false</returns>
    public async Task<bool> PreviousDisabled()
        => await Page.Locator(".flow-wizard-navigation-buttons .previous.disabled").CountAsync() == 1;

    /// <summary>
    /// Gets if the previous button is shown
    /// </summary>
    /// <returns>true if the button is shown</returns>
    public async Task<bool?> PreviousButtonShown()
        => await Page.Locator(".flow-wizard-navigation-buttons .previous").CountAsync() == 1;

    /// <summary>
    /// Gets if the next button is shown
    /// </summary>
    /// <returns>true if the button is shown</returns>
    public async Task<bool?> NextButtonShown()
        => await Page.Locator(".flow-wizard-navigation-buttons .next").CountAsync() == 1;
    
    /// <summary>
    /// Gets if the finish button is shown
    /// </summary>
    /// <returns>true if the button is shown</returns>
    public async Task<bool?> FinishButtonShown()
        => await Page.Locator(".flow-wizard-navigation-buttons .finish").CountAsync() == 1;

    /// <summary>
    /// Clicks the accept eula button
    /// </summary>
    public Task AcceptEula()
        => Page.Locator(".eula-page .switch > span").ClickAsync();


    /// <summary>
    /// Gets the items on the current page
    /// </summary>
    /// <returns>the items</returns>
    public async Task<List<InitialConfigItem>> GetItems()
    {
        List<InitialConfigItem> items = new();
        var rows = Page.Locator(".flow-wizard-content .flow-page.active .flowtable-data .flowtable-row");
        await rows.First.WaitForAsync(new LocatorWaitForOptions()
        {
            State = WaitForSelectorState.Visible
        });

        // Get the count of rows and iterate through each row
        int rowCount = await rows.CountAsync();
        for (int i = 0; i < rowCount; i++)
        {
            var row = rows.Nth(i);
            InitialConfigItem item = new();

            // Check if the checkbox is checked
            item.Checked = await row.Locator("input[type=checkbox]").IsCheckedAsync();

            // Extract text content
            item.Name = await row.Locator(".name").TextContentAsync() ?? string.Empty;
            item.Description = await row.Locator(".description").TextContentAsync() ?? string.Empty;
            
            // Check if the Installed text is "Installed"
            var installedElement = row.Locator(".right .top-right");
            if (await installedElement.CountAsync() > 0)
                item.Installed = (await installedElement.TextContentAsync()) == "Installed";

            items.Add(item);
        }
    
        return items;
    }
    

    /// <summary>
    /// An initial configuration data list item
    /// </summary>
    public class InitialConfigItem
    {
        /// <summary>
        /// Gets or sets if this is checked
        /// </summary>
        public bool Checked { get; set; }
    
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets if this is already installed
        /// </summary>
        public bool Installed { get; set; }
    }
}