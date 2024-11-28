using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.LibraryUtils;

public partial class WatchedLibraryNew
{
    
    /// <summary>
    /// Checks if a path exists
    /// </summary>
    /// <param name="fullpath">the full path</param>
    /// <returns>true if exists, otherwise false</returns>
    private bool CheckExists(string fullpath)
    {
        try
        {
            if (Library.Folders)
                return Directory.Exists(fullpath);
            return File.Exists(fullpath);
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Checks if a file is hidden
    /// </summary>
    /// <param name="fullpath">the full path to the file</param>
    /// <returns>true if it is hidden</returns>
    private bool FileIsHidden(string fullpath)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(fullpath);
            if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return true;
        }
        catch (Exception)
        {
            return false;
        }

        // recursively search the directories to see if its hidden
        var dir = new FileInfo(fullpath).Directory;
        int count = 0;
        while(dir.Parent != null)
        {
            if (dir.Attributes.HasFlag(FileAttributes.Hidden))
                return true;
            dir = dir.Parent;
            if (++count > 20)
                break; // infinite recursion safety check
        }
        return false;
    }
    
    /// <summary>
    /// Checks if a file can be accessed
    /// </summary>
    /// <param name="file">the file to check</param>
    /// <param name="fileSizeDetectionInterval">the interval to check if the file size has changed in seconds</param>
    /// <returns>true if the file can be accessed</returns>
    private async Task<bool> CanAccess(FileInfo file, int fileSizeDetectionInterval)
    {
        DateTime now = DateTime.UtcNow;
        bool canRead = false, canWrite = false, checkedAccess = false;
        try
        {
            if (file.LastWriteTimeUtc > DateTime.UtcNow.AddSeconds(-10))
            {
                // check if the file size changes
                long fs = file.Length;
                if (fileSizeDetectionInterval > 0)
                    await Task.Delay(Math.Min(300, fileSizeDetectionInterval) * 1000);

                if (fs != file.Length)
                {
                    Logger.ILog("File size has changed, skipping for now: " + file.FullName);
                    return false; // file size has changed, could still be being written too
                }
            }

            checkedAccess = true;

            await using (var fs = FileOpenHelper.OpenForCheckingReadWriteAccess(file.FullName))
            {
                if(fs.CanRead == false)
                {
                    Logger.WLog("Cannot read file: " + file.FullName);
                    return false;
                }
                canRead = true;
                if (fs.CanWrite == false)
                {
                    Logger.WLog("Cannot write file: " + file.FullName);
                    return false;
                }

                canWrite = true;
            }

            return true;
        }
        catch (Exception)
        {
            if (checkedAccess)
            {
                if (canRead == false)
                    Logger.WLog("Cannot read file: " + file.FullName);
                if (canWrite == false)
                    Logger.WLog("Cannot write file: " + file.FullName);
            }

            return false;
        }
    }

    private string GetRelativePath(string fullpath)
    {
        int skip = Library.Path.Length;
        if (Library.Path.EndsWith('/') == false && Library.Path.EndsWith('\\') == false)
            ++skip;

        return fullpath[skip..];
    }

    /// <summary>
    /// Tests if a file in a library matches the detection settings for hte library
    /// </summary>
    /// <param name="fullpath">the full path to the file</param>
    /// <returns>true if matches detection, otherwise false</returns>
    private bool MatchesDetection(string fullpath)
    {
        FileSystemInfo info = this.Library.Folders ? new DirectoryInfo(fullpath) : new FileInfo(fullpath);
        long size = this.Library.Folders ? Helpers.FileHelper.GetDirectorySize(fullpath) : ((FileInfo)info).Length;

        return LibraryMatches.MatchesDetection(Library, info, size);
    }

    
    /// <summary>
    /// Tests a path to see if its allowed based on the filters and extensions
    /// </summary>
    /// <param name="input">the input path</param>
    /// <returns>true if filters/extensions allow this path</returns>
    private bool IsMatch(string input)
    {
        if (Library.ExclusionFilters?.Any() == true)
        {
            foreach (var filter in Library.ExclusionFilters)
            {
                if (string.IsNullOrWhiteSpace(filter))
                    continue;
                if (_StringHelper.Matches(filter, input))
                {
                    // Logger.DLog($"Exclusion Filter Match [{filter}]: {input}");
                    return false;
                }
            }
        }

        if (Library.Filters?.Any() == true)
        {
            foreach (var filter in Library.Filters)
            {
                if (string.IsNullOrWhiteSpace(filter))
                    continue;
                if (_StringHelper.Matches(filter, input))
                {
                    // Logger.DLog($"Inclusion Filter Match [{filter}]: {input}");
                    return true;
                }
            }
            return false;
        }

        if (Library.Extensions?.Any() != true)
        {
            // default to true
            return true;
        }

        foreach (var extension in Library.Extensions)
        {
            var ext = extension.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext))
                continue;
            if (ext.StartsWith('.') == false)
                ext = "." + ext;
            if (input.ToLowerInvariant().EndsWith(ext))
                return true;
        }

        // didnt match extensions
        return false;
    }

    /// <summary>
    /// Checks if the dates on a file are the same as the known file
    /// </summary>
    /// <param name="file">the file to check</param>
    /// <param name="knownFile">the known file</param>
    /// <returns>true if the dates are the same</returns>
    private bool DatesAreSame(FileInfo file, LibraryFile knownFile)
    {
        if (datesSame(
                file.CreationTime, knownFile.CreationTime,
                file.LastWriteTime, knownFile.LastWriteTime
            ))
            return true;

        if (datesSame(
                file.CreationTimeUtc, knownFile.CreationTime,
                file.LastWriteTimeUtc, knownFile.LastWriteTime
            ))
            return true;

        return false;

        bool datesSame(DateTime create1, DateTime create2, DateTime write1, DateTime write2)
        {
            var createDiff = (int)Math.Abs(create1.Subtract(create2).TotalSeconds);
            var writeDiff = (int)Math.Abs(write1.Subtract(write2).TotalSeconds);

            bool create = createDiff < 5;
            bool write = writeDiff < 5;
            return create && write;
        }
    }

    /// <summary>
    /// Checks if a file/folder is in the top level of the library
    /// </summary>
    /// <param name="fullpath">the full path</param>
    /// <returns>true if in the top level, otherwise false</returns>
    private bool PathIsTopLevel(string fullpath)
    {
        if (Library.Folders)
        {
            var dir = new DirectoryInfo(fullpath);
            return dir.Parent.FullName == Library.Path;
        }
        else
        {
            var dir = new FileInfo(fullpath);
            return dir.Directory.FullName == Library.Path;
        }
    }

    /// <summary>
    /// Primitive check tgo see if a file is unchanged
    /// </summary>
    /// <param name="fileInfo">the file info</param>
    /// <param name="lfExisting">the library file</param>
    /// <returns>true if the unchanged</returns>
    private bool FileUnchanged(FileInfo fileInfo, LibraryFile lfExisting)
    {
        // do we really care if the dates are the same?
        // if (DatesAreSame(fileInfo, lfExisting) == false)
        //     return false;
        if (fileInfo.Length != lfExisting.OriginalSize && lfExisting.FinalSize != fileInfo.Length)
            return false;
        return true;
    }
}