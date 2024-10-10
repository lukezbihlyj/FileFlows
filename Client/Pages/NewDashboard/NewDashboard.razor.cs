using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class NewDashboard : ComponentBase
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] public ProfileService ProfileService { get; set; }

    /// <summary>
    /// The users profile
    /// </summary>
    private Profile Profile;
    
    /// <summary>
    /// The update data
    /// </summary>
    public UpdateInfo? UpdateInfoData;

    /// <summary>
    /// The tabs
    /// </summary>
    private FlowTabs Tabs;

    private string lblDashboard, lblSavings, lblUpdates;

    private bool loaded = false;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblDashboard = Translater.Instant("Pages.Dashboard.Tabs.Dashboard");
        lblSavings = Translater.Instant("Pages.Dashboard.Tabs.Savings");
        if (ClientService.CurrentSystemInfo == null)
        {
            var infoResult = await HttpHelper.Get<SystemInfo>("/api/dashboard/info");
            if (infoResult.Success)
                ClientService.CurrentSystemInfo ??= infoResult.Data;
        }
        if(ClientService.CurrentFileOverData == null)
        {
            var fileOverviewResult = await HttpHelper.Get<FileOverviewData>("/api/dashboard/file-overview");
            if (fileOverviewResult.Success)
                ClientService.CurrentFileOverData ??= fileOverviewResult.Data;
        }
        
        Profile = await ProfileService.Get();

        await Refresh();

        
        loaded = true;
        StateHasChanged();
    }
    
    /// <summary>
    /// Refreshes the data
    /// </summary>
    async Task Refresh()
    {
        var result = await HttpHelper.Get<UpdateInfo>("/api/dashboard/updates");
        UpdateInfoData = result.Success ? result.Data ?? new() : new();
        lblUpdates = Translater.Instant("Pages.Dashboard.Widgets.System.Updates",
            new { count = UpdateInfoData.NumberOfUpdates });
    }

    /// <summary>
    /// On Extension update
    /// </summary>
    private async Task OnUpdate()
    {
        await Refresh();
        StateHasChanged();
    }
    
    /// <summary>
    /// Called when Updates are clicked from the status widget
    /// </summary>
    private void Status_OnUpdatesClicked()
    {
        if (UpdateInfoData.HasUpdates)
        {
            Tabs?.SelectTabByUid("updates");
        }
    }
}