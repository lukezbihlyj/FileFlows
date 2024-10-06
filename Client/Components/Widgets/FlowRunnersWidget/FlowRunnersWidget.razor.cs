using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class FlowRunnersWidget : ComponentBase, IDisposable
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
    
    private List<FlowExecutorInfoMinified> Runners = new ();
    /// <summary>
    /// Gets or sets the file mode
    /// </summary>
    private int FileMode { get; set; } = 2;
    
    private Guid SelectedExecutor = Guid.Empty;


    private List<DashboardFile> UpcomingFiles, RecentlyFinished, FailedFiles;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        #if(DEBUG)
        Runners = GenerateRandomExecutors(10);
        #else
        ClientService.ExecutorsUpdated += ExecutorsUpdated;
        #endif
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

#if(DEBUG)
    
    public static List<FlowExecutorInfoMinified> GenerateRandomExecutors(int count)
    {
        var random = new Random();
        var executors = new List<FlowExecutorInfoMinified>();

        for (int i = 0; i < count; i++)
        {
            // Generate random file name and GUID
            string fileName = $"file-{i + 1}.mkv";
            Guid uid = Guid.NewGuid();
            int totalParts = random.Next(5, 20); // Random total parts between 5 and 20
            int currentPart = random.Next(1, totalParts + 1); // Random current part within total parts

            executors.Add(new FlowExecutorInfoMinified
            {
                Uid = uid,
                DisplayName = fileName,
                NodeName = "FileFlowsServer",
                LibraryFileUid = uid,
                LibraryFileName = $"/home/user/videos/{fileName}",
                RelativeFile = fileName,
                LibraryName = "Video Library",
                TotalParts = totalParts,
                CurrentPart = currentPart,
                CurrentPartName = $"Part {currentPart} Processing",
                CurrentPartPercent = random.Next(0, 100), // Random percentage 0-100
                LastUpdate = DateTime.UtcNow.AddSeconds(-random.Next(0, 60 * 60)), // Random last update within the last hour
                StartedAt = DateTime.UtcNow.AddMinutes(-random.Next(1, 120)), // Started between 1 and 120 minutes ago
                //ProcessingTime = TimeSpan.FromSeconds(random.Next(30, 3600)), // Random processing time 30 seconds to 1 hour
                FramesPerSecond = random.Next(20, 300), // Random FPS between 20.0 and 60.0
                //Additional = new List<string>() // Optionally populate with random additional data
            });
        }

        return executors;
    }
    #endif

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
        ClientService.ExecutorsUpdated -= ExecutorsUpdated;
        ClientService.FileStatusUpdated -= OnFileStatusUpdated;
    }
    
    /// <summary>
    /// Called when the executors are updated
    /// </summary>
    /// <param name="obj">the updated executors</param>
    private void ExecutorsUpdated(List<FlowExecutorInfoMinified> obj)
    {
        Runners = obj ?? new();
        StateHasChanged();
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

}