using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Container Widget
/// </summary>
public partial class ContainerWidget : ComponentBase
{
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    [Parameter] public string? Title { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of the widget
    /// </summary>
    [Parameter] public string? Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the icon
    /// </summary>
    [Parameter] public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the selected Mode
    /// </summary>
    [Parameter] public int Mode { get; set; }

    /// <summary>
    /// Gets or sets a callback for when the Mode changes
    /// </summary>
    [Parameter]
    public EventCallback<int> ModeChanged { get; set; }
    
    // This method allows for propagating the changes properly
    private async Task OnModeChanged(int newValue)
    {
        Mode = newValue;
        await ModeChanged.InvokeAsync(newValue);
    }
}