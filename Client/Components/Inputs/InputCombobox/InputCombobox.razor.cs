using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input combobox 
/// </summary>
public partial class InputCombobox : Input<object>
{
    /// <summary>
    /// Gets or sets teh options in the combobox
    /// </summary>
    [Parameter]
    public List<ListOption> Options { get; set; }
    private string SelectedLabel
    {
        get => Options.FirstOrDefault(o => o.Value?.Equals(Value) ?? false)?.Label ?? string.Empty;
        set
        {
            var selectedOption = Options.FirstOrDefault(o => o.Label == value);
            if (selectedOption != null)
            {
                Value = selectedOption.Value;
            }
        }
    }

    /// <summary>
    /// Called when the value is changed
    /// </summary>
    /// <param name="e">the event args</param>
    private void OnValueChange(ChangeEventArgs e)
    {
        var selectedLabel = e.Value?.ToString();
        SelectedLabel = selectedLabel ?? string.Empty;
    }
}