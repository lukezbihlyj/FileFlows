using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Node summary component
/// </summary>
public partial class NodeSummaryComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    /// <summary>
    /// The data
    /// </summary>
    private List<NodeStatusSummary> Data = new();

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblOperatingSystem, lblArchitecture, lblMemory, lblStatus, lblInternalProcessingNode, lblRunners, lblPriority;
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblOperatingSystem = Translater.Instant("Labels.OperatingSystem");
        lblArchitecture = Translater.Instant("Labels.Architecture");
        lblMemory = Translater.Instant("Labels.Memory");
        lblStatus = Translater.Instant("Labels.Status");
        lblRunners = Translater.Instant("Pages.Nodes.Labels.Runners");
        lblPriority = Translater.Instant("Pages.ProcessingNode.Fields.Priority");
        lblInternalProcessingNode = Translater.Instant("Labels.InternalProcessingNode");
        if(ClientService.CurrentNodeStatusSummaries == null)
        {
            var result = await HttpHelper.Get<List<NodeStatusSummary>>("/api/dashboard/node-summary");
            if (result.Success)
                ClientService.CurrentNodeStatusSummaries ??= result.Data;
        }
        Data = ClientService.CurrentNodeStatusSummaries ?? [];
        ClientService.NodeStatusSummaryUpdated += OnNodeStatusSummaryUpdated;
    }

    /// <summary>
    /// Raised when the node status summaries are updated
    /// </summary>
    /// <param name="data">the updated data</param>
    private void OnNodeStatusSummaryUpdated(List<NodeStatusSummary> data)
    {
        Data = data;
        StateHasChanged();
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        ClientService.NodeStatusSummaryUpdated -= OnNodeStatusSummaryUpdated;
    }
}