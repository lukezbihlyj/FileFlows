using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class FlowRunnersWidget : ComponentBase
{
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode { get; set; }

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblTitle, lblUpdates;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.FlowRunners.Title");
        lblUpdates = Translater.Instant("Pages.Dashboard.Widgets.Updates.Title");
    }
}