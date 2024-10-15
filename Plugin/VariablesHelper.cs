using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace FileFlows.Plugin;

using System.Text.RegularExpressions;

/// <summary>
/// A class to help the replacement of variables
/// </summary>
public class VariablesHelper
{
    /// <summary>
    /// The time this was started at.
    /// </summary>
    public static DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// Replaces variables in a given string
    /// </summary>
    /// <param name="input">the input string</param>
    /// <param name="variables">the variables used to replace</param>
    /// <param name="stripMissing">if missing variables shouild be removed</param>
    /// <param name="cleanSpecialCharacters">if special characters (eg directory path separator) should be replaced</param>
    /// <param name="encoder">Optional function to encode the variable values before replacing them</param>
    /// <returns>the string with the variables replaced</returns>
    public static string ReplaceVariables(string input, Dictionary<string, object> variables, bool stripMissing = false, bool cleanSpecialCharacters = false, Func<string, string> encoder = null)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        foreach(Match match in Regex.Matches(input,  @"{([a-zA-Z_][a-zA-Z0-9_.]*)(?:(!)|[:|]([^{}]*))?}"))
        {
            string variablePath = match.Groups[1].Value;

            // Try to resolve the variable from the dictionary
            object? value = variables != null ? ResolveVariable(variables, variablePath) : null;
            string? variableName = value == null ? variablePath : null;

            // Handle missing variables
            if (value == null)
                value = stripMissing ? string.Empty : variablePath;

            var format = match.Groups
                .Cast<Group>()
                .Skip(2) // Skip the first group (group 0 is the entire match)
                .LastOrDefault(group => group.Success && !string.IsNullOrEmpty(group.Value))
                ?.Value ?? string.Empty;
            
            var formatter = Formatters.Formatter.GetFormatter(format);
            string strValue = formatter.Format(value, format);
            
            if(encoder != null)
                strValue = encoder(strValue);
            
            if (cleanSpecialCharacters && variablePath != null
                                       && variablePath != "temp"
                                       && variablePath.StartsWith("file.") == false 
                                       && variablePath.StartsWith("folder.") == false)
            {
                // we dont want to replace user variables they set themselves, eg they may have set "DestPath" or something in the Function node
                // so we dont want to replace that, or any of the file/folder variables
                // but other nodes generate variables based on metadata, and that metadata may contain a /,\,: which would break a filename
                strValue = strValue.Replace(":/", "-");
                strValue = strValue.Replace(":\\", "-");
                strValue = strValue.Replace("/", "-");
                strValue = strValue.Replace("\\", "-");
                strValue = strValue.Replace(":", " - ");
            }

            // Replace the variable in the input
            if (string.IsNullOrWhiteSpace(strValue))
            {
                // Collapse spaces if the variable is replaced with an empty string
                input = input.Replace("(" + match.Value + ")", match.Value);
                input = input.Replace("[" + match.Value + "]", match.Value);
                input = Regex.Replace(input, $@"\s*{Regex.Escape(match.Value)}\s*", " ");
            }
            else
            {
                // Normal replacement without collapsing spaces
                input = input.Replace(match.Value, strValue.Trim());
            }
        }
    
        if (variables?.Any() == true)
        {
            foreach (string variable in variables.Keys)
            {
                string strValue = variables[variable]?.ToString() ?? "";
                if (cleanSpecialCharacters && variable.Contains('.') && variable.StartsWith("file.") == false && variable.StartsWith("folder.") == false)
                {
                    // we dont want to replace user variables they set themselves, eg they may have set "DestPath" or something in the Function node
                    // so we dont want to replace that, or any of the file/folder variables
                    // but other nodes generate variables based on metadata, and that metadata may contain a /,\,: which would break a filename
                    strValue = strValue.Replace("/", "-");
                    strValue = strValue.Replace("\\", "-");
                    strValue = strValue.Replace(":", " - ");
                }
                input = ReplaceVariable(input, variable, strValue);
            }
        }

