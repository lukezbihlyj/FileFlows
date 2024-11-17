using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.Server.LibraryUtils;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for libraries
/// </summary>
public class LibraryService : ILibraryService
{
    private Logger LibraryLogger;
    public LibraryService()
    {
        LibraryLogger = new();
        LibraryLogger.RegisterWriter(new FileLogger(DirectoryHelper.LoggingDirectory, "Library", false));
    }

    /// <summary>
    /// Sets up the library watches
    /// </summary>
    public async Task SetupWatches()
    {
        var libraries = (await GetAllAsync()).Select(x => x).ToList();
        foreach (var library in libraries)
        {
            if (library.Uid == CommonVariables.ManualLibraryUid || 
                library?.Name.Equals(CommonVariables.ManualLibrary, StringComparison.InvariantCulture) == true)
                continue; // don't watch the manual library
            WatchedLibraries[library.Uid] = new(LibraryLogger, library);
            WatchedLibraries[library.Uid].Start();
        }
    }
    
    /// <inheritdoc />
    public Task<Library?> GetByUidAsync(Guid uid)
        => new LibraryManager().GetByUid(uid);

    /// <inheritdoc />
    public Task<List<Library>> GetAllAsync()
        => new LibraryManager().GetAll();

    /// <summary>
    /// Gets if there are any libraries in the system
    /// </summary>
    /// <returns>true if there are some, otherwise false</returns>
    public async Task<bool> HasAny()
    {
        try
        {
            return await new LibraryManager().HasAny();
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Updates an library
    /// </summary>
    /// <param name="library">the library being updated</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the result of the update, if successful the updated item</returns>
    public async Task<Result<Library>> Update(Library library, AuditDetails? auditDetails)
    {
        var updated = await new LibraryManager().Update(library, auditDetails);
        if (WatchedLibraries.TryGetValue(library.Uid, out _))
        {
            WatchedLibraries[library.Uid].UpdateLibrary(updated);
        }
        else if(library.Uid != CommonVariables.ManualLibraryUid  // don't watch manual, this shouldn't ever be the case, but in case
                && library.Name?.Equals(CommonVariables.ManualLibrary, StringComparison.InvariantCulture) != true)
        {
            // create it
            WatchedLibraries[library.Uid] = new(LibraryLogger, updated);
            WatchedLibraries[library.Uid].Start();
        }
        return updated;
    }

    /// <summary>
    /// Deletes items matching the UIDs
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task Delete(Guid[] uids, AuditDetails auditDetails)
    {
        await new LibraryManager().Delete(uids, auditDetails);
        foreach (var uid in uids)
        {
            if (WatchedLibraries.TryGetValue(uid, out var watchedLibrary))
            {
                watchedLibrary.Dispose();
                WatchedLibraries.Remove(uid);
            }
        }
    }

    /// <summary>
    /// Updates the last scanned of a library to now
    /// </summary>
    /// <param name="uid">the UID of the library</param>
    public Task UpdateLastScanned(Guid uid)
        => new LibraryManager().UpdateLastScanned(uid);

    /// <summary>
    /// Updates all libraries with the new flow name if they used this flow
    /// </summary>
    /// <param name="uid">the UID of the flow</param>
    /// <param name="name">the new name of the flow</param>
    /// <returns>a task to await</returns>
    public Task UpdateFlowName(Guid uid, string name)
        => new LibraryManager().UpdateFlowName(uid, name);

    /// <summary>
    /// Gets a new unique name
    /// </summary>
    /// <param name="name">the name to base it off</param>
    /// <returns>a new unique name</returns>
    public Task<string> GetNewUniqueName(string name)
        => new LibraryManager().GetNewUniqueName(name);

    /// <summary>
    /// Rescans the libraries
    /// </summary>
    /// <param name="uids">The UIDs of the libraries to rescan</param>
    public void Rescan(Guid[] uids)
    {
        foreach (var uid in uids)
        {
            if (WatchedLibraries.TryGetValue(uid, out var watchedLibrary) == false)
                continue;
            _ = watchedLibrary.Scan();
        }
    }
    
    private Dictionary<Guid, WatchedLibraryNew> WatchedLibraries = new ();
}