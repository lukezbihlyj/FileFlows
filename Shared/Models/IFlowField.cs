namespace FileFlows.Shared.Models;

/// <summary>
/// Field that can be added to a form
/// </summary>
public interface IFlowField
{
    /// <summary>
    /// Gets or sets the order of which to display this filed
    /// </summary>
    int Order { get; set; }
    
}