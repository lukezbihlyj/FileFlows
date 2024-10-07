using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class NodeSummaryComponent : ComponentBase
{
    /// <summary>
    /// The data
    /// </summary>
    private List<ProcessingNode> Data = new();

    // /// <summary>
    // /// Translation strings
    // /// </summary>
    // private string lblPlugin, lblScript;
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await Refresh();
    }

    /// <summary>
    /// Refreshes the data
    /// </summary>
    async Task Refresh()
    {
        var result = await HttpHelper.Get<List<ProcessingNode>>("/api/dashboard/node-summary");
        Data = result.Success ? result.Data ?? new() : new();
        foreach (var d in Data)
        {
            if(d.Name == CommonVariables.InternalNodeName)
                d.Name = Translater.Instant("Labels.InternalProcessingNode");
        }
    }
}