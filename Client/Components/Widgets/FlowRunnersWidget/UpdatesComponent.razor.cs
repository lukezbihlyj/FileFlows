using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class UpdatesComponent : ComponentBase
{
    /// <summary>
    /// The update data
    /// </summary>
    private UpdateInfo Data = new();

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblPlugin, lblScript;
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblPlugin = Translater.Instant("Pages.Plugins.Single");
        lblScript = Translater.Instant("Pages.Script.Title.Script");
        await Refresh();
    }

    /// <summary>
    /// Refreshes the data
    /// </summary>
    async Task Refresh()
    {
        var result = await HttpHelper.Get<UpdateInfo>("/api/dashboard/updates");
        Data = result.Success ? result.Data ?? new() : new();
    }
}