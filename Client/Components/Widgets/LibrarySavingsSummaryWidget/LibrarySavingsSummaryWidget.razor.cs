using System.Runtime.InteropServices.JavaScript;
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
    
    private List<StorageSavedData> TotalData = new();
    private List<StorageSavedData> MonthData = new();

    private string lblNoSavings, lblAll, lblMonth;

    public List<StorageSavedData> Data => Mode == 0 ? MonthData : TotalData;
    /// <summary>
    /// Gets the mode
    /// </summary>
    private int _Mode = 0;
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
        lblAll = Translater.Instant("Labels.All");
        lblMonth = Translater.Instant("Labels.MonthShort");
        ClientService.FileOverviewUpdated += OnFileOverviewUpdated;
        _ = Refresh();
    }

    /// <summary>
    /// Refresh the data
    /// </summary>
    private async Task Refresh()
    {
        var result = await HttpHelper.Get<List<StorageSavedData>>("/api/statistics/storage-saved-raw?days=0");
        if (result.Success)
            TotalData = result.Data;
        var result2 = await HttpHelper.Get<List<StorageSavedData>>("/api/statistics/storage-saved-raw?days=31");
        if (result2.Success)
            MonthData = result2.Data;
        SetValues();
        StateHasChanged();
    }

    /// <summary>
    /// Called when the file overview is updated
    /// </summary>
    /// <param name="data">the updated data</param>
    private void OnFileOverviewUpdated(FileOverviewData data)
        => _ = Refresh();

    /// <summary>
    /// Sets the value based on the data
    /// </summary>
    private void SetValues()
    {
        var data = Data;
        if (data == null || data.Count == 0)
        {
            TotalSavings = lblNoSavings;
            TotalPercent = 0;
            return;
        }
        
        long original = data.Sum(x => x.OriginalSize);
        long final = data.Sum(x => x.FinalSize);
        long saved = original - final;
        TotalPercent = Math.Round(saved * 100f / original, 1);
        TotalSavings = final > original ? lblNoSavings : FileSizeFormatter.FormatSize(saved, 1);
    }

    /// <summary>
    /// Disposes of the object
    /// </summary>
    public void Dispose()
    {
        ClientService.FileOverviewUpdated -= OnFileOverviewUpdated;
    }
}