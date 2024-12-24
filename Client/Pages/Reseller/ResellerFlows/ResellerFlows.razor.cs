using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages.Reseller;

/// <summary>
/// Reseller Flows page
/// </summary>
public partial class ResellerFlows : ListPage<Guid, ResellerFlow>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/reseller/flows";

    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblPageTitle;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblPageTitle = Translater.Instant("Pages.Resellers.Flows.Title");
    }

    /// <summary>
    /// Adds an item
    /// </summary>
    private async Task Add()
    {
        await Edit(new ()
        {  
            Enabled = true, 
            Tokens = 10,
            MaxFileSize = 10_000_000
        });
    }
    
    /// <inheritdoc />
    public override async Task<bool> Edit(ResellerFlow item)
    {
        // this.EditingItem = library;
        return await OpenEditor(item);
    }
}