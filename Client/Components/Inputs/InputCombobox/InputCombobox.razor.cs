using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input combobox 
/// </summary>
public partial class InputCombobox : Input<string>
{
    /// <summary>
    /// Gets or sets teh options in the combobox
    /// </summary>
    [Parameter]
    public List<ListOption> Options { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
                $"./Components/Inputs/InputCombobox/InputCombobox.razor.js?v={Globals.Version}");
            await jsObjectReference.InvokeVoidAsync("createInputCombobox", DotNetObjectReference.Create(this), Uid.ToString(),
                Options.Select(x => x.Label).ToList());
        }
    }
    

    /// <summary>
    /// Updates the value from JavaScript
    /// </summary>
    /// <param name="value">the updated value</param>
    /// <returns>a task to await</returns>
    [JSInvokable]
    public Task UpdateValue(string value)
    {
        Value = value;
        StateHasChanged();
        return Task.CompletedTask;
    }
}