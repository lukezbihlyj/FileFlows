using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class NewDashboard : ComponentBase
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    
    /// <summary>
    /// The update data
    /// </summary>
    public UpdateInfo? UpdateInfoData;


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

        await Refresh();

        lblUpdates = Translater.Instant("Pages.Dashboard.Widgets.System.Updates",
            new { count = UpdateInfoData.NumberOfUpdates });
        
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
    }
}