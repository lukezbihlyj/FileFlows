using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// New Dashboard
/// </summary>
public partial class NewDashboard : ComponentBase, IDisposable
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
        UpdateInfoData = await ClientService.GetCurrentUpdatesInfo();
        OnUpdatesUpdateInfo(UpdateInfoData);
        _ = await ClientService.GetCurrentFileOverData();
        
        ClientService.UpdatesUpdateInfo += OnUpdatesUpdateInfo;
        Profile = await ProfileService.Get();

        //await Refresh();
        
        loaded = true;
        StateHasChanged();
    }

    /// <summary>
    /// Event raised when the update info has bene updated
    /// </summary>
    /// <param name="info">the current info</param>
    private void OnUpdatesUpdateInfo(UpdateInfo info)
    {
        UpdateInfoData = info;
        lblUpdates = Translater.Instant("Pages.Dashboard.Widgets.System.Updates",
                 new { count = UpdateInfoData?.NumberOfUpdates ?? 0 });
        Tabs?.TriggerStateHasChanged();
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

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        ClientService.UpdatesUpdateInfo -= OnUpdatesUpdateInfo;
    }
}