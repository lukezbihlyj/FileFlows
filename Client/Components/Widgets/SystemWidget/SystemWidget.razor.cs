using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Flow Runners widget
/// </summary>
public partial class SystemWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    
    /// <summary>
    /// Gets or sets the mode
    /// </summary>
    private int Mode { get; set; }

    /// <summary>
    /// The option buttons
    /// </summary>
    private OptionButtons OptionButtons;

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblTitle, lblRunners, lblNodes, lblSavings;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.System.Title");
        lblRunners = Translater.Instant("Pages.Dashboard.Widgets.System.Runners");
        lblNodes = Translater.Instant("Pages.Nodes.Title");
        lblSavings = Translater.Instant("Pages.Dashboard.Tabs.Savings");
        var info = await ClientService.GetCurrentExecutorInfoMinifed();
        OnExecutorsUpdated(info ?? []);
        ClientService.ExecutorsUpdated += OnExecutorsUpdated;
    }

    /// <summary>
    /// Raised when the executors are updated
    /// </summary>
    /// <param name="info">the executors</param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnExecutorsUpdated(List<FlowExecutorInfoMinified> info)
    {
        lblRunners = Translater.Instant("Pages.Dashboard.Widgets.System.Runners", new {count = info.Count});
        OptionButtons?.TriggerStateHasChanged();
        StateHasChanged();
    }


    /// <summary>
    /// Select nodes if no runners are running on load
    /// </summary>
    private void SelectNodes()
    {
        Mode = 2;
        StateHasChanged();
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        ClientService.ExecutorsUpdated -= OnExecutorsUpdated;
    }
}