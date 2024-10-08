using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// FileFlows Update widget
/// </summary>
public partial class FileFlowsUpdateWidget : ComponentBase
{
    /// <summary>
    /// Translations
    /// </summary>
    private string lblNew, lblFixed, lblUpdate;
    
    /// <summary>
    /// Gets or sets if this user can update FileFlows automatically
    /// </summary>
    [Parameter] public bool CanUpdate { get; set; }
    /// <summary>
    /// Gets or sets the Blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    
    /// <summary>
    /// Gets or sets the update data
    /// </summary>
    [Parameter] public UpdateInfo Data { get; set; }
    
    /// <summary>
    /// Event fired when a package is updated
    /// </summary>
    [Parameter] public EventCallback OnUpdate { get; set; }
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        lblNew = Translater.Instant("Pages.Dashboard.Widgets.Updates.FileFlows.New");
        lblFixed = Translater.Instant("Pages.Dashboard.Widgets.Updates.FileFlows.Fixed");
        lblUpdate = Translater.Instant("Labels.Update");
    }

    /// <summary>
    /// Performs the update
    /// </summary>
    private async Task Update()
    {
        if (CanUpdate == false)
            return;
        
        Blocker.Show();
        var available = await HttpHelper.Post<bool>("/api/settings/check-for-update-now");
        Blocker.Hide();
        if (available.Success == false)
        {
            Toast.ShowError("Pages.Settings.Messages.Update.Failed");
            return;
        }

        if (available.Data == false)
        {
            Toast.ShowInfo("Pages.Settings.Messages.Update.NotAvailable");
            return;
        }
        
        if (await Confirm.Show("Pages.Settings.Messages.Update.Title",
                "Pages.Settings.Messages.Update.Message") == false)
            return;
        await HttpHelper.Post("/api/settings/upgrade-now");
        Toast.ShowInfo("Pages.Settings.Messages.Update.Downloading");
        await OnUpdate.InvokeAsync();
    }
    
}