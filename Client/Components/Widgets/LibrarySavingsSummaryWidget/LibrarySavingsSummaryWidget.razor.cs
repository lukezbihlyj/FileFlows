using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Library Savings Summary Widget
/// </summary>
public partial class LibrarySavingsSummaryWidget : ComponentBase
{
    private LibrarySavingsComponent Component;

    private string lblTitle, lblAll, lblMonth;
    
    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    /// <summary>
    /// The key used to store the selected mode in local storage
    /// </summary>
    private const string LocalStorageKey = "LibrarySavingsSummaryWidget";
    
    /// <summary>
    /// Gets the mode
    /// </summary>
    private int _Mode = 0;
    /// <summary>
    /// Gets or sets the selected mode
    /// </summary>
    private int Mode
    {
        get => _Mode;
        set
        {
            _Mode = value;
            _ = LocalStorage.SetItemAsync(LocalStorageKey, value);
            Component.SetMode(value);
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblTitle = Translater.Instant("Pages.Dashboard.Widgets.LibrarySavings.Title");
        lblAll = Translater.Instant("Labels.All");
        lblMonth = Translater.Instant("Labels.MonthShort");
        Mode = Math.Clamp(await LocalStorage.GetItemAsync<int>(LocalStorageKey), 0, 1);
    }
}