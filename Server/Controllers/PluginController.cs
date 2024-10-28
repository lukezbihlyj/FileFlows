using System.Dynamic;
using System.Text.RegularExpressions;
using FileFlows.Server.Authentication;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Authorization;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Plugin Controller
/// </summary>
[Route("/api/plugin")]
[FileFlowsAuthorize(UserRole.Plugins)]
public class PluginController : BaseController
{
    /// <summary>
    /// Get the plugins translation file
    /// </summary>
    /// <param name="langCode">The language code to get the translations for</param>
    /// <returns>The json plugin translation file</returns>
    [HttpGet("language/{langCode}.json")]
    [AllowAnonymous]
    public IActionResult LanguageFile([FromRoute] string langCode = "en")
    {
        if(Regex.IsMatch(langCode, "^[a-zA-Z]{2,3}$") == false)
            return new JsonResult(new {});
        string file = $"i18n/plugins.{langCode}.json";
        if(System.IO.File.Exists(Path.Combine(_hostingEnvironment.WebRootPath, file)))
            return File(file, "text/json");
        return new JsonResult(new {});
    }
    
    /// <summary>
    /// Represents the hosting environment of the application.
    /// </summary>
    private readonly IWebHostEnvironment _hostingEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginController"/> class.
    /// </summary>
    /// <param name="hostingEnvironment">The hosting environment.</param>
    public PluginController(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
    }

    /// <summary>
    /// Get a list of all plugins in the system
    /// </summary>
    /// <param name="includeElements">If data should contain all the elements for the plugins</param>
    /// <returns>a list of plugins</returns>
    [HttpGet]
    public Task<IEnumerable<PluginInfoModel>> GetAll(bool includeElements = true)
        => ServiceLoader.Load<PluginService>().GetPluginInfoModels(includeElements);
    
    /// <summary>
    /// Get the plugin info for a specific plugin
    /// </summary>
    /// <param name="uid">The uid of the plugin</param>
    /// <returns>The plugin info for the plugin</returns>
    [HttpGet("{uid}")]
    public async Task<PluginInfo> Get([FromRoute] Guid uid)
        => await new Services.PluginService().GetByUid(uid) ?? new();

    /// <summary>
    /// Get the plugin info for a specific plugin by package name
    /// </summary>
    /// <param name="name">The package name of the plugin</param>
    /// <returns>The plugin info for the plugin</returns>
    [HttpGet("by-package-name/{name}")]
    public Task<PluginInfo?> GetByPackageName([FromRoute] string name)
        => new Services.PluginService().GetByPackageName(name);

    /// <summary>
    /// Get the available plugin packages 
    /// </summary>
    /// <param name="missing">If only missing plugins should be included, ie plugins not installed</param>
    /// <returns>a list of plugins</returns>
    [HttpGet("plugin-packages")]
    public async Task<IActionResult> GetPluginPackages([FromQuery] bool missing = false)
    {
        var result = await ServiceLoader.Load<PluginService>().GetPluginPackagesActual(missing);
        if (result.Failed(out string message))
            return BadRequest(message);
        return Ok(result.Value);
    }

    /// <summary>
    /// Download the latest updates for plugins from the Plugin Repository
    /// </summary>
    /// <param name="model">The list of plugins to update</param>
    /// <returns>if the updates were successful or not</returns>
    [HttpPost("update")]
    public async Task<bool> Update([FromBody] ReferenceModel<Guid> model)
    {
        bool updated = false;
        
        var pluginsResult = await ServiceLoader.Load<PluginService>().GetPluginPackagesActual();
        var plugins = pluginsResult.IsFailed ? new() : pluginsResult.Value;

        var pluginDownloader = new PluginDownloader();
        foreach (var uid in model?.Uids ?? new Guid[] { })
        {
            var plugin = await new Services.PluginService().GetByUid(uid);
            if (plugin == null)
                continue;

            var ppi = plugins.FirstOrDefault(x => x.Package.Replace(" ", "").ToLowerInvariant() 
                                                  == plugin.PackageName.Replace(" ", "").ToLowerInvariant());

            if (ppi == null)
            {
                Logger.Instance.WLog("PluginUpdate: No plugin info found for plugin: " + plugin.Name);
                continue;
            }
            if(string.IsNullOrEmpty(ppi.Package))
            {
                Logger.Instance.WLog("PluginUpdate: No plugin info did not contain Package name for plugin: " + plugin.Name);
                continue;
            }

            if (Version.Parse(ppi.Version) <= Version.Parse(plugin.Version))
            {
                // no new version, cannot update
                Logger.Instance.WLog("PluginUpdate: No newer version to download for plugin: " + plugin.Name);
                continue;
            }

            var dlResult = await pluginDownloader.Download(Version.Parse(ppi.Version), ppi.Package);
            if (dlResult.Success == false)
            {
                Logger.Instance.WLog("PluginUpdate: Failed to download plugin");
                continue;
            }

            // save the ffplugin file
            bool success = PluginScanner.UpdatePlugin(ppi.Package, dlResult.Data);
            if(success)
                Logger.Instance.ILog("PluginUpdate: Successfully updated plugin: " + plugin.Name);
            else
                Logger.Instance.WLog("PluginUpdate: Failed to updated plugin: " + plugin.Name);

            updated |= success;
        }
        
        await ServiceLoader.Load<UpdateService>().Trigger();
        return updated;
    }

