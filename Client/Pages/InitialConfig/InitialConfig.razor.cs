using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Models;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Initial configuration page
/// </summary>
public partial class InitialConfig : ComponentBase
{
    /// <summary>
    /// Gets or sets blocker instance
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    public Editor Editor { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    private Profile Profile { get; set; }

    /// <summary>
    /// The markup string of the EULA
    /// </summary>
    private MarkupString msEula;
    
    /// <summary>
    /// Gets or sets if the EULA has been accepted
    /// </summary>
    private bool EulaAccepted { get; set; }

    /// <summary>
    /// Gets or sets the number of runners
    /// </summary>
    private int Runners { get; set; } = 3;
    
    /// <summary>
    /// Gets or sets a list of available plugins
    /// </summary>
    private List<PluginPackageInfo> AvailablePlugins { get; set; }
    /// <summary>
    /// The plugins that are forced checked and cannot be unchecked
    /// These are plugins that are already installed
    /// </summary>
    private List<PluginPackageInfo> ForcedPlugins;

    /// <summary>
    /// Gets or sets a list of available DockerMods
    /// </summary>
    private List<RepositoryObject> AvailableDockerMods { get; set; }

    /// <summary>
    /// The Plugin flow table
    /// </summary>
    private FlowTable<PluginPackageInfo> PluginTable;

    /// <summary>
    /// The DockerMod flow table
    /// </summary>
    private FlowTable<RepositoryObject> DockerModTable;
    /// <summary>
    /// If this component is fully loaded or not.
    /// Is false until the plugins have been loaded which may take a second or two
    /// </summary>
    private bool loaded;
    /// <summary>
    /// The language options
    /// </summary>
    private List<IconListOption> LanguageOptions = new ();

    /// <summary>
    /// The reference to the wizard
    /// </summary>
    private FlowWizard? Wizard;

    /// <summary>
    /// If only the EULA needs accepting
    /// </summary>
    private bool onlyEula;

    private string _SelectedLanguage;

    /// <summary>
    /// Gets or sets the selected language
    /// </summary>
    private object SelectedLanguage
    {
        get => _SelectedLanguage;
        set
        {
            if ((string)value != _SelectedLanguage)
            {
                _SelectedLanguage = (string)value;
                _ = UpdateLabels(_SelectedLanguage);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Logger.Instance.ILog("InitialConfig: OnInitializedAsync");
        Blocker.Show("Labels.Loading");
        Profile = await ProfileService.Get();
        #if(DEBUG)
        Profile.ServerOS = OperatingSystemType.Docker;
        #endif
        _SelectedLanguage = Profile.Language?.EmptyAsNull() ?? "en";
        
        LanguageOptions = Profile.LanguageOptions?.Select(x =>
            new IconListOption()
            {
                Label = x.Label,
                Value = x.Value,
                IconUrl = $"/icons/flags/{x.Value}.svg"
            }
        ).ToList();
        await UpdateLabels();
        if (Profile.IsAdmin == false)
        {
            await ProfileService.Logout("Labels.AdminRequired");
            return;
        }

        if ((Profile.ConfigurationStatus & ConfigurationStatus.InitialConfig) == ConfigurationStatus.InitialConfig &&
            (Profile.ConfigurationStatus & ConfigurationStatus.EulaAccepted) == ConfigurationStatus.EulaAccepted)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
        var html = Markdig.Markdown.ToHtml(EULA).Trim();
        msEula = new MarkupString(html);

        // only show plugins if they haven't configured the system yet
        onlyEula = (Profile.ConfigurationStatus & ConfigurationStatus.InitialConfig) ==
                   ConfigurationStatus.InitialConfig;
        if (onlyEula == false)
        {
            await GetPlugins();
            await GetDockerMods();
            StateHasChanged();
        }

        Blocker.Hide();
        loaded = true;
    }

    /// <summary>
    /// Gets the plugins from the backend
    /// </summary>
    private async Task GetPlugins()
    {
        var request = await HttpHelper.Get<List<PluginPackageInfo>>("/api/plugin/plugin-packages");
        if (request.Success == false)
            return;

        AvailablePlugins = request.Data.OrderBy(x => x.Installed ? 0 : 1)
            .ThenBy(x => x.Name.ToLowerInvariant()).ToList();
        ForcedPlugins = AvailablePlugins.Where(x => x.Installed).ToList();
    }

    /// <summary>
    /// Gets the DockerMods from the backend
    /// </summary>
    private async Task GetDockerMods()
    {
        var request = await HttpHelper.Get<List<RepositoryObject>>("/api/repository/by-type/DockerMod");
        if (request.Success == false)
        {
            Logger.Instance.ILog("Failed to get DockerMods: " + request.StatusCode);
            return;
        }

        Logger.Instance.ILog("Got DockerMods 1");
        AvailableDockerMods = request.Data
            .OrderBy(x => x.Default == true ? 0 : 1)
            .ThenBy(x => x.Name.ToLowerInvariant()?.StartsWith("ffmpeg") == true ? 0 : 1)
            .ThenBy(x => x.Name.ToLowerInvariant()).ToList();
        
        Logger.Instance.ILog("Got DockerMods 2: " + AvailableDockerMods.Count);
    }

    /// <summary>
    /// Savss the initial configuration
    /// </summary>
    private async Task Save()
    {
        if (EulaAccepted == false)
        {
            Toast.ShowError("Accept the EULA to continue.");
            return;
        }

        var plugins = onlyEula ? null : PluginTable?.GetSelected()?.ToList();
        var dockerMods = onlyEula ? null : DockerModTable?.GetSelected()?.ToList();
        
        Blocker.Show("Labels.Saving");
        try
        {
            var result = await HttpHelper.Post("/api/settings/initial-config", new
            {
                EulaAccepted,
                Plugins = plugins,
                DockerMods = dockerMods,
                Language = SelectedLanguage as string ?? "en",
                Runners
            });
            if (result.Success)
            {
                await ProfileService.Refresh();
                if(onlyEula)
                    NavigationManager.NavigateTo("/");
                else if ((Profile.ConfigurationStatus & ConfigurationStatus.Flows) != ConfigurationStatus.Flows)
                    NavigationManager.NavigateTo("/flows/00000000-0000-0000-0000-000000000000", Profile.IsWebView);
                else if((Profile.ConfigurationStatus & ConfigurationStatus.Libraries) != ConfigurationStatus.Libraries)
                    NavigationManager.NavigateTo("/libraries");
                else
                    NavigationManager.NavigateTo("/");
                return;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        Toast.ShowError("Failed to save initial configuration.");
        Blocker.Hide();
    }

    private bool InitDone = false;

    /// <summary>
    /// Toggles the EULA has been accepted
    /// </summary>
    private void ToggleEulaAccepted()
    {
        EulaAccepted = !EulaAccepted;
        if (InitDone == false)
        {
            InitDone = true;
            
            PluginTable.SetSelected(AvailablePlugins.Where(x => x.Name is "Basic" or "Audio" or "Video" or "Image" or "Web").ToArray());
            DockerModTable.SetSelected(AvailableDockerMods.Where(x => x.Default == true).ToArray());
        }
    }
    
    // labels used for translations
    private string lblWelcomeMessage, lblWelcomeMessageUpdate, lblWelcome, lblWelcomeDescription, lblEula,
        lblEulaDescription, lblEulaAccept, lblPlugins, lblPluginsDescription, lblDockerMods, lblDockerModsDescription, lblFinish,
        lblFinishDescription, lblFinishTop, lblFinishCreateFirstFlow, lblFinishCreateFirstFlowDescription, lblFinishCreateALibrary,
        lblFinishCreateALibraryDescription, lblFinishBottom, lblInstalled, lblRunners, lblRunnersDescription, lblRunnersTop;
    
    /// <summary>
    /// Updates the labels
    /// </summary>
    /// <param name="language">Optional new language to load</param>
    private async Task UpdateLabels(string? language = null)
    {
        if(language != null)
            await App.Instance.LoadLanguage(language);
        lblWelcomeMessage = Translater.Instant("Pages.InitialConfig.Messages.Welcome");
        lblWelcomeMessageUpdate = Translater.Instant("Pages.InitialConfig.Messages.WelcomeUpdate"); 
        lblWelcome = Translater.Instant("Pages.InitialConfig.Tabs.Welcome");
        lblWelcomeDescription = Translater.Instant("Pages.InitialConfig.Tabs.WelcomeDescription");
        lblEula = Translater.Instant("Pages.InitialConfig.Tabs.Eula");
        lblEulaDescription = Translater.Instant("Pages.InitialConfig.Tabs.EulaDescription");
        lblEulaAccept = Translater.Instant("Pages.InitialConfig.Fields.EulaAccept");
        lblPlugins = Translater.Instant("Pages.InitialConfig.Tabs.Plugins");
        lblPluginsDescription = Translater.Instant("Pages.InitialConfig.Tabs.PluginsDescription");
        lblDockerMods = Translater.Instant("Pages.InitialConfig.Tabs.DockerMods");
        lblDockerModsDescription = Translater.Instant("Pages.InitialConfig.Tabs.DockerModsDescription");
        lblFinish = Translater.Instant("Pages.InitialConfig.Tabs.Finish");
        lblFinishDescription = Translater.Instant("Pages.InitialConfig.Tabs.FinishDescription");
        lblFinishTop = Translater.Instant("Pages.InitialConfig.Messages.Finish.Top");
        lblFinishCreateFirstFlow = Translater.Instant("Pages.InitialConfig.Messages.Finish.CreateFirstFlow");
        lblFinishCreateFirstFlowDescription= Translater.Instant("Pages.InitialConfig.Messages.Finish.CreateFirstFlowDescription");
        lblFinishCreateALibrary = Translater.Instant("Pages.InitialConfig.Messages.Finish.CreateALibrary");
        lblFinishCreateALibraryDescription= Translater.Instant("Pages.InitialConfig.Messages.Finish.CreateALibraryDescription");
        lblFinishBottom =Translater.Instant("Pages.InitialConfig.Messages.Finish.Bottom");
        lblInstalled = Translater.Instant("Labels.Installed");
        lblRunners = Translater.Instant("Pages.InitialConfig.Tabs.Runners");
        lblRunnersDescription = Translater.Instant("Pages.InitialConfig.Tabs.RunnersDescription");
        lblRunnersTop = Translater.Instant("Pages.InitialConfig.Messages.RunnersTop");
        StateHasChanged();
        Wizard?.TriggerStateHasChanged();
    }
}