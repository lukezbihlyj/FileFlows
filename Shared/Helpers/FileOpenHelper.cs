using System.IO;
using System.Threading;

/// <summary>
/// Helper class for file open operations
/// </summary>
public static class FileOpenHelper
{
    /// <summary>
    /// Opens a file for reading without a read/write lock
    /// This allows other applications to read/write to the file while it is being read
    /// </summary>
    /// <param name="file">the file to open</param>
    /// <returns>the file stream</returns>
    public static FileStream OpenRead_NoLocks(string file)
        => File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    /// <summary>
    /// Opens a file for reading and writing without a read/write lock
    /// This allows other applications to keep reading/writing the file while it's being opened
    /// This is useful to check if CanRead/CanWrite is true on a file
    /// Actually writing to the file with this stream might have undesired effects
    /// </summary>
    /// <param name="file">the file to open</param>
    /// <returns>the file stream</returns>
    public static FileStream OpenForCheckingReadWriteAccess(string file)
        => File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

    /// <summary>
    /// Opens a file for writing without a write lock
    /// This allows other applications to read the file while it is being written to
    /// </summary>
    public static FileStream OpenWrite_NoReadLock(string file, FileMode fileMode)
        => File.Open(file, fileMode, FileAccess.Write, FileShare.Read);
    
    /// <summary>
    /// Opens a file for reading without a read/write lock, with retry attempts if the file is locked by another process.
    /// This allows other applications to read/write to the file while it is being read.
    /// </summary>
    /// <param name="file">The path of the file to open.</param>
    /// <param name="maxRetries">The maximum number of retry attempts if the file is unavailable due to another process.</param>
    /// <param name="delayMilliseconds">The delay in milliseconds between retry attempts.</param>
    /// <returns>A <see cref="FileStream"/> for reading the file.</returns>
    /// <exception cref="IOException">Thrown if the file cannot be opened after the maximum number of retry attempts.</exception>
    public static FileStream OpenRead_NoLocks_Retry(string file, int maxRetries = 20, int delayMilliseconds = 100)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (IOException) when (attempt < maxRetries)
            {
                // Wait briefly before retrying if file is in use by another process
                Thread.Sleep(delayMilliseconds);
            }
        }
        // Last attempt, throw the exception if it still fails
        return File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

}