    /// <summary>
    /// Delete plugins from the system
    /// </summary>
    /// <param name="model">A reference model containing UIDs to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete]
    public async Task Delete([FromBody] ReferenceModel<Guid> model)
        => await new PluginService().Delete(model.Uids, await GetAuditDetails());

    /// <summary>
    /// Download plugins into the FileFlows system
    /// </summary>
    /// <param name="model">A list of plugins to download</param>
    /// <returns>an awaited task</returns>
    [HttpPost("download")]
    public async Task Download([FromBody] DownloadModel model)
    {
        if (model == null || model.Packages?.Any() != true)
            return; // nothing to delete

        await ServiceLoader.Load<PluginService>().DownloadPlugins(model.Packages);
    }

    /// <summary>
    /// Download the plugin ffplugin file.  Only intended to be used by the FlowRunner
    /// </summary>
    /// <param name="package">The plugin package name to download</param>
    /// <returns>A download stream of the ffplugin file</returns>
    [HttpGet("download-package/{package}")]
    public FileStreamResult DownloadPackage([FromRoute] string package)
    {
        if (string.IsNullOrEmpty(package))
        {
            Logger.Instance?.ELog("Download Package Error: package not set");
            throw new ArgumentNullException(nameof(package));
        }
        if (package.EndsWith(".ffplugin") == false)
            package += ".ffplugin";

        if(Regex.IsMatch(package, "^[a-zA-Z0-9_\\-]+\\.ffplugin$") == false)
        {
            Logger.Instance?.ELog("Download Package Error: invalid package: " + package);
            throw new Exception("Download Package Error: invalid package: " + package);
        }

        string dir = PluginScanner.GetPluginDirectory();
        string file = Path.Combine(dir, package);

        if (System.IO.File.Exists(file) == false)
        {
            Logger.Instance?.ELog("Download Package Error: File not found => " + file);
            throw new Exception("File not found");
        }

        try
        {
            return File(FileOpenHelper.OpenRead_NoLocks(file), "application/octet-stream");
        }
        catch(Exception ex)
        {
            Logger.Instance?.ELog("Download Package Error: Failed to read data => " + ex.Message); ;
            throw;
        }
    }

    /// <summary>
    /// Gets the json plugin settings for a plugin
    /// </summary>
    /// <param name="packageName">The full plugin name</param>
    /// <returns>the plugin settings json</returns>
    [HttpGet("{packageName}/settings")]
    public Task<string> GetPluginSettings([FromRoute] string packageName)
        => new PluginService().GetSettingsJson(packageName);

    /// <summary>
    /// Sets the json plugin settings for a plugin
    /// </summary>
    /// <param name="packageName">The full plugin name</param>
    /// <param name="json">the settings json</param>
    /// <returns>an awaited task</returns>
    [HttpPost("{packageName}/settings")]
    public async Task SetPluginSettingsJson([FromRoute] string packageName, [FromBody] string json)
    {
        // need to encrypt any passwords
        if (string.IsNullOrEmpty(json) == false)
        {
            try
            {
                var plugin = await GetByPackageName(packageName);
                if (string.IsNullOrEmpty(plugin?.Name) == false)
                {
                    bool updated = false;

                    IDictionary<string, object> dict = JsonSerializer.Deserialize<ExpandoObject>(json) as IDictionary<string, object> ?? new Dictionary<string, object>();
                    foreach (var key in dict.Keys.ToArray())
                    {
                        if (plugin.Settings.Any(x => x.Name == key && x.InputType == Plugin.FormInputType.Password))
                        {
                            // its a password, decrypt 
                            string text = string.Empty;
                            if (dict[key] is JsonElement je)
                            {
                                text = je.GetString() ?? String.Empty;
                            }
                            else if (dict[key] is string str)
                            {
                                text = str;
                            }

                            if (string.IsNullOrEmpty(text))
                                continue;

                            dict[key] = DataLayer.Helpers.Decrypter.Encrypt(text);
                            updated = true;
                        }
                    }
                    if (updated)
                        json = JsonSerializer.Serialize(dict);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog("Failed to encrypting passwords in plugin settings: " + ex.Message);
            }
        }

        var service = ServiceLoader.Load<PluginService>();
        var oldSettings = await service.GetSettingsJson(packageName);
        var newJson = json ?? string.Empty;
        if (newJson == oldSettings)
            return;
        await service.SetSettingsJson(packageName, newJson, await GetAuditDetails());
        await ((SettingsService)ServiceLoader.Load<ISettingsService>()).RevisionIncrement();
    }

    
    /// <summary>
    /// Set state of a processing node
    /// </summary>
    /// <param name="uid">The UID of the processing node</param>
    /// <param name="enable">Whether or not this node is enabled and will process files</param>
    /// <returns>an awaited task</returns>
    [HttpPut("state/{uid}")]
    public async Task<PluginInfo> SetState([FromRoute] Guid uid, [FromQuery] bool? enable)
    {
        var service = new Services.PluginService();
        var plugin = await service.GetByUid(uid);
        if (plugin == null)
            throw new Exception("Node not found.");
        if (enable != null && plugin.Enabled != enable.Value)
        {
            plugin.Enabled = enable.Value;
            await service.Update(plugin, await GetAuditDetails());
        }

        return plugin;
    }
    
    /// <summary>
    /// Download model
    /// </summary>
    public class DownloadModel
    {
        /// <summary>
        /// A list of plugin packages to download
        /// </summary>
        public List<PluginPackageInfo> Packages { get; set; }
    }
}
