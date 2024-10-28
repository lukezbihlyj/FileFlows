using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class LargestSavingsWidget : ComponentBase
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    [CascadingParameter] Editor Editor { get; set; }
    
    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] private ProfileService ProfileService { get; set; }

    /// <summary>
    /// The users profile
    /// </summary>
    private Profile Profile;
    /// <summary>
    /// The key used to store the selected mode in local storage
    /// </summary>
    private const string LocalStorageKey = "LargestSavingsWidget";

    private List<LibraryFile> MonthData = [], AllData = [];
    
    /// <summary>
    /// Gets the current data
    /// </summary>
    private List<LibraryFile> Data => Mode == 0 ? MonthData : AllData;
    private string lblTitle, lblAll, lblMonth;
    
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
            _ = LocalStorage.SetItemAsync(LocalStorageKey, value);
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblAll = Translater.Instant("Labels.All");
        lblMonth = Translater.Instant("Labels.MonthShort");
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.LargestSavings.Title");
        Profile = await ProfileService.Get();
        Mode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 1);
        await Refresh();
    }

    /// <summary>
    /// Refreshes the data
    /// </summary>
    private async Task Refresh()
    {
        AllData = await GetData(0);
        MonthData = await GetData(31);
        StateHasChanged();
    }
    
    private async Task<List<LibraryFile>> GetData(int days)
    {
        var result = await HttpHelper.Post<List<LibraryFile>>("/api/library-file/search", new
        {
            FinishedProcessingFrom = days == 0 ? null : DateTime.UtcNow.AddDays(-days) as DateTime?,
            Status = FileStatus.Processed,
            Limit = 20,
            OrderBy = LibraryFileSearchOrderBy.Savings
        });
        return result.Success ? result.Data ?? [] : [];
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> value based on its duration.
    /// </summary>
    /// <param name="processingTime">The <see cref="TimeSpan"/> representing the processing time.</param>
    /// <returns>A formatted string representation of the time.</returns>
    private string FormatProcessingTime(TimeSpan processingTime)
    {
        if (processingTime.TotalDays >= 1)
            return processingTime.ToString(@"d\.hh\:mm\:ss");
        if (processingTime.TotalHours >= 1)
            return processingTime.ToString(@"h\:mm\:ss");
        
        return processingTime.ToString(@"m\:ss");
    }

    /// <summary>
    /// Opens the file for viewing
    /// </summary>
    /// <param name="file">the file</param>
    private void OpenFile(LibraryFile file)
    {
        _ = Helpers.LibraryFileEditor.Open(Blocker, Editor, file.Uid, Profile);
    }
}