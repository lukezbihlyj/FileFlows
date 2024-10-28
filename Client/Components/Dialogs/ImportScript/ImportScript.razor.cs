using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Dialogs;

public partial class ImportScript : VisibleEscapableComponent
{
    private string lblImport, lblCancel;
    private string Title;
    TaskCompletionSource<List<string>> ShowTask;
    private List<ListOption> AvailableScript;

    private List<object> CheckedItems = new();

    [Inject] private IJSRuntime jsRuntime { get; set; }

    protected override void OnInitialized()
    {
        this.lblImport = Translater.Instant("Labels.Import");
        this.lblCancel = Translater.Instant("Labels.Cancel");
        this.Title = Translater.Instant("Dialogs.ImportScript.Title");
    }

    public Task<List<string>> Show(List<string> availableScripts)
    {
        this.Visible = true;
        this.CheckedItems.Clear();
        this.AvailableScript = availableScripts?.Select(x => new ListOption()
        {
            Label = x,
            Value = x
        }).ToList();
        this.StateHasChanged();

        ShowTask = new TaskCompletionSource<List<string>>();
        return ShowTask.Task;
    }

    private void OnChange(ChangeEventArgs args, ListOption opt)
    {
        bool @checked = args.Value as bool? == true;
        if (@checked && this.CheckedItems.Contains(opt.Value) == false)
            this.CheckedItems.Add(opt.Value);
        else if (@checked == false && this.CheckedItems.Contains(opt.Value))
            this.CheckedItems.Remove(opt.Value);
    }

    private async void Accept()
    {
        this.Visible = false;
        ShowTask.TrySetResult(CheckedItems.Select(x => x.ToString()!).ToList());
        await Task.CompletedTask;
    }

    public override void Cancel()
    {
        this.Visible = false;
        ShowTask.TrySetResult(new List<string>());
    }
}