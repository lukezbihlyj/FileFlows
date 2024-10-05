using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class OptionButtons : ComponentBase, IWidgetRegistrar
{
    
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// The options for the widget
    /// </summary>
    private List<WidgetOption> Options = new();
    
    /// <summary>
    /// Gets or sets the selected value
    /// </summary>
    [Parameter] public int Value { get; set; }

    /// <summary>
    /// Gets or sets a callback for when the value changes
    /// </summary>
    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }


    /// <summary>
    /// Selects the option
    /// </summary>
    /// <param name="option">the option to select</param>
    private void SelectOption(WidgetOption option)
    {
        Value = option.Value;
        _ = ValueChanged.InvokeAsync(option.Value);
    }

    /// <inheritdoc />
    public void Register(object component)
    {
        if (component is WidgetOption option && Options.Contains(option) == false)
        {
            Options.Add(option);
            StateHasChanged();
        }
    }
}