        if (stripMissing)
            input = Regex.Replace(input, "{[a-zA-Z_][a-zA-Z0-9_\\.]*}", string.Empty);

        return input.Trim();
    }
    /// <summary>
    /// Resolves a nested property or key from an object, progressively checking parent keys in the dictionary.
    /// </summary>
    /// <param name="variables">A dictionary of variables to resolve the property from.</param>
    /// <param name="variablePath">The dotted path string (e.g., "a.b.c").</param>
    /// <returns>
    /// The value of the nested property if found, or <c>null</c> if the property does not exist.
    /// </returns>
    public static object? ResolveVariable(Dictionary<string, object> variables, string variablePath)
    {
        // Any additional changes here add to the ScribanRenderer
        if (variablePath == "time.processing" && StartedAt != null)
            return DateTime.Now.Subtract(StartedAt.Value).ToString();
        if (variablePath == "time.processingRaw" && StartedAt != null)
            return DateTime.Now.Subtract(StartedAt.Value);
        if (variablePath == "time.now")
            return DateTime.Now.ToShortTimeString();
        
        // Split the path into parts
        string[] parts = variablePath.Split('.');
    
        // Try to find the longest key in the dictionary (e.g., "a.b.c", then "a.b", etc.)
        for (int i = parts.Length; i > 0; i--)
        {
            string baseKey = string.Join(".", parts.Take(i));
        
            if (variables.TryGetValue(baseKey, out object? baseValue))
            {
                // Once the base object is found (e.g., "a" or "a.b"), resolve remaining parts
                string remainingPath = string.Join(".", parts.Skip(i));
                return ResolveNestedProperty(baseValue, remainingPath);
            }
        }

        // Return null if no base key was found
        return null;
    }
    
    /// <summary>
    /// Resolves a nested property or field on an object or JsonElement.
    /// </summary>
    /// <param name="obj">The base object or JsonElement.</param>
    /// <param name="propertyPath">The remaining property path to resolve (e.g., "b.c").</param>
    /// <returns>The resolved value or null if the path could not be resolved.</returns>
    public static object? ResolveNestedProperty(object? obj, string propertyPath)
    {
        if (string.IsNullOrEmpty(propertyPath))
            return obj;  // No more parts to resolve

        string[] parts = propertyPath.Split('.');
    
        foreach (string part in parts)
        {
            if (obj == null)
                return null;

            // Handle JsonElement
            if (obj is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(part, out JsonElement nestedElement))
                {
                    obj = nestedElement;
                }
                else
                {
                    return null;  // Property not found
                }
            }
            // Handle IDictionary
            else if (obj is IDictionary<string, object> dictionary)
            {
                if (dictionary.TryGetValue(part, out object? nestedValue))
                {
                    obj = nestedValue;
                }
                else
                {
                    return null;  // Key not found
                }
            }
            else
            {
                // Handle regular object properties via reflection
                Type objType = obj.GetType();
            
                PropertyInfo? prop = objType.GetProperty(part);
                FieldInfo? field = objType.GetField(part);
            
                if (prop != null)
                {
                    obj = prop.GetValue(obj);
                }
                else if (field != null)
                {
                    obj = field.GetValue(obj);
                }
                else
                {
                    return null;  // Property or field not found
                }
            }
        }

        return obj;
    }
    
    /// <summary>
    /// Replaces a variable in the string
    /// </summary>
    /// <param name="input">the input string</param>
    /// <param name="variable">the variable to replace</param>
    /// <param name="value">the new value of the variable</param>
    /// <returns>the input string with the variable replaced</returns>
    private static string ReplaceVariable(string input, string variable, string value)
    {
        var result = input;
        if(Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"}"))
            result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"}", value, RegexOptions.IgnoreCase);
        if (Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"!}"))
            result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"!}", value.ToUpper(), RegexOptions.IgnoreCase);

        return result;
    }

}
