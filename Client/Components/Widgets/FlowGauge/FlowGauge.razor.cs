using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class FlowGauge : ComponentBase
{
    [Parameter] public double OverallProgress { get; set; } // Outer ring progress (0-100)
    [Parameter] public double? InnerProgress { get; set; }  // Inner ring progress (optional, 0-100)

    protected string CurrentProgressText => InnerProgress.HasValue ? $"{InnerProgress:F1} %" : $"{OverallProgress:F1} %";

    private string OuterArcPath => CreateArcPath(75, 75, 65, 0.75);  // Adjust radius and angles as needed
    private string InnerArcPath => CreateArcPath(75, 75, 50, 0.75);

    private string CreateArcPath(double cx, double cy, double radius, double portion)
    {
        // Rotate the arc by 90 degrees by shifting the angles
        double startAngle = Math.PI * (1.5 - portion); // Start 90 degrees from the top
        double endAngle = Math.PI * (1.5 + portion);   // End at 270 degrees
        double startX = cx + radius * Math.Cos(startAngle);
        double startY = cy + radius * Math.Sin(startAngle);
        double endX = cx + radius * Math.Cos(endAngle);
        double endY = cy + radius * Math.Sin(endAngle);

        return $"M {startX} {startY} A {radius} {radius} 0 1 1 {endX} {endY}";
    }
}