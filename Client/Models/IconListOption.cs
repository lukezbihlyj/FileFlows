using FileFlows.Plugin;

namespace FileFlows.Client.Models;

/// <summary>
/// List option with an icon
/// </summary>
public class IconListOption : ListOption
{
    /// <summary>
    /// Gets or sets the icon CSS to show 
    /// </summary>
    public string IconCss { get; set; }
    /// <summary>
    /// Gets or sets the icon URL to show 
    /// </summary>
    public string IconUrl { get; set; }
}