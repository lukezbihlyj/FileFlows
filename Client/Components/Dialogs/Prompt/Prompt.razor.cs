using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using FileFlows.Shared;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// A prompt dialog that asks the user for text input
/// </summary>
public partial class Prompt : ComponentBase, IDisposable
{
    private string lblOk, lblCancel;
    private string Message, Title;
    /// <summary>
    /// The task used for when the dialog is closed
    /// </summary>
    TaskCompletionSource<string> ShowTask;

    /// <summary>
    /// Gets or sets the singleton instance
    /// </summary>
    private static Prompt Instance { get; set; }

    /// <summary>
    /// Gets or sets if this is visible
    /// </summary>
    private bool Visible { get; set; }

    /// <summary>
    /// Gets or sets the current value
    /// </summary>
    private string Value { get; set; }

    /// <summary>
    /// The unique identifier for this dialog
    /// </summary>
    private readonly string Uid = Guid.NewGuid().ToString();

    /// <summary>
    /// If this has focus
    /// </summary>
    private bool Focus;

    /// <summary>
    /// Gets or sets the Javascript runtime
    /// </summary>
    [Inject] private IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Initializes the component
    /// </summary>
    protected override void OnInitialized()
    {
        this.lblOk = Translater.Instant("Labels.Ok");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        Instance = this;
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
    }

    /// <summary>
    /// Called when escape is pushed
    /// </summary>
    /// <param name="args">the args for the event</param>
    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Show a dialog
    /// </summary>
    /// <param name="title">the title of the dialog</param>
    /// <param name="message">the message of the dialog</param>
    /// <param name="value">the current value</param>
    /// <returns>an task to await for the dialog result</returns>
    public static Task<string> Show(string title, string message, string value = "")
    {
        if (Instance == null)
            return Task.FromResult<string>("");

        return Instance.ShowInstance(title, message, value);
    }

    /// <summary>
    /// Show an instance of the dialog
    /// </summary>
    /// <param name="title">the title of the dialog</param>
    /// <param name="message">the message of the dialog</param>
    /// <param name="value">the current value</param>
    /// <returns>an task to await for the dialog result</returns>
    private Task<string> ShowInstance(string title, string message, string value = "")
    {
        this.Title = Translater.TranslateIfNeeded(title?.EmptyAsNull() ?? "Labels.Prompt");
        this.Message = Translater.TranslateIfNeeded(message ?? "");
        this.Value = value ?? "";
        this.Visible = true;
        this.Focus = true;
        this.StateHasChanged();

        Instance.ShowTask = new TaskCompletionSource<string>();
        return Instance.ShowTask.Task;
    }

    /// <summary>
    /// After the component was rendered
    /// </summary>
    /// <param name="firstRender">true if this is after the first render of the component</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Focus)
        {
            Focus = false;
            await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
        }
    }

    /// <summary>
    /// A key was pressed
    /// </summary>
    /// <param name="e">the keyboard event</param>
    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.AltKey || e.CtrlKey || e.ShiftKey || string.IsNullOrWhiteSpace(Value))
            return;
        if (e.Key == "Enter")
        {
            Accept();
        }
    }

    /// <summary>
    /// Accept the dialog
    /// </summary>
    private async void Accept()
    {
        this.Visible = false;
        Instance.ShowTask.TrySetResult(Value);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Cancel the dialog
    /// </summary>
    private async void Cancel()
    {
        this.Visible = false;
        Instance.ShowTask.TrySetResult("");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of the cancel
    /// </summary>
    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}