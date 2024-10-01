using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Language controller
/// </summary>
/// <param name="_hostingEnvironment">the hosting environment</param>
[Route("/api/language")]
public class LanguageController(IWebHostEnvironment _hostingEnvironment) : Controller
{
    /// <summary>
    /// Gets the language file
    /// </summary>
    /// <returns>the language JSON</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await ServiceLoader.Load<ISettingsService>().Get();
        string langCode = settings.Language?.EmptyAsNull() ?? "en";
        List<string> files = new();
        
        #if(DEBUG)
        var dllDir =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        dllDir = dllDir[..(dllDir.IndexOf("Server", StringComparison.Ordinal))];
        var mainLangPath = Path.Combine(dllDir, "Client", "wwwroot", "i18n");
        #else
        var mainLangPath = Path.Combine(_hostingEnvironment.WebRootPath,"i18n");
        #endif
        
        if(System.IO.File.Exists(Path.Combine(mainLangPath, $"{langCode}.json")))
            files.Add(Path.Combine(mainLangPath, $"{langCode}.json"));
        else if(System.IO.File.Exists(Path.Combine(mainLangPath, "en.json")))
            files.Add(Path.Combine(mainLangPath, "en.json")); // fall back to english
        
        string pluginLangPath = Path.Combine(_hostingEnvironment.WebRootPath,"i18n");
        if(System.IO.File.Exists(Path.Combine(pluginLangPath, $"plugins.{langCode}.json")))
            files.Add(Path.Combine(pluginLangPath, $"plugins.{langCode}.json"));
        else if(System.IO.File.Exists(Path.Combine(pluginLangPath, "plugins.en.json")))
            files.Add(Path.Combine(pluginLangPath, "plugins.en.json")); // fall back to english
            
        if(files.Count == 0)
            return new JsonResult(new {});
        
        if(files.Count == 1)
        {
            var json = await System.IO.File.ReadAllTextAsync(files[0]);
            return Content(json, "application/json");
        }

        string json1 = await System.IO.File.ReadAllTextAsync(files[0]);
        string json2 = await System.IO.File.ReadAllTextAsync(files[1]);
        
        if(string.IsNullOrWhiteSpace(json1) && string.IsNullOrWhiteSpace(json2))
            return new JsonResult(new {});
        if(string.IsNullOrWhiteSpace(json1))
            return Content(json2, "application/json");
        if(string.IsNullOrWhiteSpace(json2))
            return Content(json1, "application/json");

        var merged = JsonHelper.Merge(json1, json2);
        return Content(merged, "application/json");
    }
}