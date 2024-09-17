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
        await EditorTitle("Add Flow");
        await SetText("Name", name);
        await SetSelect("Template", template);
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
        await SelectItem(name);
    }

    /// <summary>
    /// Creates a library
    /// </summary>
    /// <param name="library">the library</param>
    /// <param name="scan">if the library should be scanned</param>
    protected async Task CreateLibrary(Library library, bool scan = false)
    {
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
        if (library.Filter != null)
            await SetText("Filter", library.Filter);
        if (library.ExclusionFilter != null)
            await SetText("ExclusionFilter", library.ExclusionFilter);
        if (library.UseFingerprinting != null)
            await SetToggle("UseFingerprinting", library.UseFingerprinting.Value);
        if (library.Scan != null)
            await SetToggle("Scan", library.Scan.Value);
        
        await ButtonClick("Save");
        await SelectItem(library.Name);

        if (scan)
            await TableButtonClick("Rescan");
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

}
