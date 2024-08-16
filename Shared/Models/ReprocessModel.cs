using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// Reprocess model
/// </summary>
public class ReprocessModel
{
    /// <summary>
    /// Gets or sets the UID to process
    /// </summary>
    public List<Guid> Uids { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the custom variables
    /// </summary>
    public Dictionary<string, object> CustomVariables { get; set; }
    
    /// <summary>
    /// Gets or sets the custom variables mode
    /// </summary>
    public CustomVariablesMode Mode { get; set; }
    
    /// <summary>
    /// Gets or sets the flow to reprocess these files with
    /// </summary>
    public ObjectReference? Flow { get; set; }
    
    /// <summary>
    /// Gets or sets the node to reprocess these files on
    /// </summary>
    public ObjectReference? Node { get; set; }
    
    /// <summary>
    /// Gets or sets if these files should be reprocessed at the bottom of the queue
    /// </summary>
    public bool BottomOfQueue { get; set; }

    /// <summary>
    /// Custom variable modes
    /// </summary>
    public enum CustomVariablesMode
    {
        /// <summary>
        /// The original custom variables of the file should be used
        /// </summary>
        Original,
        /// <summary>
        /// The custom variables should be merged with the original
        /// </summary>
        Merge,
        /// <summary>
        /// Replace the variables with the ones specified
        /// </summary>
        Replace
    }
}