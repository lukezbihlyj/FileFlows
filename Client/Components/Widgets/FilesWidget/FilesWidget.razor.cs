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
    /// <summary>
    /// Gets or sets the file mode
    /// </summary>
    private int FileMode { get; set; } = 2;


    private List<DashboardFile> UpcomingFiles, RecentlyFinished, FailedFiles;
    
    protected override async Task OnInitializedAsync()
    {
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
    /// Opens the file for vieweing
    /// </summary>
    /// <param name="file">the file</param>
    private void OpenFile(DashboardFile file)
    {
        _ = Helpers.LibraryFileEditor.Open(Blocker, Editor, file.Uid);
    }
    
    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        ClientService.FileStatusUpdated -= OnFileStatusUpdated;
    }
}