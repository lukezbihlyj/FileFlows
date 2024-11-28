using FileFlows.Managers;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for Resources
/// </summary>
public class ResourceService
{
    /// <summary>
    /// Gets all the resources in the system
    /// </summary>
    /// <returns>all the resources</returns>
    public Task<List<Resource>> GetAllAsync()
        => new ResourceManager().GetAll();

    /// <summary>
    /// Gets a resource by its UID
    /// </summary>
    /// <param name="uid">the UID of the resource</param>
    /// <returns></returns>
    public Task<Resource?> GetByUidAsync(Guid uid)
        => new ResourceManager().GetByUid(uid);

    /// <summary>
    /// Gets a resource by its name
    /// </summary>
    /// <param name="name">the name of the resource</param>
    /// <returns>the resource if found</returns>
    public Task<Resource?> GetByNameAsync(string name)
        => new ResourceManager().GetByName(name);

    /// <summary>
    /// Updates a resource
    /// </summary>
    /// <param name="resource">the resource to update</param>
    /// <param name="auditDetails">The audit details</param>
    /// <returns>the update result</returns>
    public Task<Result<Resource>> Update(Resource resource, AuditDetails? auditDetails)
        => new ResourceManager().Update(resource, auditDetails);

    /// <summary>
    /// Deletes the given resources
    /// </summary>
    /// <param name="uids">the UID of the resources to delete</param>
    /// <param name="auditDetails">the audit details</param>
    /// <returns>a task to await</returns>
    public Task Delete(Guid[] uids, AuditDetails auditDetails)
        => new ResourceManager().Delete(uids, auditDetails);
    
}