using System.Text.RegularExpressions;

namespace FileFlows.Managers;

/// <summary>
/// Service for communicating with FileFlows server for reseller flows
/// </summary>
public class ResellerFlowManager : CachedManager<ResellerFlow>
{
    /// <inheritdoc />
    protected override bool SaveRevisions => false;

    /// <inheritdoc />
    protected override async Task<ResellerFlow?> GetByName(ResellerFlow item, bool ignoreCase = true)
    {
        string name = Normalize(item.Name);
        return (await GetData()).FirstOrDefault(x => Normalize(x.Name) == name);
    }
    
    
    /// <summary>
    /// Normalize the name to make matching loose
    /// </summary>
    /// <param name="input">the string to normalize</param>
    /// <returns>the normalizd string</returns>
    string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove spaces, special characters, and punctuation, and convert to lowercase
        return Regex.Replace(input, @"[\s\W_]+", "").ToLowerInvariant();
    }
}
