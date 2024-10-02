using Jeffijoe.MessageFormat;
using System.Text.Json;
using System.Dynamic;
using System.Text.Encodings.Web;

namespace FileFlows.Shared;

/// <summary>
/// Translater is responsible for language translations
/// </summary>
public class Translater
{
    /// <summary>
    /// Formatter for formatting messages.
    /// </summary>
    private static MessageFormatter Formatter;

    /// <summary>
    /// Dictionary containing language translations.
    /// </summary>
    private static Dictionary<string, string> Language { get; set; } = new ();

    /// <summary>
    /// Regular expression to check if a string needs translating.
    /// </summary>
    private static Regex rgxNeedsTranslating = new (@"^([\w\d_\-]+\.)+[\w\d_\-]+$");


    /// <summary>
    /// Translates a string if the string needs translating
    /// </summary>
    /// <param name="value">The string to translate if needed</param>
    /// <returns>The translated string if needed, otherwise the original string</returns>
    public static string TranslateIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (NeedsTranslating(value) == false)
            return value;
        return Instant(value);
    }

    /// <summary>
    /// Gets if the translator has been initialized
    /// </summary>
    public static bool InitDone => Formatter != null;

    /// <summary>
    /// Checks if a string needs translating
    /// </summary>
    /// <param name="label">The string to test</param>
    /// <returns>if the string needs to be translated or not</returns>
    public static bool NeedsTranslating(string label) => rgxNeedsTranslating.IsMatch(label ?? "");
    
    /// <summary>
    /// Initializes the translator
    /// </summary>
    /// <param name="json">the language JSON</param>
    public static void Init(string json)
    {
        Formatter ??= new MessageFormatter();
        Language = DeserializeAndFlatten(json);
        Logger.Instance.DLog("Language keys found: " + Language.Keys.Count);
    }
    
    /// <summary>
    /// Deserialized a json string and flattens it into a Dictionary using dot notation
    /// </summary>
    /// <param name="json">The json string to flatten</param>
    /// <returns>a dictionary of the flatten json string</returns>
    public static Dictionary<string, string> DeserializeAndFlatten(string json)
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            Converters = { new LanguageConverter() }
        };
        dynamic d = JsonSerializer.Deserialize<ExpandoObject>(json, options) ?? throw new InvalidOperationException();
        FillDictionaryFromExpando(dict, d, "");
        return dict;
    }

    /// <summary>
    /// Fills a dictoinary from an expando object
    /// </summary>
    /// <param name="dict">the dictionary to fill</param>
    /// <param name="expando">the expando to use</param>
    /// <param name="prefix">the prefix for any newly added keys</param>
    private static void FillDictionaryFromExpando(Dictionary<string, string> dict, ExpandoObject expando, string prefix)
    {
        IDictionary<string, object> dictExpando = expando as IDictionary<string, object>;
        foreach (string key in dictExpando.Keys)
        {
            if (dictExpando[key] is ExpandoObject eo)
            {
                FillDictionaryFromExpando(dict, eo, Join(prefix, key));
            }
            else if (dictExpando[key] is string str)
                dict.Add(Join(prefix, key), str);

        }
    }
    /// <summary>
    /// Joins a string
    /// </summary>
    /// <param name="prefix">the prefix, coudl be empty</param>
    /// <param name="name">the name</param>
    /// <returns>the joined string</returns>
    private static string Join(string prefix, string name)
        => (string.IsNullOrEmpty(prefix) ? name : prefix + "." + name);

    /// <summary>
    /// Looks up a translation for a list of possible keys.
    /// </summary>
    /// <param name="possibleKeys">The possible keys to look up</param>
    /// <param name="supressWarnings">If warnings should be suppressed</param>
    /// <returns>The translation if found, otherwise the first key</returns>
    private static string Lookup(string[] possibleKeys, bool supressWarnings = false)
    {
        foreach (var key in possibleKeys)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;
            if (Language.TryGetValue(key, out var lookup))
                return lookup;
        }
        if (possibleKeys[0].EndsWith("-Help") || possibleKeys[0].EndsWith("-Placeholder") || possibleKeys[0].EndsWith("-Suffix") || possibleKeys[0].EndsWith("-Prefix") || possibleKeys[0].EndsWith(".Description"))
            return "";

        if (possibleKeys[0].EndsWith(".Name") && Language.ContainsKey("Labels.Name"))
            return Language["Labels.Name"];

        string result = possibleKeys?.FirstOrDefault() ?? "";
        if(supressWarnings == false && result.EndsWith(".UID") == false && result.StartsWith("Flow.Parts.") == false)
            Logger.Instance.WLog("Failed to lookup key: " + result);
        result = result[(result.LastIndexOf(".", StringComparison.Ordinal) + 1)..];

        return result;
    }

    /// <summary>
    /// Translates a string
    /// </summary>
    /// <param name="key">The string to translate</param>
    /// <param name="parameters">any translation parameters</param>
    /// <param name="suppressWarnings">if translation warnings should be suppressed and not printed to the log</param>
    /// <returns>the translated string</returns>
    public static string Instant(string key, object parameters = null, bool suppressWarnings = false)
        => Instant(new[] { key }, parameters, suppressWarnings: suppressWarnings);

    /// <summary>
    /// Attempts to translate from a range of possible keys.
    /// The first key found in the translation dictionary will be returned
    /// </summary>
    /// <param name="possibleKeys">a list of possible translation keys</param>
    /// <param name="parameters">any translation parameters</param>
    /// <param name="suppressWarnings">if translation warnings should be suppressed and not printed to the log</param>
    /// <returns>the translated string</returns>
    public static string Instant(string[] possibleKeys, object parameters = null, bool suppressWarnings = false)
    {
        try
        {
            string msg = Lookup(possibleKeys, supressWarnings: suppressWarnings);
            if (msg == "")
                return "";
            if (parameters is IDictionary<string, object> dict)
                return Formatter.FormatMessage(msg, dict);

            return Formatter.FormatMessage(msg, parameters ?? new { });
        }
        catch (Exception ex)
        {
            if (possibleKeys[0].EndsWith(".UID"))
                return "UID";
            if(suppressWarnings == false)
                Logger.Instance.WLog("Failed to translating key: " + possibleKeys[0] + ", " + ex.Message);
            return possibleKeys[0];
        }
    }

    /// <summary>
    /// Translates a string if there is a translation for it
    /// </summary>
    /// <param name="key">the key to translate</param>
    /// <param name="default">the default string to return if no translation is found</param>
    /// <returns>the translated string or default if not found</returns>
    public static string TranslateIfHasTranslation(string key, string @default)
    {
        try
        {
            if (Language.ContainsKey(key) == false)
                return @default;
            string msg = Language[key];
            return Formatter.FormatMessage(msg, new { });
        }
        catch (Exception)
        {
            return @default;
        }
    }

    /// <summary>
    /// Checks if a string can be translated
    /// </summary>
    /// <param name="key">the key to translate</param>
    /// <param name="translation">the translation if succeeded</param>
    /// <returns>true if can translate</returns>
    public static bool CanTranslate(string key, out string translation)
    {
        translation = string.Empty;
        try
        {
            if (Language.ContainsKey(key) == false)
                return false;
            string msg = Language[key];
            translation = Formatter.FormatMessage(msg, new { });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    
    /// <summary>
    /// Reorders a language JSON file so that keys are sorted alphabetically.
    /// Keys with children appear below keys that are just string values.
    /// </summary>
    /// <param name="json">The JSON string to reorder</param>
    /// <returns>The reordered JSON string</returns>
    public static string ReorderJson(string json)
    {
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        var orderedDictionary = OrderDictionary(dictionary);
        return JsonSerializer.Serialize(orderedDictionary, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Prevents escaping Unicode characters
        });
    }
    
    /// <summary>
    /// Orders a dictionary so that keys are sorted alphabetically.
    /// Keys with string values appear first, followed by keys with nested dictionaries.
    /// The group "Labels" will appear before any other group.
    /// </summary>
    /// <param name="dictionary">The dictionary to order</param>
    /// <returns>The ordered dictionary</returns>
    private static Dictionary<string, object> OrderDictionary(Dictionary<string, object> dictionary)
    {
        var ordered = new Dictionary<string, object>();

        // Add string values next
        foreach (var kvp in dictionary.Where(kvp => kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.String).OrderBy(kvp => kvp.Key))
        {
            ordered[kvp.Key] = kvp.Value;
        }
        
        // Add "Labels" group first if it exists
        if (dictionary.TryGetValue("Labels", out var labelsValue) && labelsValue is JsonElement labelsElement && labelsElement.ValueKind == JsonValueKind.Object)
        {
            var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(labelsElement.GetRawText());
            ordered["Labels"] = OrderDictionary(nestedDict);
        }

        // Add other nested dictionaries
        foreach (var kvp in dictionary.Where(kvp => kvp.Key != "Labels" && kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Object).OrderBy(kvp => kvp.Key))
        {
            var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(kvp.Value.ToString());
            ordered[kvp.Key] = OrderDictionary(nestedDict);
        }

        return ordered;
    }
}