using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Connections.Features;
using ffElement = FileFlows.Shared.Models.FlowElement;

namespace FileFlows.Client.Components;

/// <summary>
/// Represents a base class for a component that displays a list of elements.
/// </summary>
public partial class FlowElementList : ComponentBase
{
    private string txtFilter;
    private ElementReference eleFilter;
    private string lblObsoleteMessage, lblFilter, lblAdd, lblClose;
    /// <summary>
    /// The selected item, used for mobile view so can add an element
    /// </summary>
    private string SelectedElement;

    /// <summary>
    /// The selected group for the accordion view
    /// </summary>
    private string SelectedGroup;
    
    /// <summary>
    /// Gets or sets the default selected group
    /// </summary>
    [Parameter] public string DefaultGroup { get; set; }


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblFilter = Translater.Instant("Labels.FilterPlaceholder");
        lblObsoleteMessage = Translater.Instant("Labels.ObsoleteConfirm.Message");
        lblAdd = Translater.Instant("Labels.Add");
        lblClose = Translater.Instant("Labels.Close");

        SetItems(Items);
    }

    /// <summary>
    /// Gets or sets if the filter is currently applied
    /// </summary>
    private bool Filtering { get; set; }
    
    /// <summary>
    /// Gets or sets the items to display in the list.
    /// </summary>
    [Parameter]
    public IEnumerable<ffElement> Items { get; set; }

    /// <summary>
    /// Gets or sets the filtered items to display in the list.
    /// </summary>
    private IEnumerable<ffElement> Filtered { get; set; }
    
    /// <summary>
    /// Gets or sets the event to open the browser
    /// </summary>
    [Parameter] public Action OpenBrowser { get; set; }
    /// <summary>
    /// Gets or sets the label to open the browser
    /// </summary>
    [Parameter] public string OpenBrowserLabel { get; set; }
    /// <summary>
    /// Gets or sets the icon to open the browser
    /// </summary>
    [Parameter] public string OpenBrowserIcon { get; set; }
    
    /// <summary>
    /// Gets or sets the event to close the element list when viewed on mobile
    /// </summary>
    [Parameter] public Action Close { get; set; }
    
    /// <summary>
    /// Gets or sets the event that handles adding a selected item when viewed on mobile
    /// </summary>
    [Parameter] public Action<string> AddSelectedElement { get; set; }
    
    /// <summary>
    /// Gets or sets the event callback for the drag start event.
    /// </summary>
    [Parameter] public EventCallback<(DragEventArgs, FlowElement)> OnDragStart { get; set; }

    /// <summary>
    /// Regex used to check if a group specifies indexes for order
    /// </summary>
    private Regex GroupIndexRegex = new Regex("(.*?):([\\d]+)$");

    /// <summary>
    /// Handles the key down event for filtering.
    /// </summary>
    /// <param name="e">The keyboard event arguments.</param>
    protected void FilterKeyUp(KeyboardEventArgs e)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Applies the filter to the items list.
    /// </summary>
    protected void ApplyFilter()
    {
        Filtering = false;
        if (Items == null)
            return;

        string filter = txtFilter?.Trim()?.Replace(" ", string.Empty)?.ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(filter))
        {
            Filtered = Items;
        }
        else
        {
            Filtering = true;
            Filtered = Items
                .Where(x => x.DisplayName.ToLowerInvariant().Replace(" ", "").Contains(filter)
                            || x.Group.ToLowerInvariant().Replace(" ", "").Contains(filter) ||
                            x.Uid.ToLowerInvariant().Equals(filter, StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();
        }
    }

    /// <summary>
    /// Invokes the <see cref="OnSelectPart"/> event callback asynchronously.
    /// </summary>
    /// <param name="uid">The unique identifier of the selected element.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected void SelectPart(string uid)
    {
        if (App.Instance.IsMobile)
            SelectedElement = uid;
    }

    protected void DragStart(DragEventArgs e, FlowElement element)
        => _ = OnDragStart.InvokeAsync((e, element));

    private void SelectGroup(string group)
        => SelectedGroup = SelectedGroup == group ? null : group;

    private void FixItem(FlowElement x)
    {
        if (x.Type != FlowElementType.Script)
            return;
        
        // get the group name from the script name, eg 'File - Older Than', becomes 'File' Group and name 'Older Than'
        int index = x.Name.IndexOf(" - ", StringComparison.InvariantCulture);
        if (index < 0)
            return;
        x.Group = x.Name[..(index)];
    }

    private object FormatName(ffElement ele)
    {
        if (ele.Name.StartsWith(ele.Group + " - "))
            return ele.Name[(ele.Group.Length + 3)..];
        return ele.Name;
    }

    public void SetItems(IEnumerable<ffElement> items, string newDefaultGroup = "notset")
    {
        if (newDefaultGroup != "notset")
            DefaultGroup = newDefaultGroup;
        
        SelectedGroup = DefaultGroup;
        this.Items = Items;
        if (Items?.Any() == true)
        {
            foreach (var item in Items)
            {
                FixItem(item);
            }
        }

        ApplyFilter();
        StateHasChanged();
    }

    /// <summary>
    /// Gets the custom styling for a given flow element
    /// </summary>
    /// <param name="ele">the element</param>
    /// <returns>the custom styling</returns>
    private object GetElementStyling(ffElement ele)
    {
        if (string.IsNullOrWhiteSpace(ele.CustomColor))
            return string.Empty;
        return "--custom-color:" + ele.CustomColor;
    }
}