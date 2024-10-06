using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class NewDashboard : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }

    private string lblDashboard, lblSavings;

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

        loaded = true;
        StateHasChanged();
    }


    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        RegisterListeners(false);
    }
    
    /// <summary>
    /// Registers the listeners
    /// </summary>
    /// <param name="unregister">if the listeners should be unregistered</param>
    private void RegisterListeners(bool unregister = false)
    {
        if (unregister)
        {
            ClientService.FileStatusUpdated -= OnFileStatusUpdated;
            ClientService.SystemPausedUpdated -= SystemPausedUpdated;
            ClientService.ExecutorsUpdated -= ExecutorsUpdated;
        }
        else
        {
            ClientService.FileStatusUpdated += OnFileStatusUpdated;
            ClientService.SystemPausedUpdated += SystemPausedUpdated;
            ClientService.ExecutorsUpdated += ExecutorsUpdated;
        }
    }


    private void OnFileStatusUpdated(List<LibraryStatus> obj)
    {
        //throw new NotImplementedException();
    }

    private void SystemPausedUpdated(bool obj)
    {
        //throw new NotImplementedException();
        StateHasChanged();
    }
    /// <summary>
    /// Called when the executors are updated
    /// </summary>
    /// <param name="obj">the updated executors</param>
    private void ExecutorsUpdated(List<FlowExecutorInfoMinified> obj)
    {
        StateHasChanged();
    }
}