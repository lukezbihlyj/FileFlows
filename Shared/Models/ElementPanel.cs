namespace FileFlows.Shared.Models;

/// <summary>
/// Panel element
/// </summary>
public class ElementPanel : IFlowField
{
    /// <inheritdoc />
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the fields to show in this panel
    /// </summary>
    public List<IFlowField> Fields { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the number of columns in this panel
    /// </summary>
    public int Columns { get; set; }
}