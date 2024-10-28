using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Dialog for choosing a script language
/// </summary>
public partial class ScriptLanguagePicker : VisibleEscapableComponent
{
    private string lblNext, lblCancel, lblJavaScriptDescription, lblBatchDescription, lblCSharpDescription, 
        lblPowerShellDescription, lblShellDescription, lblFileDisplayName, lblFileDisplayNameDescription;
    private string Title;
    TaskCompletionSource<Result<ScriptLanguage>> ShowTask;
    
    /// <summary>
    /// Gets or sets the language
    /// </summary>
    private ScriptLanguage Language { get; set; }

    /// <summary>
    /// If the file display name script option should be shown
    /// </summary>
    private bool showFileDisplayScript = false;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblNext = Translater.Instant("Labels.Next");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblJavaScriptDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.JavaScriptDescription");
        lblBatchDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.BatchDescription");
        lblCSharpDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.CSharpDescription");
        lblPowerShellDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.PowerShellDescription");
        lblShellDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.ShellDescription");
        lblFileDisplayName = Translater.Instant("Dialogs.ScriptLanguage.Labels.FileDisplayName");
        lblFileDisplayNameDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.FileDisplayNameDescription");
        Title = Translater.Instant("Dialogs.ScriptLanguage.Title");
    }
    
    /// <summary>
    /// Shows the language picker
    /// </summary>
    /// <param name="showFileDisplayScript">if the file display name script option should be shown</param>
    /// <returns>the task to await</returns>
    public Task<Result<ScriptLanguage>> Show(bool showFileDisplayScript)
    {
        this.showFileDisplayScript = showFileDisplayScript;
        this.Language = ScriptLanguage.JavaScript;
        this.Visible = true;
        this.StateHasChanged();

        ShowTask = new TaskCompletionSource<Result<ScriptLanguage>>();
        return ShowTask.Task;
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public override void Cancel()
    {
        this.Visible = false;
        ShowTask.TrySetResult(Result<ScriptLanguage>.Fail("Canceled"));
    }

    /// <summary>
    /// Language is choosen
    /// </summary>
    private async void Next()
    {
        this.Visible = false;
        ShowTask.TrySetResult(Language);
        await Task.CompletedTask;
    }
    
    private void SetLanguage(ScriptLanguage language, bool close = false)
    {
        Language = language;
        if(close)
            Next();
    }
}