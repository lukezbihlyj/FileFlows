
namespace FileFlows.Server.LibraryUtils;

/// <summary>
/// A watcher that listens to file system events in a specified directory and raises events when files or folders are created, modified, renamed, or deleted.
/// </summary>
public class LibraryFileWatcher : IDisposable
{
    /// <summary>
    /// The <see cref="FileSystemWatcher"/> instance that monitors the specified path.
    /// </summary>
    private readonly FileSystemWatcher _watcher;

    /// <summary>
    /// Indicates whether folders should be monitored instead of files.
    /// </summary>
    private readonly bool _monitorFolders;

    /// <summary>
    /// The delay, in milliseconds, between checks to confirm file access after detecting a change.
    /// </summary>
    private readonly int _fileAccessRetryDelay = 3000;

    /// <summary>
    /// The maximum number of attempts to check if a file is accessible after detecting a change.
    /// </summary>
    private readonly int _maxFileAccessAttempts = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryFileWatcher"/> class.
    /// </summary>
    /// <param name="path">The path to monitor for changes.</param>
    /// <param name="monitorFolders">If true, monitors folders; otherwise, monitors files.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null or empty.</exception>
    public LibraryFileWatcher(string path, bool monitorFolders = false)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        _monitorFolders = monitorFolders;
        _watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.Size
        };

        _watcher.Created += OnChanged;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
    }

    /// <summary>
    /// Starts monitoring the specified path for changes.
    /// </summary>
    public void Start() => _watcher.EnableRaisingEvents = true;

    /// <summary>
    /// Stops monitoring the specified path for changes.
    /// </summary>
    public void Stop() => _watcher.EnableRaisingEvents = false;

    /// <summary>
    /// Occurs when a file or folder is added or modified.
    /// </summary>
    public event EventHandler<FileSystemEventArgs> FileChanged;

    /// <summary>
    /// Occurs when a file or folder is renamed.
    /// </summary>
    public event EventHandler<RenamedEventArgs> FileRenamed;

    /// <summary>
    /// Handles the <see cref="FileSystemWatcher.Created"/> and <see cref="FileSystemWatcher.Changed"/> events.
    /// Raises the <see cref="FileChanged"/> event if the change meets notification criteria.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private async void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldNotify(e.FullPath) && await WaitForFileAccess(e.FullPath))
            FileChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Handles the <see cref="FileSystemWatcher.Renamed"/> event.
    /// Raises the <see cref="FileRenamed"/> event if the rename meets notification criteria.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldNotify(e.FullPath))
            FileRenamed?.Invoke(this, e);
    }

    /// <summary>
    /// Determines if a notification should be raised based on the file or folder path and the monitoring mode.
    /// </summary>
    /// <param name="fullPath">The full path of the file or folder that triggered the event.</param>
    /// <returns>
    /// <c>true</c> if the event should be notified to subscribers; otherwise, <c>false</c>.
    /// </returns>
    private bool ShouldNotify(string fullPath)
    {
        if (!_monitorFolders) return File.Exists(fullPath);
        string relativePath = Path.GetRelativePath(_watcher.Path, fullPath);
        return relativePath == "." || !relativePath.Contains(Path.DirectorySeparatorChar);

    }

    /// <summary>
    /// Waits until the specified file is accessible, indicating that it is no longer being written to.
    /// </summary>
    /// <param name="filePath">The full path of the file to check.</param>
    /// <returns>A task that represents the asynchronous wait operation. The result is <c>true</c> if the file is accessible; otherwise, <c>false</c>.</returns>
    private async Task<bool> WaitForFileAccess(string filePath)
    {
        for (int attempt = 0; attempt < _maxFileAccessAttempts; attempt++)
        {
            try
            {
                await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                // File is still in use, wait and retry
                await Task.Delay(_fileAccessRetryDelay);
            }
            catch (Exception)
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Disposes of the watcher
    /// </summary>
    public void Dispose()
    {
        _watcher.Created -= OnChanged;
        _watcher.Changed -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Dispose();
    }
}
