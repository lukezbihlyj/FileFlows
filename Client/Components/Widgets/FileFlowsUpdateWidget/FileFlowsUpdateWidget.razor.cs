using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// FileFlows Update widget
/// </summary>
public partial class FileFlowsUpdateWidget : ComponentBase
{
    /// <summary>
    /// Translations
    /// </summary>
    private string lblNew, lblFixed;
    
    /// <summary>
    /// Gets or sets the update data
    /// </summary>
    [Parameter] public UpdateInfo Data { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblNew = Translater.Instant("Pages.Dashboard.Widgets.Updates.FileFlows.New");
        lblFixed = Translater.Instant("Pages.Dashboard.Widgets.Updates.FileFlows.Fixed");
    }
}