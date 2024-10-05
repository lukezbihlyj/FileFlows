using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// Progress bar that shows the progress of a file through a flow
/// </summary>
public partial class FlowProgress : ComponentBase
{
    /// <summary>
    /// Gets or sets the total parts
    /// </summary>
    [Parameter] public int TotalParts { get; set; }
    /// <summary>
    /// Gets or sets the current part
    /// </summary>
    [Parameter] public int CurrentPart { get; set; }
    /// <summary>
    /// Gets or sets the current percent of the executing flow part
    /// </summary>
    [Parameter]public float CurrentPartPercent { get; set; }
}