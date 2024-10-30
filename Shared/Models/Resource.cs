using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A resource that can be used in a flow or script
/// </summary>
public class Resource :FileFlowObject, IInUse
{
    /// <summary>
    /// Gets or sets the mime/type of the resource
    /// </summary>
    public string MimeType { get; set; }
    
    /// <summary>
    /// Gets or sets the binary date of the resource
    /// </summary>
    public byte[] Data { get; set; }
    
    /// <summary>
    /// Gets or sets what is using this object
    /// </summary>
    [DbIgnore]
    public List<ObjectReference> UsedBy { get; set; }
}