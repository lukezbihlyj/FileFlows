using System.Text.Json.Serialization;
using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for reports
/// </summary>
public partial class Reports : ListPage<Guid, ReportUiModel>
{
    /// <summary>
    /// Gets or sets the report form editor component
    /// </summary>
    private Editor ReportFormEditor { get; set; }
    
    /// <inheritdoc />
    public override string ApiUrl => "/api/report";

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}/definitions";

    /// <inheritdoc />
    public override Task PostLoad()
    {
        foreach (var report in this.Data ?? [])
        {
            report.Name = Translater.Instant($"Reports.{report.Type}.Name");
            report.Description = Translater.Instant($"Reports.{report.Type}.Description");
        }

        Data = Data.OrderBy(x => x.Name.ToLowerInvariant()).ToList();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Launches the report
    /// </summary>
    /// <param name="rd">the report definition</param>
    private Task Launch(ReportUiModel rd)
        => Edit(rd);

    /// <inheritdoc />
    public override Task<bool> Edit(ReportUiModel rd)
    {
        NavigationManager.NavigateTo($"/report/{rd.Uid}");
        return Task.FromResult(true);
    }
}

/// <summary>
/// Report UI Model
/// </summary>
public class ReportUiModel : ReportDefinition
{
    /// <summary>
    /// Gets or sets the name of the report
    /// </summary>
    [JsonIgnore]
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the description of the report
    /// </summary>
    [JsonIgnore]
    public string Description { get; set; }
}