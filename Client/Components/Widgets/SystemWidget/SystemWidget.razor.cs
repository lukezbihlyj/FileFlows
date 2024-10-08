using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Flow Runners widget
/// </summary>
public partial class SystemWidget : ComponentBase
{
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode { get; set; }

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblTitle, lblRunners, lblNodes, lblSavings;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.System.Title");
        lblRunners = Translater.Instant("Pages.Dashboard.Widgets.System.Runners");
        lblNodes = Translater.Instant("Pages.Nodes.Title");
        lblSavings = Translater.Instant("Pages.Dashboard.Tabs.Savings");
    }


    /// <summary>
    /// Select nodes if no runners are running on load
    /// </summary>
    private void SelectNodes()
    {
        Mode = 2;
        StateHasChanged();
    }
}