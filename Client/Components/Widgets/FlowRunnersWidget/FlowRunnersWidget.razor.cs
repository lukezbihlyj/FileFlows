using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Flow Runners widget
/// </summary>
public partial class FlowRunnersWidget : ComponentBase
{
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode { get; set; }

    /// <summary>
    /// The update data
    /// </summary>
    public UpdateInfo? UpdateInfoData;

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblTitle, lblRunners, lblUpdates, lblNodes;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.System.Title");
        lblRunners = Translater.Instant("Pages.Dashboard.Widgets.System.Runners");
        lblNodes = Translater.Instant("Pages.Nodes.Title");
        await Refresh();
        if (UpdateInfoData.HasUpdates)
            Mode = 1;
        lblUpdates = Translater.Instant("Pages.Dashboard.Widgets.System.Updates", new { count = UpdateInfoData.NumberOfUpdates});
    }

    /// <summary>
    /// Refreshes the data
    /// </summary>
    async Task Refresh()
    {
        var result = await HttpHelper.Get<UpdateInfo>("/api/dashboard/updates");
        UpdateInfoData = result.Success ? result.Data ?? new() : new();
    }

    /// <summary>
    /// Select nodes if no runners are running on load
    /// </summary>
    private void SelectNodes()
    {
        if (UpdateInfoData == null)
            return;
        
        if (UpdateInfoData.HasUpdates)
            Mode = 1;
        else
            Mode = 2;
        StateHasChanged();
    }
}