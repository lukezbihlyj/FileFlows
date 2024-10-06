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

    private string lblNoSavings, lblWeek, lblMonth;
    
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
        lblNoSavings = Translater.Instant("Pages.Dashboard.Widgets.LibrarySavings.NoSavings");
        lblWeek = Translater.Instant("Labels.WeekShort");
        lblMonth = Translater.Instant("Labels.MonthShort");
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
        Logger.Instance.ILog("LibrarySavings: Original: " + original);
        long final = dataset.Sum(x => x.Value.FinalStorage);
        Logger.Instance.ILog("LibrarySavings: Final: " + final);
        TotalPercent = Math.Round(final * 100f / original, 1);
        Logger.Instance.ILog("LibrarySavings: TotalPercent: " + TotalPercent);
        TotalSavings = final > original ? lblNoSavings : FileSizeFormatter.FormatSize(original - final, 1);
        Logger.Instance.ILog("LibrarySavings: TotalSavings: " + TotalSavings);
    }

    /// <summary>
    /// Disposes of the object
    /// </summary>
    public void Dispose()
    {
        ClientService.FileOverviewUpdated -= OnFileOverviewUpdated;
    }
}