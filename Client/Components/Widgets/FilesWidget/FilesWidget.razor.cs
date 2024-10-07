using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class FilesWidget : ComponentBase, IDisposable
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

    private int _FileMode = 0;

    /// <summary>
    /// Gets or sets the file mode
    /// </summary>
    private int FileMode
    {
        get => _FileMode;
        set
        {
            _FileMode = value;
            _ = LocalStorage.SetItemAsync(LocalStorageKey, value);
        }
    }
    

    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    /// <summary>
    /// The key used to store the selected mode in local storage
    /// </summary>
    private const string LocalStorageKey = "FilesWidget";

    private string lblUpcoming, lblFinished, lblFailed;


    private List<DashboardFile> UpcomingFiles, RecentlyFinished, FailedFiles;
    private int TotalUpcoming, TotalFinished, TotalFailed;
    
    protected override async Task OnInitializedAsync()
    {
        lblUpcoming = Translater.Instant("Pages.Dashboard.Widgets.Files.Upcoming", new { count = 0});
        lblFinished = Translater.Instant("Pages.Dashboard.Widgets.Files.Finished", new { count = 0});
        lblFailed = Translater.Instant("Pages.Dashboard.Widgets.Files.Failed", new { count = 0 });
        FileMode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 2);
        await RefreshData();
        ClientService.FileStatusUpdated += OnFileStatusUpdated;
    }

    /// <summary>
    /// File status updated
    /// </summary>
    /// <param name="obj">the files updated</param>
    private void OnFileStatusUpdated(List<LibraryStatus> obj)
    {
        bool watchedStatuses = obj.Any(x => x.Status is FileStatus.Unprocessed or FileStatus.Processed or FileStatus.ProcessingFailed);
        if (watchedStatuses)
            _ = RefreshData();
    }

    /// <summary>
    /// Refreshes the data
    /// </summary>
    private async Task RefreshData()
    {
        UpcomingFiles = await LoadData<List<DashboardFile>>("/api/library-file/upcoming");
        RecentlyFinished = await LoadData<List<DashboardFile>>("/api/library-file/recently-finished?failedFiles=false");
        FailedFiles = await LoadData<List<DashboardFile>>("/api/library-file/recently-finished?failedFiles=true");
        TotalUpcoming = UpcomingFiles.Count;
        TotalFailed = FailedFiles.Count;
        TotalFinished = RecentlyFinished.Count;
        lblUpcoming = Translater.Instant("Pages.Dashboard.Widgets.Files.Upcoming", new { count = TotalUpcoming});
        lblFinished = Translater.Instant("Pages.Dashboard.Widgets.Files.Finished", new { count = TotalFinished});
        lblFailed = Translater.Instant("Pages.Dashboard.Widgets.Files.Failed", new { count = TotalFailed });
        StateHasChanged();
    }

    private async Task<T> LoadData<T>(string url)
    {
        var result = await HttpHelper.Get<T>(url);
        if(result.Success == false || result.Data == null)
            return default;
        return result.Data;
    }
    
    public record DashboardFile(Guid Uid, string Name, string DisplayName,
        string RelativePath,
        DateTime ProcessingEnded,
        string LibraryName,
        string? When,
        long OriginalSize,
        long FinalSize,
        string Message,
        FileStatus Status
    );
    
    /// <summary>
    /// Opens the file for viewing
    /// </summary>
    /// <param name="file">the file</param>
    private void OpenFile(DashboardFile file)
        => _ = Helpers.LibraryFileEditor.Open(Blocker, Editor, file.Uid);
    
    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        ClientService.FileStatusUpdated -= OnFileStatusUpdated;
    }
}