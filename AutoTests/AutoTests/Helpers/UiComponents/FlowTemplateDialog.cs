namespace FileFlowsTests.Helpers.UiComponents;

/// <summary>
/// Helper for interacting with Flow Template Dialog
/// </summary>
/// <param name="test">the executing test</param>
public class FlowTemplateDialog(TestBase test) : UiComponent(test)
{
    /// <summary>
    /// Select a flow template
    /// </summary>
    /// <param name="templateName">the name of the template</param>
    /// <returns>the task to await</returns>
    public Task Select(string templateName)
        => Page.Locator($".flow-modal-body .templates .template .name >> text='{templateName}'").DblClickAsync();

    /// <summary>
    /// Gets the templates on the current page
    /// </summary>
    /// <returns>the templates</returns>
    public async Task<List<FlowTemplate>> GetTemplates()
    {
        List<FlowTemplate> items = new();
        
        // Wait for at least one element to be present
        await Page.WaitForSelectorAsync(".flow-modal-body .templates .template");

        // Now you can get the rows after ensuring they exist
        var rows = Page.Locator(".flow-modal-body .templates .template");

        // Get the count of rows and iterate through each row
        int rowCount = await rows.CountAsync();
        for (int i = 0; i < rowCount; i++)
        {
            var row = rows.Nth(i);
            FlowTemplate item = new();
            item.Name = await row.Locator(".name").TextContentAsync() ?? string.Empty;
            item.Description = await row.Locator(".description").TextContentAsync() ?? string.Empty;
            item.Author = await row.Locator(".author").TextContentAsync() ?? string.Empty;
            items.Add(item);
        }
    
        return items;
        
    }

    /// <summary>
    /// A flow template
    /// </summary>
    public class FlowTemplate
    {
        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the author
        /// </summary>
        public string Author { get; set; } = string.Empty;
    }
}