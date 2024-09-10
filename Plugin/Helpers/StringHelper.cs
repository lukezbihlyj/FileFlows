using System.Text.Json;
using System.Text.RegularExpressions;

namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Helper for string operations
/// </summary>
/// <param name="_logger">the logger to use</param>
public class StringHelper(ILogger _logger)
{
    /// <summary>
    /// Checks if the input string represents a regular expression.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the input is a regular expression, otherwise false.</returns>
    public static bool IsRegex(string input)
        => input.StartsWith('/') && input.EndsWith('/') && input.Length > 2;
    // {
    //     return new[] { "?", "|", "^", "$", "*" }.Any(ch => input.Contains(ch));
    // }
    
    /// <summary>
    /// Matches an object, if value is a boolean then 1 or true will match true, and 0 or false will match false
    /// </summary>
    /// <param name="matchExpression">the match expression</param>
    /// <param name="value">the value</param>
    /// <returns>true if matches, otherwise false</returns>
    public bool Matches(string matchExpression, object? value)
    {
        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.False)
                value = false;
            else if (jsonElement.ValueKind == JsonValueKind.True)
                value = true;
            else if (jsonElement.ValueKind == JsonValueKind.Null)
                value = null;
        }
        
        if (value is bool bValue)
        {
            if (matchExpression is "1" or "^0")
                return bValue;
            if (matchExpression is "0" or "^1")
                return !bValue;
        }

        return Matches(matchExpression, value.ToString());
    }

    /// <summary>
    /// Checks if a value matches a specified match expression.
    /// </summary>
    /// <param name="matchExpression">
    /// The match expression to check against the value. Examples:
    /// <list type="bullet">
    /// <item><description>'abc' to match exactly 'abc'.</description></item>
    /// <item><description>'*abc*' to check if the value contains 'abc'.</description></item>
    /// <item><description>'*abc' to check if the value starts with 'abc'.</description></item>
    /// <item><description>'abc*' to check if the value ends with 'abc'.</description></item>
    /// <item><description>'!abc' to check if the value does not equal 'abc'.</description></item>
    /// <item><description>'!*abc' to check if the value does not start with 'abc'.</description></item>
    /// <item><description>'abc*' to check if the value does not end with 'abc'.</description></item>
    /// <item><description>'!*abc*' to check if the value does not contain 'abc'.</description></item>
    /// </list>
    /// </param>
    /// <param name="value">The value to be checked against the match expression.</param>
    /// <returns>Returns <c>true</c> if the value matches the match expression; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// bool result1 = Matches("abc", "abc"); // returns true
    /// bool result2 = Matches("*abc*", "xyzabcxyz"); // returns true
    /// bool result3 = Matches("*abc", "abcxyz"); // returns true
    /// bool result4 = Matches("abc*", "abcdef"); // returns true
    /// bool result5 = Matches("!abc", "xyz"); // returns true
    /// bool result6 = Matches("!*abc", "xyzabc"); // returns true
    /// bool result7 = Matches("abc*", "xyzabc"); // returns true
    /// bool result8 = Matches("!*abc*", "xyzxyz"); // returns true
    /// bool result9 = Matches("^abc$", "abc"); // returns true (regex example)
    /// bool result10 = Matches("[invalid", "abc"); // returns false (regex example)
    /// </code>
    /// </example>
    public bool Matches(string matchExpression, string value)
    {
        matchExpression = matchExpression?.Trim() ?? string.Empty;
        value = value?.Trim() ?? string.Empty;

        bool invert = false;

        if (matchExpression.StartsWith('!'))
        {
            invert = true;
            matchExpression = matchExpression[1..];
        }

        // Handle exact match
        if (string.Equals(matchExpression, value, StringComparison.InvariantCultureIgnoreCase))
        {
            _logger?.ILog($"Match found: '{value}' equals '{matchExpression}'" + (invert ? " (negated)" : ""));
            return !invert;
        }

        // Handle contains
        if (matchExpression.StartsWith('*') && matchExpression.EndsWith('*'))
        {
            bool contains = value.Contains(matchExpression[1..^1], StringComparison.InvariantCultureIgnoreCase);
            _logger?.ILog($"Match found: '{value}' contains '{matchExpression}'" + (invert ? " (negated)" : ""));
            return invert ? !contains : contains;
        }

        // Handle starts with
        if (matchExpression.EndsWith('*'))
        {
            bool startsWith = value.StartsWith(matchExpression[..^1], StringComparison.InvariantCultureIgnoreCase);
            _logger?.ILog($"Match found: '{value}' starts with '{matchExpression}'" + (invert ? " (negated)" : ""));
            return invert ? !startsWith : startsWith;
        }

        // Handle ends with
        if (matchExpression.StartsWith('*'))
        {
            bool endsWith = value.EndsWith(matchExpression[1..], StringComparison.InvariantCultureIgnoreCase);
            _logger?.ILog($"Match found: '{value}' ends with '{matchExpression}'" + (invert ? " (negated)" : ""));
            return invert ? !endsWith : endsWith;
        }

        if (IsRegex(matchExpression) == false)
            return invert;

        // Handle regex
        try
        {
            if (matchExpression.StartsWith("/") && matchExpression.EndsWith("/"))
                matchExpression = matchExpression[1..^1];
            
            var rgx = new Regex(matchExpression, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            bool matchesRegex = rgx.IsMatch(value);
            _logger?.ILog($"Match found: '{value}' matches pattern '{matchExpression}'" + (invert ? " (negated)" : ""));
            return invert ? !matchesRegex : matchesRegex;
        }
        catch (Exception)
        {
            // Ignored
        }

        return invert;
    }
}