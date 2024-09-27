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
    protected async Task CreateFlow(string name, string template, FlowField[]? parameters = null)
    {
        await GotoPage("Flows");
        await TableButtonClick("Add");
        await SelectTemplate(template);
        if (parameters?.Length > 0)
        {
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
                            await SetText(field.Name.Replace(" ", string.Empty), (string)field.Value);
                            break;
                        case InputType.Select:
                            await SetSelect(field.Name.Replace(" ", string.Empty), (string)field.Value);
                            break;
                        case InputType.Toggle:
                            await SetToggle(field.Name.Replace(" ", string.Empty), (bool)field.Value);
                            break;
                    }
                }
            }

            await ButtonClick("Save");
        }
        else
        {
            await FileFlows.Flow.SetTitle(name);
            await FileFlows.Flow.Save();
        }
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
        string libPath = Path.Combine(TempPath, "lib-" + Guid.NewGuid());
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
    /// Creates a folder library
    /// </summary>
    /// <param name="name">the name of the library</param>
    /// <param name="flow">the flow to use</param>
    /// <param name="libPath">library path to use, else one will be created</param>
    /// <param name="template">the template to use</param>
    /// <param name="scan">if the library should be scanned</param>
    /// <param name="fingerprinting">If the library should do fingerprinting</param>
    /// <returns>the full path to the library</returns>
    protected async Task<string> CreateBasicLibrary(string name, string flow, string libPath, string template = "File", 
        bool scan = false, bool fingerprinting = false)
    {
        await GotoPage("Libraries", forceLoad: true);
        await TableButtonClick("Add");
        await EditorTitle("Library");
        await Task.Delay(250);
        await SetSelect("Template", template);
        await SetText("Name", name);
        await SetText("Path", libPath);
        await SetSelect("Flow", flow);

        if (fingerprinting)
        {
            await SelectTab("Advanced");
            await SetToggle("UseFingerprinting", fingerprinting);
        }
        
        await ButtonClick("Save");
        await Task.Delay(500);
        await SelectItem(name);

        if (scan)
        {
            try
            {
                await TableButtonClick("Rescan");
            }
            catch (Exception)
            {
            }
        }

        return libPath;
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

    
    
    /// <summary>
    /// Recursively copies all contents from the source directory to the target directory.
    /// </summary>
    /// <param name="sourceDir">The path of the source directory</param>
    /// <param name="targetDir">The path of the target directory</param>
    private void CopyDirectory(string sourceDir, string targetDir)
    {
        // Get the directory info for the source directory
        var dir = new DirectoryInfo(sourceDir);
    
        // Ensure the source directory exists
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }

        // Create the target directory if it doesn't exist
        Directory.CreateDirectory(targetDir);

        // Copy all files from the source to the target directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(targetDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // Recursively copy subdirectories
        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
            string newTargetDir = Path.Combine(targetDir, subDir.Name);
            CopyDirectory(subDir.FullName, newTargetDir); // Recursion for subdirectories
        }
    }

    /// <summary>
    /// Checks if a file was processed
    /// </summary>
    /// <param name="fileName">the name of the file (or folder)</param>
    protected async Task CheckFileProcessed(string fileName)
    {
        Logger.ILog("Checking file processed: " + fileName);
        await GotoPage("Files");
        await SkyBox("Processed");
        await Task.Delay(5_000);
        DateTime end = DateTime.Now.AddMinutes(2);
        while (end > DateTime.Now)
        {
            await SkyBox("Processed");
            if (await ItemExists(fileName))
                break;
        
            await Task.Delay(1000);
        }
        
        await DoubleClickItem(fileName);

        string log = await DownloadLog();
        Logger.ILog(new string('-', 100) + Environment.NewLine + log);
        Logger.ILog(new string('-', 100));
    }
}
