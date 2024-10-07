using System.Threading;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Node summary component
/// </summary>
public partial class NodeSummaryComponent : ComponentBase, IDisposable
{
    /// <summary>
    /// The data
    /// </summary>
    private List<ProcessingNode> Data = new();
    /// <summary>
    /// The timer
    /// </summary>
    private Timer _timer;

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblOperatingSystem, lblArchitecture, lblMemory, lblDisabled;
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblOperatingSystem = Translater.Instant("Labels.OperatingSystem");
        lblArchitecture = Translater.Instant("Labels.Architecture");
        lblMemory = Translater.Instant("Labels.Memory");
        lblDisabled =Translater.Instant("Labels.Disabled");
        await Refresh();
        _timer = new Timer(RefreshCallback!, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }
    
    /// <summary>
    /// Refreshes the data
    /// </summary>
    /// <param name="state">the state from the timer</param>
    private async void RefreshCallback(object state)
    {
        await Refresh();
        StateHasChanged();
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

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}