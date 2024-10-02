using System.Text.RegularExpressions;
using FileFlows.Server.Helpers;

namespace FileFlows.Server.Services;

/// <summary>
/// Language Service
/// </summary>
public class LanguageService
{
    /// <summary>
    /// Initializes the language service
    /// </summary>
    public async Task Initialize()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get();
        var json = await GetLanguageJson(settings.Language?.EmptyAsNull() ?? "en");
        Translater.Init(json);
    }

    /// <summary>
    /// Gets the complete JSON language
    /// </summary>
    /// <param name="language">the langauge to get</param>
    /// <returns>the complete JSON</returns>
    public async Task<string> GetLanguageJson(string language)
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get();
        string langCode = language?.EmptyAsNull() ?? settings.Language?.EmptyAsNull() ?? "en";
        if (Regex.IsMatch(langCode, "^[a-z]{2,3}$") == false)
            langCode = "en";
        
        List<string> files = new();

        string wwwRootLang = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "wwwroot", "i18n");
        #if(DEBUG)
        var dllDir =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        dllDir = dllDir[..(dllDir.IndexOf("Server", StringComparison.Ordinal))];
        var mainLangPath = Path.Combine(dllDir, "Client", "wwwroot", "i18n");
        wwwRootLang = Path.Combine(dllDir, "Server", "wwwroot", "i18n");
        #else
        var mainLangPath = wwwRootLang;
        #endif
        
        if(System.IO.File.Exists(Path.Combine(mainLangPath, $"{langCode}.json")))
            files.Add(Path.Combine(mainLangPath, $"{langCode}.json"));
        else if(System.IO.File.Exists(Path.Combine(mainLangPath, "en.json")))
            files.Add(Path.Combine(mainLangPath, "en.json")); // fall back to english
        
        if(System.IO.File.Exists(Path.Combine(wwwRootLang, $"plugins.{langCode}.json")))
            files.Add(Path.Combine(wwwRootLang, $"plugins.{langCode}.json"));
        else if(System.IO.File.Exists(Path.Combine(wwwRootLang, "plugins.en.json")))
            files.Add(Path.Combine(wwwRootLang, "plugins.en.json")); // fall back to english
            
        if(files.Count == 0)
            return "{}";
        
        if(files.Count == 1)
            return await File.ReadAllTextAsync(files[0]);

        string json1 = await File.ReadAllTextAsync(files[0]);
        string json2 = await File.ReadAllTextAsync(files[1]);

        if (string.IsNullOrWhiteSpace(json1) && string.IsNullOrWhiteSpace(json2))
            return "{}";
        if(string.IsNullOrWhiteSpace(json1))
            return json2;
        if(string.IsNullOrWhiteSpace(json2))
            return json1;

        var merged = JsonHelper.Merge(json1, json2);
        return merged;
    }
}