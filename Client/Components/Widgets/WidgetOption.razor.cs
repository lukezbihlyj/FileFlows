using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class WidgetOption : ComponentBase
{
    /// <summary>
    /// Gets or sets the registrar
    /// </summary>
    [CascadingParameter] public IWidgetRegistrar? Registrar { get; set; }
    
    /// <summary>
    /// Gets or sets the label of the option
    /// </summary>
    [Parameter] public string Label { get; set; }
    
    /// <summary>
    /// Gets or sets the icon to show
    /// </summary>
    [Parameter] public string? Icon { get; set; }
    
    /// <summary>
    /// Gets or sets the color to use
    /// </summary>
    [Parameter] public string? Color { get; set; }
    
    /// <summary>
    /// Gets or sets the value of the option
    /// </summary>
    [Parameter] public int Value { get; set; }
    
    /// <summary>
    /// Initializes the tab when it is first rendered.
    /// </summary>
    protected override void OnInitialized()
    {
        Registrar.Register(this);
    }
}