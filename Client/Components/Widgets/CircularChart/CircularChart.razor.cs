using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Circular Chart
/// </summary>
public partial class CircularChart : ComponentBase
{
    /// <summary>
    /// Gets or sets the percent
    /// </summary>
    [Parameter]
    public double Percent { get; set; }

    /// <summary>
    /// Gets or sets the title
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Transactions";

    // Convert percent to the value needed for the stroke-dasharray (SVG path)
    private string _percent => (Percent / 100 * 100).ToString("F1");
}