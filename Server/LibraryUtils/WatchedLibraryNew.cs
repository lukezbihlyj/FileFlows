using System.Collections.Concurrent;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Humanizer;
using FileHelper = FileFlows.ServerShared.Helpers.FileHelper;
using Timer = System.Threading.Timer;

namespace FileFlows.Server.LibraryUtils;

/// <summary>
/// A new watched library that scans and queues up files for processing
/// </summary>
public partial class WatchedLibraryNew : IDisposable
{
    /// <summary>
    /// The file system watcher that watches for file system events
    /// </summary>
    private LibraryFileWatcher? Watcher;

    /// <summary>
    /// The string helper instance
    /// </summary>
    private StringHelper _StringHelper;

    /// <summary>
    /// The timer that runs on an interval
    /// </summary>
    private Timer? ScanTimer;
    
    /// <summary>
    /// Cancellation token
    /// </summary>
    private CancellationTokenSource? cancellationTokenSource;
    
    /// <summary>
    /// Gets or sets the library being watched
    /// </summary>
    public Library Library { get;private set; } 
    
    /// <summary>
    /// The lgoger to use
    /// </summary>
    private Plugin.ILogger Logger { get; init; }

    private readonly SemaphoreSlim FileSemaphore = new(1, 1);
    private readonly SemaphoreSlim ScanSemaphore = new(1, 1);
    private LibraryFileService LibraryFileService { get; init; }
    private Settings Settings;

    /// <summary>
    /// Tthe flow runner service
    /// </summary>
    private readonly FlowRunnerService RunnerService;
    
    /// <summary>
    /// Constructs a instance of a Watched Library
    /// </summary>
    /// <param name="logger">the logger to use</param>
    /// <param name="library">The library to watch</param>
    public WatchedLibraryNew(Plugin.ILogger logger, Library library)
    {
        this.Logger = logger;
        this.Library = library;
        this.LibraryFileService = ServiceLoader.Load<LibraryFileService>();
        this.RunnerService = ServiceLoader.Load<FlowRunnerService>();
        this.Settings = ServiceLoader.Load<ISettingsService>().Get().Result;
        this._StringHelper = new(logger)
        {
            Silent = true
        };
    }

    private void SetupWatcher()
    {
        if (Watcher != null)
            return; // already configured
        
        if (Directory.Exists(Library.Path))
        {
            Watcher = new(Library.Path, Library.Folders);
            Watcher.FileChanged += WatcherOnFileChanged;
            Watcher.FileRenamed += WatcherOnFileChanged;
            Watcher.Start();
        }
    }
    
    /// <summary>
    /// Files currently being checked
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _pendingFiles = new();

    private void WatcherOnFileChanged(object? sender, FileSystemEventArgs e)
    {
        if(Library.DisableFileSystemEvents == true)
            return;
        if(e.FullPath.EndsWith(".fftemp"))
            return; // we ignore .fftemp files
        
        if (Settings?.LogQueueMessages == true)
            Logger.DLog("WatcherOnFileChanged: " + e.FullPath);
        // If the file is already being processed, return early
        if (_pendingFiles.ContainsKey(e.FullPath) && _pendingFiles[e.FullPath])
            return;
        
        // Mark the file as pending
        _pendingFiles[e.FullPath] = true;
        
        _ = Task.Run(async () =>
        {
            // Wait until the file is stabilized
            if (!await IsFileStabilized(e.FullPath))
            {
                if (Settings?.LogQueueMessages == true)
                    Logger.DLog("WatcherOnFileChanged.NotStablized: " + e.FullPath);
                return;
            }

            var executors = (await this.RunnerService.GetExecutors()).Values;
            if (executors.Any(x => x.LibraryFileName == e.FullPath || x.WorkingFile == e.FullPath))
                return; // ignore these files
            
            int delay = 5000;
            string? folder = new FileInfo(e.FullPath).Directory?.FullName;
            if (folder != null)
            {
                bool thisFolder = executors.Any(x => x.LibraryFileName?.StartsWith(folder) == true);
                if (thisFolder)
                    delay = 30_000;
            }
            // need a delay incase a file is processing and replcae original with new extension
            // for example is called, this could cause a file to be detected when it will become the output file
            await Task.Delay(delay);
            if (Settings?.LogQueueMessages == true)
                Logger.DLog("WatcherOnFileChanged.CheckFile: " + e.FullPath);
            await CheckFile(e.FullPath);
        });
    }

