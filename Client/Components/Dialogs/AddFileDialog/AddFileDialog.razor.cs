using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Dialog used when adding a file manually
/// </summary>
public partial class AddFileDialog : ComponentBase, IDisposable
{
    
    /// <summary>
    /// Gets or sets if this dialog is visible
    /// </summary>
    private bool Visible { get; set; }
    TaskCompletionSource<(Guid FlowUid, List<string> Files)> ShowTask;

    private List<string> Files = new ();
    /// <summary>
    /// Gets or sets the text list when using raw mode
    /// </summary>
    private string TextList { get; set; }
    private string NewItem { get; set; } = string.Empty;
    private Guid FlowUid { get; set; }
    private List<KeyValuePair<Guid, string>> Flows { get; set; }
    private int Mode = 0;
    
    private string lblTitle, lblDescription, lblMode, lblFlow, lblAdd, lblCancel, lblList, lblRaw;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Dialogs.AddFile.Title");
        lblDescription = Translater.Instant("Dialogs.AddFile.Description");
        lblMode = Translater.Instant("Dialogs.AddFile.Fields.Modes");
        lblFlow = Translater.Instant("Dialogs.AddFile.Fields.Flow");
        lblAdd = Translater.Instant("Labels.Add");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblList = Translater.Instant("Dialogs.AddFile.Fields.List");
        lblRaw = Translater.Instant("Dialogs.AddFile.Fields.Raw");
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
    }

    /// <summary>
    /// Removes an item by index
    /// </summary>
    /// <param name="index">The index of the item to remove</param>
    private void Remove(int index)
    {
        if (index >= 0 && index < Files.Count)
        {
            Files.RemoveAt(index);
        }
    }

    /// <summary>
    /// Adds an item to the list
    /// </summary>
    private void Add()
    {
        string newItem = NewItem.Trim();
        if (string.IsNullOrWhiteSpace(newItem) == false && Files.Contains(newItem) == false)
        {
            Files.Add(newItem);
            NewItem = string.Empty;
        }
    }

    /// <summary>
    /// Opens the browser 
    /// </summary>
    private async Task Browse(int index)
    {
        var start = index >= 0 && index <= Files.Count ? Files[index] : NewItem;
        var result = await FileBrowser.Show(start);
        if (string.IsNullOrEmpty(result))
            return;
        
        if (index >= 0)
            Files[index] = result;
        else
            Files.Add(result);
        StateHasChanged();
    }

    /// <summary>
    /// Escaped is pressed
    /// </summary>
    /// <param name="args">the escape arguments</param>
    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            this.StateHasChanged();
        }
    }
    
    
    /// <summary>
    /// Event when the mode changed
    /// </summary>
    /// <param name="args">the change event</param>
    private void ModeChanged(ChangeEventArgs args)
    {
        if (int.TryParse(args?.Value?.ToString(), out int index) == false)
            return;
        if (Mode == index)
            return;
        if (index == 0)
        {
            Files = TextList.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
        }
        else
        {
            TextList = string.Join("\n", Files);
        }
        Mode = index;
    }
    /// <summary>
    /// Shows the language picker
    /// </summary>
    /// <param name="flows">the list of flows</param>
    /// <returns>the task to await</returns>
    public Task<(Guid FlowUid, List<string> Files)> Show(Dictionary<Guid, string> flows)
    {
        Files = new ();
        TextList = string.Empty;
        Visible = true;
        Flows = flows.OrderBy(x => x.Value.ToLowerInvariant())
            .Select(x => new KeyValuePair<Guid, string>(x.Key, x.Value)).ToList();
        FlowUid = Flows.First().Key;
        StateHasChanged();

        ShowTask = new TaskCompletionSource<(Guid FlowUid, List<string> Files)>();
        return ShowTask.Task;
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    private async void Cancel()
    {
        this.Visible = false;
        ShowTask.TrySetResult(new ());
        await Task.CompletedTask;
    }

    /// <summary>
    /// Language is chosen
    /// </summary>
    private async void Save()
    {
        this.Visible = false;
        ShowTask.TrySetResult((FlowUid, Files));
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        App.Instance.OnEscapePushed -= InstanceOnOnEscapePushed;
    }
}