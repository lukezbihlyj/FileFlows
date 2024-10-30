using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a binary file
/// </summary>
public partial class InputBinary : Input<FileData>
{
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] private IJSRuntime JS { get; set; }
    /// <summary>
    /// The JS Input Binary class instance
    /// </summary>
    private IJSObjectReference jsInputBinary;

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblBrowse;
    
    public override bool Focus() => FocusUid();
    async Task Browse()
    {
        var file = await jsInputBinary.InvokeAsync<FileData>("chooseFile");
        if (file == null)
            return;

        Value = file;

        // Notify Blazor the Value property has changed
        await ValueChanged.InvokeAsync(Value);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        lblBrowse = Translater.Instant("Labels.Browse");
        var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import", $"./Components/Inputs/InputBinary/InputBinary.razor.js?v={Globals.Version}");
        jsInputBinary = await jsObjectReference.InvokeAsync<IJSObjectReference>("createInputBinary");
    }
}


/// <summary>
/// File Data
/// </summary>
public class FileData
{
    /// <summary>
    /// Gets or sets the files mime type
    /// </summary>
    public string MimeType { get; set; }
    /// <summary>
    /// Gets or sets the files binary content
    /// </summary>
    public byte[] Content { get; set; }
}