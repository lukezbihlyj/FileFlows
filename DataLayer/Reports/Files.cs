using FileFlows.DataLayer.Reports.Charts;
using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.Shared.Formatters;
using FileFlows.Shared.Models;
using Humanizer;
using NPoco;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Files Report
/// </summary>
public class Files : Report
{
    /// <inheritdoc />
    public override Guid Uid => new Guid("68c44fa1-d797-408c-93a3-bd49f0233ea2");
    /// <inheritdoc />
    public override string Icon => "fas fa-file-alt";
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.AnyOrAll;
    /// <inheritdoc />
    public override ReportSelection NodeSelection => ReportSelection.AnyOrAll;
    /// <inheritdoc />
    public override ReportSelection FlowSelection  => ReportSelection.Any;
    /// <inheritdoc />
    public override ReportSelection TagSelection  => ReportSelection.AnyOrAll;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        using var db = await GetDb();
        string sql =
            $"select {Wrap("Name")}, {Wrap("NodeName")}, " +
            $"{Wrap("LibraryName")}, {Wrap("FlowName")}, " +
            $"{Wrap("OriginalSize")}, " +
            $"{Wrap("FinalSize")}, {Wrap("ProcessingStarted")}, {Wrap("ProcessingEnded")} " +
            $"from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";

        AddPeriodToSql(model, ref sql);
        AddLibrariesToSql(model, ref sql);
        AddFlowsToSql(model, ref sql);
        AddNodesToSql(model, ref sql);
        AddTagsToSql(model, ref sql);
        
        var files = await db.Db.FetchAsync<FileData>(sql);
        if (files.Count < 1)
            return string.Empty; // no data

        var builder = new ReportBuilder(emailing);
        
        builder.StartLargeTableRow();
        builder.AddRowItem(TableGenerator.Generate(nameof(Files), files, emailing: emailing));
        builder.EndRow();
        
        return builder.ToString();
    }


    /// <summary>
    /// Represents the data for a node in the processing system.
    /// </summary>
    public class FileData
    {
        /// <summary>
        /// Gets or sets the relative name of the file.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        public string NodeName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the library.
        /// </summary>
        public string LibraryName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the flow.
        /// </summary>
        public string FlowName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the original size of the data before processing.
        /// </summary>
        public long OriginalSize { get; set; }

        /// <summary>
        /// Gets or sets the final size of the data after processing.
        /// </summary>
        public long FinalSize { get; set; }

        /// <summary>
        /// Gets or sets the date and time when processing started.
        /// </summary>
        [Ignore]
        public DateTime ProcessingStarted { get; set; }

        /// <summary>
        /// Gets or sets the date and time when processing ended.
        /// </summary>
        [Ignore]
        public DateTime ProcessingEnded { get; set; }

        /// <summary>
        /// Gets the percentage of the final size compared to the original size.
        /// </summary>
        public double Percentage => ((double)FinalSize / OriginalSize * 100);
    }
}