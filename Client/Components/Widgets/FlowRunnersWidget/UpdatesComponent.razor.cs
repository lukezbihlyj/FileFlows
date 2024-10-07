using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class UpdatesComponent : ComponentBase
{
    /// <summary>
    /// The update data
    /// </summary>
    [Parameter]
    public UpdateInfo Data { get; set; } = new();

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblPlugin, lblScript;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblPlugin = Translater.Instant("Pages.Plugins.Single");
        lblScript = Translater.Instant("Pages.Script.Title.Script");
    }
}