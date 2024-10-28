using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// Dialog used when adding a file manually
/// </summary>
public partial class AddFileDialog : VisibleEscapableComponent
{
    /// <summary>
    /// The task returned when showing the dialog
    /// </summary>
    TaskCompletionSource<AddFileModel> ShowTask;

    /// <summary>
    /// Gets or sets the files in the list mode
    /// </summary>
    private List<string> Files = new ();
    /// <summary>
    /// Gets or sets the text list when using raw mode
    /// </summary>
    private string TextList { get; set; }
    /// <summary>
    /// Gets or sets the text of the new item being added in the list mode
    /// </summary>
    private string NewItem { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the flow to run against
    /// </summary>
    private Guid FlowUid { get; set; }
    /// <summary>
    /// Gets or sets the flows in the system
    /// </summary>
    private List<KeyValuePair<Guid, string>> Flows { get; set; }
    /// <summary>
    /// Gets or sets the Node to run against
    /// </summary>
    private Guid NodeUid { get; set; }
    /// <summary>
    /// Gets or sets the nodes in the system
    /// </summary>
    private List<KeyValuePair<Guid, string>> Nodes { get; set; }
    /// <summary>
    /// The entering files mode, 0 = list, 1 = raw text field
    /// </summary>
    private int Mode = 0;
    /// <summary>
    /// The strings for translations
    /// </summary>
    private string lblTitle, lblDescription, lblMode, lblFlow, lblNode, lblAnyNode, lblAdd, lblCancel, lblList, lblRaw;

    /// <summary>
    /// The input options
    /// </summary>
    private List<ListOption> ModeOptions, FlowOptions = new (), NodeOptions = new ();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Dialogs.AddFile.Title");
        lblDescription = Translater.Instant("Dialogs.AddFile.Description");
        lblMode = Translater.Instant("Dialogs.AddFile.Fields.Mode");
        lblFlow = Translater.Instant("Dialogs.AddFile.Fields.Flow");
        lblNode = Translater.Instant("Dialogs.AddFile.Fields.Node");
        lblAnyNode = Translater.Instant("Dialogs.AddFile.Fields.AnyNode");
        lblAdd = Translater.Instant("Labels.Add");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblList = Translater.Instant("Dialogs.AddFile.Fields.List");
        lblRaw = Translater.Instant("Dialogs.AddFile.Fields.Raw");
        ModeOptions =
        [
            new() { Label = lblList, Value = 0 },
            new() { Label = lblRaw, Value = 1 },
        ];
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
    /// Shows the language picker
    /// </summary>
    /// <param name="flows">the list of flows</param>
    /// <param name="nodes">the list of nodes</param>
    /// <returns>the task to await</returns>
    public Task<AddFileModel> Show(Dictionary<Guid, string> flows, Dictionary<Guid, string> nodes)
    {
        Files = new ();
        TextList = string.Empty;
        CustomVariables = new();
        Visible = true;
        FlowOptions = flows.OrderBy(x => x.Value.ToLowerInvariant())
            .Select(x => new ListOption { Label = x.Value, Value = x.Key }).ToList();
        FlowUid = (Guid)FlowOptions.First().Value!;
        
        NodeOptions = nodes.OrderBy(x => x.Value.ToLowerInvariant())
            .Select(x => new ListOption { Label = x.Value == "FileFlowsServer" ? "Internal Processing Node" : x.Value, Value = x.Key }).ToList();

        NodeOptions.Insert(0, new() { Label = lblAnyNode, Value = Guid.Empty });
        NodeUid = Guid.Empty;
        StateHasChanged();

        ShowTask = new TaskCompletionSource<AddFileModel>();
        return ShowTask.Task;
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public override void Cancel()
    {
        this.Visible = false;
        ShowTask.TrySetResult(new ());
    }

    /// <summary>
    /// Language is chosen
    /// </summary>
    private async void Save()
    {
        this.Visible = false;

        var dict = new Dictionary<string, object>();
        foreach (var kv in CustomVariables)
        {
            if (dict.Keys.Any(x => x.Equals(kv.Key, StringComparison.InvariantCultureIgnoreCase)))
                continue;
            dict[kv.Key] = ObjectHelper.StringToObject(kv.Value);
        }
        
        ShowTask.TrySetResult(new AddFileModel()
        {
            FlowUid = FlowUid,
            NodeUid = NodeUid,
            CustomVariables = dict,
            Files = Mode == 0 ? Files : TextList.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).Distinct().ToList()
        });
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets or sets the input mode
    /// </summary>
    private object BoundMode
    {
        get => Mode;
        set
        {
            if (value is int index == false)
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
    }
    
    /// <summary>
    /// Gets or sets flow uid
    /// </summary>
    private object BoundFlowUid
    {
        get => FlowUid;
        set
        {
            if (value is Guid uid)
                FlowUid = uid;
        }
    }
    
    /// <summary>
    /// Gets or sets node uid
    /// </summary>
    private object BoundNodeUid
    {
        get => NodeUid;
        set
        {
            if (value is Guid uid)
                NodeUid = uid;
        }
    }

    /// <summary>
    /// Gets or sets the custom variables
    /// </summary>
    private List<KeyValuePair<string, string>> CustomVariables { get; set; } = new();
}