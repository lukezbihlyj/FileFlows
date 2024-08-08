using System.Text.RegularExpressions;

namespace FileFlows.Plugin.Formatters;

/// <summary>
/// Formatter that makes a relative path from a URL
/// </summary>
public class UrlRelativePathFormatter : Formatter
{
    /// <inheritdoc />
    public override bool IsMatch(string format)
        => format.ToLowerInvariant() is "url-relative-path" or "url-rp";
    
    /// <inheritdoc />
    public override string Format(object value, string format)
    {
        var str = value?.ToString() ?? string.Empty;
        try
        {
            // Create a Uri object from the URL
            var uri = new Uri(str);

            // Get the path without the query
            var path = uri.AbsolutePath;

            // Get the query part and replace the '=' and '&' with '-'
            var query = uri.Query.TrimStart('?').Replace('=', '-').Replace('&', '/');

            // Combine the path and the modified query
            path = path.TrimEnd('/') + (string.IsNullOrEmpty(query) ? string.Empty : "/" + query);

            // Sanitize the path to remove unsafe characters and directory traversal sequences
            path = SanitizePath(path);
            
            return path;
        }
        catch (Exception)
        {
            return str;
        }
    }
    private string SanitizePath(string path)
    {
        // Remove directory traversal sequences
        path = Regex.Replace(path, @"(\.\./)+", string.Empty);

        // Allow only safe characters (a-z, A-Z, 0-9, -, _, /, .) and remove any other characters
        path = Regex.Replace(path, @"[^a-zA-Z0-9\-_./]", string.Empty);

        // Remove redundant slashes and prevent any remaining '..'
        path = Regex.Replace(path, @"//+", "/", RegexOptions.None);
        path = Regex.Replace(path, @"(?<!/)\.\.(?!/)", string.Empty);

        // Remove leading and trailing slashes
        path = path.TrimStart('/').TrimEnd('/');

        return path;
    }
}