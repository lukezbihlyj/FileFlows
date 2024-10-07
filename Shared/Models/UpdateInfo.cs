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
    /// Gets or sets the release notes
    /// </summary>
    public List<ReleaseNotes> ReleaseNotes { get; set; } = new();

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
    
    /// <summary>
    /// Gets if there are any updates
    /// </summary>
    public bool HasUpdates => PluginUpdates.Count != 0 || DockerModUpdates.Count != 0 || ScriptUpdates.Count != 0 || FileFlowsVersion != null;

    /// <summary>
    /// Gets if there are any extension updates
    /// </summary>
    public bool HasExtensionUpdates => PluginUpdates.Count != 0 || DockerModUpdates.Count != 0 || ScriptUpdates.Count != 0;
    
    /// <summary>
    /// Gets the number of updates
    /// </summary>
    public int NumberOfUpdates => PluginUpdates.Count + DockerModUpdates.Count + ScriptUpdates.Count + (FileFlowsVersion != null ? 1 : 0);
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

/// <summary>
/// Release Notes
/// </summary>
public class ReleaseNotes
{
    /// <summary>
    /// Gets or sets the version
    /// </summary>
    public string Version { get; set; }
    /// <summary>
    /// Gets or sets what is new in this version
    /// </summary>
    public List<string> New { get; set; } = new ();
    /// <summary>
    /// Gets or sets what is fixed in this version
    /// </summary>
    public List<string> Fixed { get; set; } = new ();
}