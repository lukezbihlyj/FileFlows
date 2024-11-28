using FileFlows.Shared.Models;

namespace FileFlows.Server.LibraryUtils;

/// <summary>
/// Interface for a watched library
/// </summary>
public interface IWatchedLibrary: IDisposable
{
    Library Library { get; set; }
    void UpdateLibrary(Library library);
}