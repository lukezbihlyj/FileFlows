using FileFlows.Shared.Formatters;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Files Widget
/// </summary>
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
    private const int MODE_UPCOMING = 1;
    private const int MODE_FINISHED = 0;
    private const int MODE_FAILED = 2;

    /// <summary>
    /// Gets or sets the file mode
    /// </summary>
    private int FileMode
    {
        get => _FileMode;
        set
        {
            _FileMode = value;
            if(initialized)
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

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblTitle, lblUpcoming, lblFinished, lblFailed, lblNoUpcomingFiles, lblNoFailedFiles, lblNoRecentlyFinishedFiles;

    private OptionButtons OptionButtons;
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; private set; }


    private List<DashboardFile> UpcomingFiles, RecentlyFinished, FailedFiles;
    private int TotalUpcoming, TotalFinished, TotalFailed;
    private bool initialized = false;
    
    protected override async Task OnInitializedAsync()
    {
        Profile = await ProfileService.Get();
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.Files.Title");
        lblUpcoming = Translater.Instant("Pages.Dashboard.Widgets.Files.Upcoming", new { count = 0});
        lblFinished = Translater.Instant("Pages.Dashboard.Widgets.Files.Finished", new { count = 0});
        lblFailed = Translater.Instant("Pages.Dashboard.Widgets.Files.Failed", new { count = 0 });
        lblNoUpcomingFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoUpcomingFiles");
        lblNoRecentlyFinishedFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoRecentlyFinishedFiles");
        lblNoFailedFiles = Translater.Instant("Pages.Dashboard.Widgets.Files.NoFailedFiles");
        _FileMode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 2);
        await RefreshData();
        ClientService.FileStatusUpdated += OnFileStatusUpdated;
    }

    /// <summary>
    /// File status updated
    /// </summary>
    /// <param name="obj">the files updated</param>
    private void OnFileStatusUpdated(List<LibraryStatus> obj)
    {
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

        if (initialized == false)
        {
            switch (FileMode)
            {
                case MODE_UPCOMING when TotalUpcoming == 0:
                {
                    if (TotalFailed > 0 && TotalFinished > 0)
                    {
                        var failed = FailedFiles.Max(x => x.ProcessingEnded);
                        var success = RecentlyFinished.Max(x => x.ProcessingEnded);
                        FileMode = failed > success ? MODE_FAILED : MODE_FINISHED;
                    }
                    else if (TotalFinished > 0)
                        FileMode = MODE_FINISHED;
                    else if (TotalFailed > 0)
                        FileMode = MODE_FAILED;

                    break;
                }
                case MODE_FAILED when TotalFailed == 0:
                {
                    if (TotalUpcoming > 0)
                        FileMode = MODE_UPCOMING;   
                    else if(TotalFinished > 0)
                        FileMode = MODE_FINISHED;
                    break;
                }
                case MODE_FINISHED when TotalFinished == 0:
                {
                    if (TotalUpcoming > 0)
                        FileMode = MODE_UPCOMING;   
                    else if(TotalFailed > 0)
                        FileMode = MODE_FAILED;
                    break;
                }
            }

            initialized = true;
        }
        StateHasChanged();
        OptionButtons?.TriggerStateHasChanged();
    }

    /// <summary>
    /// Loads the data from the server
    /// </summary>
    /// <param name="url">the URL to call</param>
    /// <typeparam name="T">the type of data</typeparam>
    /// <returns>the returned ata</returns>
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
        => _ = Helpers.LibraryFileEditor.Open(Blocker, Editor, file.Uid, Profile);
    
    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        ClientService.FileStatusUpdated -= OnFileStatusUpdated;
    }
}