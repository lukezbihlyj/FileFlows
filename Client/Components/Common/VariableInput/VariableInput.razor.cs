using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// A variable text input that shows a variables drop down when { is pushed
/// </summary>
public partial class VariableInput:ComponentBase
{
    private string _Uid = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the UID of the control
    /// </summary>
    [Parameter]
    public string Uid
    {
        get => _Uid;
        set => _Uid = value;    
    }

    /// <summary>
    /// Gets or sets the variables available to show
    /// </summary>
    [Parameter]
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// The input element
    /// </summary>
    private ElementReference eleInput;

    private string _Value;
#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the value 
    /// </summary>
    [Parameter]
    public string Value
    {
        get => _Value;
        set
        {
            if (_Value == value)
                return;

            _Value = value ?? string.Empty;
            ValueChanged.InvokeAsync(_Value);
        }
    }
#pragma warning restore BL0007
    /// <summary>
    /// Gets or sets the placeholder text
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; }

    /// <summary>
    /// Gets or sets a callback for when the value changes
    /// </summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets a callback for when the value is submitted
    /// </summary>
    [Parameter]
    public EventCallback SubmitEvent { get; set; }
    [Parameter]
    public EventCallback CloseEvent { get; set; }
    
    /// <summary>
    /// Gets or sets the event that occurs on the blur event
    /// </summary>
    [Parameter] public EventCallback Blur { get; set; }
    
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Focuses the input
    /// </summary>
    public void Focus()
    {
        _ = jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
    }
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
            $"./Components/Common/VariableInput/VariableInput.razor.js?v={Globals.Version}");
        await jsObjectReference.InvokeVoidAsync("createVariableInput", DotNetObjectReference.Create(this), Uid, Variables);
    }

    
    /// <summary>
    /// Updates the value from JavaScript
    /// </summary>
    /// <param name="value">the updated value</param>
    /// <returns>a task to await</returns>
    [JSInvokable("updateValue")]
    public Task UpdateValue(string value)
    {
        this.Value = value;
        StateHasChanged(); 
        return Task.CompletedTask;
    }
}
