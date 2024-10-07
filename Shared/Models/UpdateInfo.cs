namespace FileFlows.Shared.Models;
/// <summary>
/// Represents information about updates in the system, including FileFlows version, plugin updates, 
/// Docker mod updates, and script updates.
/// </summary>
public class UpdateInfo
{
    /// <summary>
    /// Gets or sets the current version of FileFlows.
    /// </summary>
    public string? FileFlowsVersion { get; set; }

    /// <summary>
    /// Gets or sets the list of plugin updates.
    /// </summary>
    public List<PackageUpdate> PluginUpdates { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of Docker mod updates.
    /// </summary>
    public List<PackageUpdate> DockerModUpdates { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of script updates.
    /// </summary>
    public List<PackageUpdate> ScriptUpdates { get; set; } = new();
}

/// <summary>
/// Represents details about a package update, including the package name, the current version,
/// and the latest available version.
/// </summary>
public class PackageUpdate
{
    /// <summary>
    /// Gets or sets the name of the package.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the current version of the package.
    /// </summary>
    public string CurrentVersion { get; set; }

    /// <summary>
    /// Gets or sets the latest available version of the package.
    /// </summary>
    public string LatestVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the icon for the package.
    /// </summary>
    public string? Icon { get; set; }
}
