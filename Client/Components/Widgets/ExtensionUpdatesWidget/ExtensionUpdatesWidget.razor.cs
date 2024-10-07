using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Extension Updates widget
/// </summary>
public partial class ExtensionUpdatesWidget : ComponentBase
{
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode { get; set; }
    /// <summary>
    /// Gets or sets the update data
    /// </summary>
    [Parameter] public UpdateInfo Data { get; set; }

    /// <summary>
    /// Translations
    /// </summary>
    private string lblTitle, lblPlugins, lblScripts, lblDockerMods;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("MenuGroups.Extensions");
        lblPlugins = Translater.Instant("Pages.Dashboard.Widgets.Updates.Plugins", new { count = Data.PluginUpdates.Count });
        lblScripts = Translater.Instant("Pages.Dashboard.Widgets.Updates.Scripts", new { count = Data.ScriptUpdates.Count });
        lblDockerMods = Translater.Instant("Pages.Dashboard.Widgets.Updates.DockerMods", new { count = Data.DockerModUpdates.Count });

        if (Data.PluginUpdates.Count > 0)
            Mode = 0;
        else if (Data.ScriptUpdates.Count > 0)
            Mode = 1;
        else if (Data.DockerModUpdates.Count > 0)
            Mode = 2;
    }
    
    /// <summary>
    /// Gets the selected list
    /// </summary>
    private List<PackageUpdate> SelectedList => Mode switch
    {
        0 => Data.PluginUpdates,
        1 => Data.ScriptUpdates,
        2 => Data.DockerModUpdates,
        _ => new()
    };
    
    /// <summary>
    /// Gets the default icon
    /// </summary>
    private string DefaultIcon => Mode switch
    {
        0 => "fas fa-puzzle-piece",
        1 => "fas fa-scroll",
        2 => "fab fa-docker",
        _ => string.Empty
    };
}