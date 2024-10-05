using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class NewDashboard : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    

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