    /// <summary>
    /// Checks if a file is stabilized (i.e., hasn't changed for a given period).
    /// </summary>
    /// <param name="filePath">The path to the file to check.</param>
    /// <param name="stabilizationDelay">The delay to confirm stabilization, in milliseconds.</param>
    /// <returns>True if the file is stabilized, otherwise false.</returns>
    private async Task<bool> IsFileStabilized(string filePath, int stabilizationDelay = 10_000)
    {
        try
        {
            FileInfo fileInfo = new(filePath);
            DateTime lastWriteTime = fileInfo.LastWriteTime;

            // Wait for the stabilization delay
            await Task.Delay(stabilizationDelay);

            // Re-fetch the file information to check if it changed
            fileInfo.Refresh();
            return fileInfo.LastWriteTime == lastWriteTime;
        }
        catch (IOException)
        {
            // File is likely in use or not accessible, so not stabilized
            return false;
        }
    }
    
    /// <summary>
    /// Performs the full scan of the library
    /// </summary>
    public async Task Scan()
    {
        try
        {
            await ScanSemaphore.WaitAsync(10_000, cancellationTokenSource!.Token);

            // refresh the library instance
            var libraryService = ServiceLoader.Load<LibraryService>();
            Library = await libraryService.GetByUidAsync(Library.Uid) ?? Library;
            if (Library.Enabled == false)
                return; // no need to scan

            Logger.ILog($"Library '{Library.Name}' scanning");
            // refresh the settings
            this.Settings = await ServiceLoader.Load<ISettingsService>().Get();
            DateTime start = DateTime.Now;
            // scan library 
            if (Library.Folders)
            {
                var dirs = Directory.GetDirectories(Library.Path);
                foreach (var dir in dirs)
                    await CheckFile(dir);
            }
            else
            {
                var files = Directory.GetFiles(Library.Path, "*.*",
                    Library.TopLevelOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
                foreach (var file in files)
                    await CheckFile(file);
            }

            Logger.ILog($"Library '{Library.Name}' scan complete in " + (DateTime.Now - start).Humanize());
            await libraryService.UpdateLastScanned(Library.Uid);
        }
        catch (Exception ex)
        {
            Logger.ELog($"Scan '{Library.Name}' failed: {ex.Message}");
        }
        finally
        {
            ScanSemaphore.Release();
            StartOrResetScanTimer();
        }
    }
    

    /// <summary>
    /// Checks if a file is new or has been updated and if so adds it/updates it in the library
    /// </summary>
    /// <param name="filePath">the file to check</param>
    private async Task CheckFile(string filePath)
    {
        if (filePath.EndsWith(".fftemp"))
            return; // we ignore fftemp files
        
        await FileSemaphore.WaitAsync(30_000, cancellationTokenSource.Token);
        try
        {

            if (Settings?.LogQueueMessages == true)
                Logger.DLog($"Checking [{Library.Name}]: " + filePath);
            
            if (IsMatch(filePath) == false)
                return; // doesnt match extension/filters

            if (MatchesDetection(filePath) == false)
                return; // file doesnt match library conditions
            if (Library.ExcludeHidden && FileIsHidden(filePath))
                return;
            if (Library.TopLevelOnly && PathIsTopLevel(filePath) == false)
                return;

            if (Library.Folders == false)
            {
                var fileInfo = new FileInfo(filePath);

                var lfExisting = await LibraryFileService.GetFileIfKnown(filePath, Library.Uid);
                //var existing = KnownFiles.FirstOrDefault(x => x.Name == filePath);
                if (lfExisting != null)
                {
                    if (lfExisting.Status is FileStatus.Unprocessed or FileStatus.Disabled or FileStatus.OnHold or FileStatus.OutOfSchedule)
                        return; // not processed yet, no need to do anything
                    
                    if (Library.DownloadsDirectory)
                    {
                        // check if was processed
                        if (lfExisting.Status != FileStatus.Processed)
                            return;
                        // file was processed, so this is treated as a new file/new download and will be reprocessed
                        Logger.ILog("Processed file found in download library, reprocessing: " + filePath);
                    }
                    else if (FileUnchanged(fileInfo, lfExisting)) // existing file, check if its changed
                        return;
                
                    if (Library.SkipFileAccessTests == false &&
                        await CanAccess(fileInfo, Library.FileSizeDetectionInterval) == false)
                    {
                        // can't access the file
                        return;
                    }

                    Logger.ILog($"Reprocessing Library '{Library.Name} File: {filePath}");
                    // reprocess the file
                    lfExisting.Status = FileStatus.Unprocessed;
                    if (Library.HoldMinutes < 1)
                        await LibraryFileService.SetStatus(FileStatus.Unprocessed, lfExisting.Uid);
                    else
                    {
                        // need to set hold time
                        lfExisting.HoldUntil = DateTime.UtcNow.AddMinutes(Library.HoldMinutes);
                        lfExisting.Status =
                            FileStatus.Unprocessed; // we want unprocessed here, so it's saved as 0 in the DB
                        await LibraryFileService.Update(lfExisting);
                    }

                    return;
                }
                
                if (Library.SkipFileAccessTests == false &&
                    await CanAccess(fileInfo, Library.FileSizeDetectionInterval) == false)
                {
                    // can't access the file
                    return;
                }
            }
            else 
            {
                var lfExisting = await LibraryFileService.GetFileIfKnown(filePath, Library.Uid);
                if (lfExisting != null)
                {
                    if (Library.DownloadsDirectory == false)
                        return;

                    // check if was processed
                    if (lfExisting.Status != FileStatus.Processed)
                        return;
                    // file was processed, so this is treated as a new file/new download and will be reprocessed
                    Logger.ILog("Processed folder found in download library, reprocessing: " + filePath);

                    // reprocess the file
                    lfExisting.Status = FileStatus.Unprocessed;
                    if (Library.HoldMinutes < 1)
                        await LibraryFileService.SetStatus(FileStatus.Unprocessed, lfExisting.Uid);
                    else
                    {
                        // need to set hold time
                        lfExisting.HoldUntil = DateTime.UtcNow.AddMinutes(Library.HoldMinutes);
                        lfExisting.Status =
                            FileStatus.Unprocessed; // we want unprocessed here, so it's saved as 0 in the DB
                        await LibraryFileService.Update(lfExisting);
                    }

                    return;
                }
            }
                
            // it's a new file/folder
            Logger.ILog($"New Library '{Library.Name} File: {filePath}");
            var lf = NewLibraryFile(filePath);
            await LibraryFileService.Insert(lf);
        }
        finally
        {
            FileSemaphore.Release();
        }
    }

    private LibraryFile NewLibraryFile(string path)
    {
        var lf = new LibraryFile
        {
            Uid = Guid.NewGuid(),
            Name = path,
            RelativePath = GetRelativePath(path),
            Status = FileStatus.Unprocessed,
            IsDirectory = Library.Folders,
            HoldUntil = Library.HoldMinutes > 0 ? DateTime.UtcNow.AddMinutes(Library.HoldMinutes) : DateTime.MinValue,
            Library = new ObjectReference
            {
                Name = Library.Name,
                Uid = Library.Uid,
                Type = Library.GetType()?.FullName ?? string.Empty
            },
            Order = -1
        };
        if (Library.Folders)
        {
            var dirInfo = new DirectoryInfo(path);
            lf.CreationTime = dirInfo.CreationTime;
            lf.LastWriteTime = dirInfo.LastWriteTime;
        }
        else
        {
            var fileInfo = new FileInfo(path);
            lf.OriginalSize = fileInfo.Length;
            lf.CreationTime = fileInfo.CreationTimeUtc;
            lf.LastWriteTime = fileInfo.LastWriteTimeUtc;
        }

        return lf;
    }

    /// <summary>
    /// Updates the library being watched
    /// </summary>
    /// <param name="updated">the updated library</param>
    public void UpdateLibrary(Library updated)
    {
        bool resetScanner = updated.ScanInterval != Library.ScanInterval;
        this.Library = updated;
        if(resetScanner)
            StartOrResetScanTimer();
    }

    /// <summary>
    /// Initiates or resets the scan timer.
    /// </summary>
    private void StartOrResetScanTimer()
    {
        cancellationTokenSource ??= new();
        // Dispose the current timer if it exists
        ScanTimer?.Dispose();

        // Retrieve the updated interval
        var scanInterval = TimeSpan.FromSeconds(Library.ScanInterval);

        // Set up the timer with the updated interval
        ScanTimer = new Timer(async _ =>
        {
            try
            {
                await Scan();
            }
            catch (Exception ex)
            {
                Logger.ELog($"Scan '{Library.Name}' failed: {ex.Message}");
            }
        }, null, scanInterval, Timeout.InfiniteTimeSpan);

    }

    /// <summary>
    /// Disposes of this object
    /// </summary>
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// Stops the watched library
    /// </summary>
    public void Stop()
    {
        cancellationTokenSource.Cancel();
        ScanTimer.Change(Timeout.Infinite, Timeout.Infinite);
        ScanTimer.Dispose();
        ScanTimer = null;
        if (Watcher != null)
        {
            Watcher.Dispose();
            Watcher = null;
        }
        cancellationTokenSource.Dispose();
        cancellationTokenSource = null;
    }

    /// <summary>
    /// Starts the watched lbirary
    /// </summary>
    public void Start()
    {
        cancellationTokenSource = new();
        SetupWatcher();
        _ = Scan();
    }
}