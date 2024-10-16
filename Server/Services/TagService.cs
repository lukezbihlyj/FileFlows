using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.ServerShared.Services;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for tags
/// </summary>
public class TagService 
{
    /// <inheritdoc />
    public async Task<List<Tag>?> GetAllAsync()
        => await new TagManager().GetAll();

    /// <summary>
    /// Deletes the given tags
    /// </summary>
    /// <param name="uids">the UID of the tags to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new TagManager().Delete(uids, auditDetails);

    /// <summary>
    /// Gets a tag by its UID
    /// </summary>
    /// <param name="uid">the UID of the tag</param>
    /// <returns>the tag</returns>
    public Task<Tag?> GetByUidAsync(Guid uid)
        => new TagManager().GetByUid(uid);


    /// <summary>
    /// Gets a tag by it's name
    /// </summary>
    /// <param name="name">the tag of the item</param>
    /// <param name="ignoreCase">if case should be ignored</param>
    /// <returns>the tag</returns>
    public Task<Tag?> GetByName(string name, bool ignoreCase = false)
        => new TagManager().GetByName(name, ignoreCase);

    /// <summary>
    /// Updates a tag
    /// </summary>
    /// <param name="tag">the tag to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the result</returns>
    public Task<Result<Tag>> Update(Tag tag, AuditDetails? auditDetails)
        => new TagManager().Update(tag, auditDetails);
}