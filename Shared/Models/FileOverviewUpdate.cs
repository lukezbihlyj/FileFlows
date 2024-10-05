namespace FileFlows.Shared.Models;

/// <summary>
/// File Overview Data
/// </summary>
public class FileOverviewData
{
    /// <summary>
    /// Gets or sets the data for the last 24 hours.
    /// </summary>
    public Dictionary<DateTime, DashboardFileData> Last24Hours { get; init; }
    /// <summary>
    /// Gets or sets the data for the last 7 days.
    /// </summary>
    public Dictionary<DateTime, DashboardFileData> Last7Days{ get; init; }
    /// <summary>
    /// Gets or sets the data for the last 31 days.
    /// </summary>
    public Dictionary<DateTime, DashboardFileData> Last31Days{ get; init; }
}

/// <summary>
/// Represents file data statistics such as file count and file size.
/// </summary>
public record DashboardFileData
{
    /// <summary>
    /// Gets or sets the number of files processed.
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Gets or sets the total storage saved
    /// </summary>
    public long StorageSaved { get; set; }
}
