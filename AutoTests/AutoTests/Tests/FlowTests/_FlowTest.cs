namespace FileFlowsTests.Tests.FlowTests;

/// <summary>
/// A flow test that test the flow editor and processing
/// </summary>
public abstract class FlowTest():TestBase("Flows")
{
    /// <summary>
    /// Creates a flow
    /// </summary>
    /// <param name="name">the name of the flow</param>
    /// <param name="template">the template to use</param>
    /// <param name="parameters">the parameters to fill out</param>
    protected async Task CreateFlow(string name, string template, IEnumerable<FlowField> parameters)
    {
        await GotoPage("Flows");
        await TableButtonClick("Add");
        await SelectTemplate(template);
        await EditorTitle(template);
        await SetText("Name", name);
        var flowFields = parameters as FlowField[] ?? parameters.ToArray();
        if (flowFields?.Any() == true)
        {
            foreach (var field in flowFields)
            {
                switch (field.Type)
                {
                    case InputType.Text:
                        await SetText(field.Name.Replace(" ", string.Empty), (string)field.Value); break;
                    case InputType.Select:
                        await SetSelect(field.Name.Replace(" ", string.Empty), (string)field.Value); break;   
                    case InputType.Toggle:
                        await SetToggle(field.Name.Replace(" ", string.Empty), (bool)field.Value); break;
                }
            }
        }

        await ButtonClick("Save");
    }

    /// <summary>
    /// Creates a library
    /// </summary>
    /// <param name="file">the file to base the library around</param>
    /// <param name="library">the library</param>
    /// <param name="scan">if the library should be scanned</param>
    /// <returns>the result</returns>
    protected async Task<string> CreateLibrary(string file, Library library, bool scan = false)
    {
        string libPath = Path.Combine(TempPath, "lig-" + Guid.NewGuid());
        Directory.CreateDirectory(libPath);
        string shortName = Guid.NewGuid() + new FileInfo(file).Extension;
        File.Copy(file, Path.Combine(libPath, shortName));
        library.Path = libPath;
        
        await GotoPage("Libraries");
        await TableButtonClick("Add");
        await EditorTitle("Library");
        if (string.IsNullOrWhiteSpace(library.Template) == false)
            await SetSelect("Template", library.Template);
        await SetText("Name", library.Name);
        await SetText("Path", library.Path);
        await SetSelect("Flow", library.Flow);
        if (library.Priority != null)
            await SetSelect("Priority", library.Priority.Value.ToString());
        if (library.ProcessingOrder != null)
            await SetSelect("ProcessingOrder", library.ProcessingOrder.Value switch
            {
                LibraryProcessingOrder.AsFound => "As Found (Default)",
                LibraryProcessingOrder.LargestFirst => "Largest First", 
                LibraryProcessingOrder.SmallestFirst => "Smallest First",
                LibraryProcessingOrder.NewestFirst => "Newest First",
                _ => library.ProcessingOrder.Value.ToString()
            });
        if (library.HoldMinutes != null)
            await SetNumber("HoldMinutes", library.HoldMinutes.Value);
        if (library.Enabled != null)
            await SetToggle("Enabled", library.Enabled.Value);

        await SelectTab("Advanced");
        if (library.Filters?.Length > 0)
            await SetArray("Filters", library.Filters);
        if (library.ExclusionFilters?.Length > 0)
            await SetArray("ExclusionFilters", library.ExclusionFilters);
        if (library.UseFingerprinting != null)
            await SetToggle("UseFingerprinting", library.UseFingerprinting.Value);
        if (library.Scan != null)
            await SetToggle("Scan", library.Scan.Value);
        
        await ButtonClick("Save");
        await SelectItem(library.Name);

        if (scan)
            await TableButtonClick("Rescan");

        return shortName;
    }

    /// <summary>
    /// Selects a flow part
    /// </summary>
    /// <param name="name">the name of the flow part</param>
    /// <returns>the task to await</returns>
    public Task FlowPartSelect(string name) => FileFlows.Flow.Select(name);
    /// <summary>
    /// Edits a flow part
    /// </summary>
    /// <param name="name">the name of the flow part</param>
    /// <returns>the task to await</returns>
    public Task FlowPartEdit(string name) => FileFlows.Flow.Edit(name);
    /// <summary>
    /// Deletes a flow part
    /// </summary>
    /// <param name="name">the name of the flow part</param>
    /// <returns>the task to await</returns>
    public Task FlowPartDelete(string name) => FileFlows.Flow.Delete(name);


    /// <summary>
    /// Selects a template in the flow template wizard
    /// </summary>
    /// <param name="name">the name of the template</param>
    protected async Task SelectTemplate(string name)
    {
        var txtFilter = Page.Locator("#flow-template-picker-filter");
        await txtFilter.FillAsync(name);
        await txtFilter.PressAsync("Enter");

        await Page.Locator($".template[x-name='{name}']").ClickAsync();

        await Page.Locator("#flow-template-picker-button-next").ClickAsync();
    }

    /// <summary>
    /// Downloads a file log
    /// </summary>
    /// <returns>the file log</returns>
    protected async Task<string> DownloadLog()
    {
        // Start the task of waiting for the download
        var waitForDownloadTask = Page.WaitForDownloadAsync();
        // Perform the action that initiates download
        await Page.Locator("button >> text='Download Log'").ClickAsync();
        // Wait for the download process to complete
        var download = await waitForDownloadTask;
        Logger.ILog(await download.PathAsync() ?? "Path is null");
        // Save downloaded file somewhere
        string file = Path.GetTempFileName() + ".log";
        await download.SaveAsAsync(file);
        return await File.ReadAllTextAsync(file);
    }

}
