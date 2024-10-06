using FileFlows.Client.Helpers;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Library Savings Summary Widget
/// </summary>
public partial class LibrarySavingsSummaryWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }

    private double TotalPercent = 0;
    private string TotalSavings = string.Empty;
    
    private List<StorageSavedData> Data = new();
    
    private int _Mode = 1;
    private FileOverviewData? CurrentData;
    /// <summary>
    /// Gets or sets the selected mode
    /// </summary>
    private int Mode
    {
        get => _Mode;
        set
        {
            _Mode = value;
            SetValues();

            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ClientService.FileOverviewUpdated += OnFileOverviewUpdated;
        if (ClientService.CurrentFileOverData != null)
        {
            CurrentData = ClientService.CurrentFileOverData;
            SetValues();
        }

        _ = Refresh();
    }

    private async Task Refresh()
    {
        var result = await HttpHelper.Get<List<StorageSavedData>>("/api/statistics/storage-saved-raw");
        if (result.Success == false)
            return;
        Data = result.Data;
        StateHasChanged();
    }

    /// <summary>
    /// Called when the file overview is updated
    /// </summary>
    /// <param name="data">the updated data</param>
    private void OnFileOverviewUpdated(FileOverviewData data)
    {
        CurrentData = data;
        SetValues();
        StateHasChanged();
    }

    /// <summary>
    /// Sets the value based on the data
    /// </summary>
    private void SetValues()
    {
        var dataset = Mode switch
        {
            1 => CurrentData.Last7Days,
            2 => CurrentData.Last31Days,
            _ => CurrentData.Last24Hours
        };

        if (dataset.Count == 0)
            return;
        
        long original = dataset.Sum(x => x.Value.OriginalStorage);
        long final = dataset.Sum(x => x.Value.FinalStorage);
        TotalPercent = Math.Round((1 - (double)final / original) * 100, 1);
        TotalSavings = FileSizeFormatter.FormatSize(original - final, 1);
    }

    /// <summary>
    /// Disposes of the object
    /// </summary>
    public void Dispose()
    {
        ClientService.FileOverviewUpdated -= OnFileOverviewUpdated;
    }
}