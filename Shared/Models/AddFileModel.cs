namespace FileFlows.Shared.Models;

/// <summary>
/// A result from the add file dialog
/// </summary>
public class AddFileModel
{
    /// <summary>
    /// Gets or sets the UID of the flow
    /// </summary>
    public Guid FlowUid { get; set; }
    /// <summary>
    /// Gets or sets the UID of the node
    /// </summary>
    public Guid? NodeUid { get; set; }
    /// <summary>
    /// Gets or sets the files
    /// </summary>
    public List<string> Files { get; set; }
    /// <summary>
    /// Gets or sets the custom variables
    /// </summary>
    public Dictionary<string, object> CustomVariables { get; set; }
}