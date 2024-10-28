namespace FileFlows.Shared.Models;

/// <summary>
/// A Tag used by the system
/// </summary>
public class Tag : FileFlowObject
{
    /// <summary>
    /// Gets or sets the description of the tag
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the Icon of the tag
    /// </summary>
    public string Icon { get; set; }
}