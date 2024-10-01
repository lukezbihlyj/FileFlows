using Microsoft.AspNetCore.Components;
using System.Text.Json;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input for a dropdown component
/// </summary>
public partial class InputDropDown : Input<object>
{
    /// <summary>
    /// Groups of options for the dropdown
    /// </summary>
    private Dictionary<string, List<ListOption>> Groups = new ();

    /// <summary>
    /// List of options for the dropdown
    /// </summary>
    private readonly List<ListOption> _Options = new ();

    /// <summary>
    /// Gets or sets if the description is shown
    /// </summary>
    [Parameter] public bool ShowDescription { get; set; }

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    private string Description { get; set; }

    /// <summary>
    /// If the value is currently being updated via UI action and if any update events received should be ignored during this update
    /// </summary>
    private bool UpdatingValue = false;

    /// <summary>
    /// Backing field for the Options property
    /// </summary>
    private IEnumerable<ListOption> _options;

    /// <summary>
    /// Gets or sets the options
    /// </summary>
    [Parameter]
    public IEnumerable<ListOption> Options
    {
        get => _options;
        set => _options = value;
    }

    /// <summary>
    /// Called when the component's parameters are set
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateOptions();
    }

    /// <summary>
    /// Updates the options for the dropdown
    /// </summary>
    private void UpdateOptions()
    {
        _Options.Clear();
        if (_options == null)
            return;
        string group = string.Empty;
        Groups = new Dictionary<string, List<ListOption>>();
        Groups.Add(string.Empty, new List<ListOption>());
        foreach (var lo in _options)
        {
            if (lo.Value is JsonElement je && je.ValueKind == JsonValueKind.String)
                lo.Value = je.GetString();  // this can happen from the Templates where the object is a JsonElement

            if (lo.Value is string && (string)lo.Value == Globals.LIST_OPTION_GROUP)
            {
                group = lo.Label!;
                Groups.Add(group, new List<ListOption>());
                continue;
            }
            if (Translater.NeedsTranslating(lo.Label))
                lo.Label = Translater.Instant(lo.Label);
            _Options.Add(lo);
            Groups[group].Add(lo);
        }
    }

    /// <summary>
    /// Indicates if the dropdown is open
    /// </summary>
    private bool IsOpen { get; set; }

    /// <summary>
    /// The selected option
    /// </summary>
    private ListOption SelectedOption { get; set; }

    /// <summary>
    /// Toggles the dropdown open or closed
    /// </summary>
    private void ToggleDropdown()
    {
        IsOpen = !IsOpen;
    }

    /// <summary>
    /// Selects an option from the dropdown
    /// </summary>
    /// <param name="option">The option to select</param>
    private void SelectOption(ListOption option)
    {
        if (option == null)
            SelectedOption =  new () { Label = lblSelectOne };
        else
            SelectedOption = option;
        Value = option.Value;
        IsOpen = false;
    }

    /// <inheritdoc />
    public override bool Focus() => FocusUid();

    /// <summary>
    /// Label for the "Select One" option
    /// </summary>
    private string lblSelectOne;

    /// <summary>
    /// Gets or sets if "Clear" is allowed
    /// </summary>
    [Parameter] public bool AllowClear { get; set; }

    /// <summary>
    /// Gets or sets if the clear label should be blank
    /// </summary>
    [Parameter] public bool BlankClearLabel { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblSelectOne = BlankClearLabel ? string.Empty : Translater.Instant("Labels.SelectOne");
        if (Value != null)
            SelectedOption = FindOption(Value);
        SelectedOption ??= new () { Label = lblSelectOne };
        ValueUpdated();
    }

    /// <summary>
    /// Called when the value updates
    /// </summary>
    protected override void ValueUpdated()
    {
        if (UpdatingValue)
            return;
        if (Value != null)
        {
            var opt = Options.ToArray();
            var valueJson = JsonSerializer.Serialize(Value);
            var objReference = TryParseObjectReference(valueJson);
            for (int i = 0; i < opt.Length; i++)
            {
                if (opt[i].Value == Value)
                    break;
                string optJson = JsonSerializer.Serialize(opt[i].Value);
                if (optJson.ToLower() == valueJson.ToLower())
                    break;

                if (objReference.Uid != Guid.Empty)
                {
                    // incase the object reference name has changed, we look for the UID
                    var otherObjReference = TryParseObjectReference(optJson);
                    if (otherObjReference.Uid == objReference.Uid)
                        break;
                }
            }
        }

        SelectedOption = FindOption(Value) ?? new() { Label = lblSelectOne };
        StateHasChanged();
    }

    
    /// <summary>
    /// Finds a option based on its value
    /// </summary>
    /// <param name="value">the value</param>
    /// <returns>the option if found</returns>
    private ListOption? FindOption(object value)
    {
        value = GetValue(value);
        foreach (var opt in _options)
        {
            var optValue = GetValue(opt.Value);
            if (optValue == value)
                return opt;
            if (optValue.Equals(value))
                return opt;
        }

        return null;
    }

    /// <summary>
    /// Gets the value
    /// </summary>
    /// <param name="value">the value</param>
    /// <returns>the value</returns>
    private object GetValue(object value)
    {
        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.False)
                return false;
            if (jsonElement.ValueKind == JsonValueKind.True)
                return true;
            if (jsonElement.ValueKind == JsonValueKind.Number)
                return jsonElement.GetInt32();
            if (jsonElement.ValueKind == JsonValueKind.String)
                return jsonElement.GetString();
        }

        return value;
    }

    /// <summary>
    /// Try parses an object reference from the JSON text
    /// </summary>
    /// <param name="json">The JSON text</param>
    /// <returns>The object reference, or empty if failed to parse</returns>
    private ObjectReference TryParseObjectReference(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ObjectReference>(json);
        }
        catch (Exception)
        {
            return new ObjectReference { Name = string.Empty, Type = string.Empty, Uid = Guid.Empty };
        }
    }

    /// <summary>
    /// Validates the input
    /// </summary>
    /// <returns>true if valid, otherwise false</returns>
    public override async Task<bool> Validate()
    {
        if (this.SelectedOption == null || this.SelectedOption.Label == lblSelectOne)
        {
            ErrorMessage = Translater.Instant($"Validators.Required");
            return false;
        }
        return await base.Validate();
    }

    /// <summary>
    /// Updates the description
    /// </summary>
    private void UpdateDescription()
    {
        Description = string.Empty;
        if (this.ShowDescription == false)
            return;

        var dict = Value as IDictionary<string, object>;

        if (dict == null)
        {
            try
            {
                string json = JsonSerializer.Serialize(Value);
                ExpandoObject? eo = JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(json);
                dict = eo == null ? new Dictionary<string, object>() : (IDictionary<string, object>)eo!;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        if (dict?.TryGetValue("Description", out var value) is true)
            Description = value?.ToString() ?? string.Empty;

        this.Help = Description;
        this.StateHasChanged();
    }

    /// <summary>
    /// Called when a key is down
    /// </summary>
    /// <param name="e">The event args</param>
    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Code == "Enter")
            await OnSubmit.InvokeAsync();
    }
}
