using System.Text.RegularExpressions;

namespace FileFlows.Client.Helpers;

/// <summary>
/// Icon Helper
/// </summary>
public static class IconHelper
{
    /// <summary>
    /// Gets the image for the file
    /// </summary>
    /// <param name="path">the path of the file</param>
    /// <returns>the image to show</returns>
    public static string GetExtensionImage(string path)
    {
        if(string.IsNullOrWhiteSpace(path))
            return "/icons/filetypes/folder.svg";
        if(path.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || path.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            return "/icons/filetypes/html.svg";
        int index = path.LastIndexOf('.');
        if (index < 0)
            return "/icons/filetypes/folder.svg";
        string prefix = "/icon/filetype";
#if (DEBUG)
        prefix = "http://localhost:6868/icon/filetype";
#endif
        
        string extension = path[(index + 1)..].ToLowerInvariant();
        if (Regex.IsMatch(path, "^http(s)?://", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            extension = "url";
        return $"{prefix}/{extension}.svg";
    }
}