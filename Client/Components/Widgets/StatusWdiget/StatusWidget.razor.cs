using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Status Update widget
/// </summary>
public partial class StatusWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    /// <summary>
    /// Gets or sets the paused service
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    
    /// <summary>
    /// Translations
    /// </summary>
    private string lblTitle, lblPause, lblResume;

    private List<FlowExecutorInfoMinified> _executors = [];
    private UpdateInfo? _updateInfo = null;

    private enum SystemStatus
    {
        Idle,
        Paused,
        Processing,
        UpdateAvailable
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblTitle = Translater.Instant("Labels.Status");
        lblPause = Translater.Instant("Labels.Pause");
        lblResume = Translater.Instant("Labels.Resume");
        _updateInfo = await ClientService.GetCurrentUpdatesInfo();
        PausedService.OnPausedLabelChanged += OnPausedLabelChanged;
        ClientService.ExecutorsUpdated += OnExecutorsUpdated;
        ClientService.UpdatesUpdateInfo += OnUpdatesUpdateInfo;
        OnPausedLabelChanged(PausedService.PausedLabel);
    }

    /// <summary>
    /// Called when the update info changes
    /// </summary>
    /// <param name="info">the new info</param>
    private void OnUpdatesUpdateInfo(UpdateInfo info)
    {
        _updateInfo = info;
        StateHasChanged();
    }

    /// <summary>
    /// Gets the status
    /// </summary>
    /// <returns>the status</returns>
    private SystemStatus GetStatus()
    {
        if (PausedService.IsPaused)
            return SystemStatus.Paused;
        if (_executors.Count > 0)
            return SystemStatus.Processing;
        if(_updateInfo is { HasUpdates: true })
            return SystemStatus.UpdateAvailable;
        
        return SystemStatus.Idle;
    }

    /// <summary>
    /// Called when the paused label is changed
    /// </summary>
    /// <param name="label">the new label</param>
    private void OnPausedLabelChanged(string label)
        => StateHasChanged();

    /// <summary>
    /// Called when the executors are updated
    /// </summary>
    /// <param name="info">the updated executgors</param>
    private void OnExecutorsUpdated(List<FlowExecutorInfoMinified> info)
    {
        _executors = info ?? [];
        StateHasChanged();
    }

    /// <summary>
    /// Gets the label to toggle the paused state
    /// </summary>
    private string lblTogglePause => PausedService.IsPaused ? lblResume : lblPause;

    /// <summary>
    /// Toggles the paused state
    /// </summary>
    private void TogglePause()
        => _ = PausedService.Toggle();
    
    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        ClientService.ExecutorsUpdated -= OnExecutorsUpdated;
        PausedService.OnPausedLabelChanged -= OnPausedLabelChanged;
    }
}