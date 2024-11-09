using System.Timers;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;

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
    private System.Timers.Timer ScanTimer = new();
    
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

    private readonly FairSemaphore Semaphore = new(1, 1);
    private readonly FairSemaphore ScanSemaphore = new(1, 1);
    private LibraryFileService LibraryFileService { get; init; }
    
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
        this._StringHelper = new(logger);
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

    private void WatcherOnFileChanged(object? sender, FileSystemEventArgs e)
    {
        _ = CheckFile(e.FullPath);
    }

    /// <summary>
    /// The event called when the scan interval is triggered
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the elapsed event args</param>
    private void ScanTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        SetupWatcher();
        _ = PerformScan();
    }

    /// <summary>
    /// Performs the full scan of the library
    /// </summary>
    private async Task PerformScan()
    {
        await ScanSemaphore.WaitAsync(cancellationTokenSource.Token);
        try
        {
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
        }
        finally
        {
            ScanSemaphore.Release();
        }
    }

    /// <summary>
    /// Checks if a file is new or has been updated and if so adds it/updates it in the library
    /// </summary>
    /// <param name="filePath">the file to check</param>
    private async Task CheckFile(string filePath)
    {
        await Semaphore.WaitAsync(cancellationTokenSource.Token);
        try
        {
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
                if (Library.SkipFileAccessTests == false &&
                    await CanAccess(fileInfo, Library.FileSizeDetectionInterval) == false)
                {
                    // can't access the file
                    return;
                }

                var lfExisting = await LibraryFileService.GetFileIfKnown(filePath, Library.Uid);
                //var existing = KnownFiles.FirstOrDefault(x => x.Name == filePath);
                if (lfExisting != null)
                {
                    if (Library.DownloadsDirectory)
                    {
                        // check if was processed
                        if (lfExisting.Status != FileStatus.Processed)
                            return;
                        // file was processed, so this is treated as a new file/new download and will be reproccessed 

                        Logger.ILog("Processed file found in download library, reprocessing: " + filePath);
                    }
                    else if (FileUnchanged(fileInfo, lfExisting)) // existing file, check if its changed
                        return;

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
            else 
            {
                var lfExisting = await LibraryFileService.GetFileIfKnown(filePath, Library.Uid);
                if (lfExisting != null)
                    return; // already known
            }
                
            // it's a new file/folder
            var lf = NewLibraryFile(filePath);
            await LibraryFileService.Insert(lf);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private LibraryFile NewLibraryFile(string path, bool isDirectory = false)
    {
        var lf = new LibraryFile
        {
            Uid = Guid.NewGuid(),
            Name = path,
            RelativePath = GetRelativePath(path),
            Status = FileStatus.Unprocessed,
            IsDirectory = isDirectory,
            HoldUntil = Library.HoldMinutes > 0 ? DateTime.UtcNow.AddMinutes(Library.HoldMinutes) : DateTime.MinValue,
            Library = new ObjectReference
            {
                Name = Library.Name,
                Uid = Library.Uid,
                Type = Library.GetType()?.FullName ?? string.Empty
            },
            Order = -1
        };
        if (isDirectory)
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
    /// Configures the scanner
    /// </summary>
    private void ConfigureScanner()
    {
        if (ScanTimer == null)
            return; // has been disposed
        if (Math.Abs(ScanTimer.Interval - Library.FullScanIntervalMinutes * 60 * 1000) < 500)
            return;
        
        ScanTimer.Elapsed += ScanTimerOnElapsed;
        ScanTimer.AutoReset = true;
        ScanTimer.Interval = Library.FullScanIntervalMinutes * 60 * 1000;
        ScanTimer.Start();
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
        ScanTimer.Stop();
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
        ConfigureScanner();
    }
}