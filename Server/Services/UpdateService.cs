using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service that monitors for updates
/// </summary>
public class UpdateService
{
    /// <summary>
    /// Gets the update information
    /// </summary>
    public UpdateInfo Info { get; private init; } = new UpdateInfo();

    /// <summary>
    /// Triggers a scan
    /// </summary>
    public async Task Trigger()
    {
        var repository = await ServiceLoader.Load<RepositoryService>().GetRepository();
        Info.FileFlowsVersion = await CheckForFileFlowsUpdate();
        if (Info.FileFlowsVersion != null)
            Info.ReleaseNotes = await GetReleaseNotes();
        Info.PluginUpdates = await CheckForPluginUpdates();
        Info.DockerModUpdates = repository == null ? [] : await CheckForDockerModUpdates(repository);
        Info.ScriptUpdates = repository == null ? [] : await CheckForScriptUpdates(repository);
    }

    /// <summary>
    /// Gets the release notes
    /// </summary>
    /// <returns>the release notes</returns>
    private async Task<List<ReleaseNotes>> GetReleaseNotes()
    {
        var response = await HttpHelper.Get<string>("https://fileflows.com/news/feed.json");
        if (response.Success == false || string.IsNullOrWhiteSpace(response.Body))
            return [];
        var releaseNotes = FileFlowsFeedParser.Parse(response.Body);
        var thisVersion = Version.Parse(Globals.Version);
        //return releaseNotes.Where(x => Version.Parse(x.Version + ".0") > thisVersion).ToList();
        return releaseNotes.Take(4).ToList();
    }

    /// <summary>
    /// Checks for a new FileFlows Version
    /// </summary>
    /// <returns>the latest FileFlows version if available, or a null string if non available</returns>
    private async Task<string?> CheckForFileFlowsUpdate()
    {
        try
        {
            var settings = await ServiceLoader.Load<ISettingsService>().Get();
            if (settings.DisableTelemetry)
                return null; 
            var result = ServerUpdater.Instance.GetLatestOnlineVersion();
            if (result.updateAvailable == false)
                return null;
            return result.onlineVersion.ToString();
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed checking latest version: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return null;
        }
    }

    /// <summary>
    /// Check for plugin updates
    /// </summary>
    /// <returns>the list of plugin updates</returns>
    public async Task<List<PackageUpdate>> CheckForPluginUpdates()
    {
        var service = ServiceLoader.Load<PluginService>();
        var plugins = await service.GetPluginInfoModels();
        List<PackageUpdate> updates = new();
        foreach (var plugin in plugins)
        {
            if (plugin.LatestVersion == null)
                continue;
            if (plugin.LatestVersion != plugin.Version)
            {
                updates.Add(new PackageUpdate
                {
                    Uid = plugin.Uid,
                    Name = plugin.Name,
                    Icon = plugin.Icon,
                    CurrentVersion = plugin.Version,
                    LatestVersion = plugin.LatestVersion
                });
            }
        }
        return updates;
    }
    
    /// <summary>
    /// Checks for DockerMod updates
    /// </summary>
    /// <param name="repository">the repository</param>
    /// <returns>any updates</returns>
    private async Task<List<PackageUpdate>> CheckForDockerModUpdates(FileFlowsRepository repository)
    {
        var service = ServiceLoader.Load<DockerModService>();
        var mods = await service.GetAll();
        List<PackageUpdate> updates = new();
        foreach (var mod in mods)
        {
            var repoMod = repository.DockerMods.FirstOrDefault(x => x.Uid == mod.Uid);
            if (repoMod == null)
                continue;
            if (mod.Revision >= repoMod.Revision)
                continue;
            if(repoMod.MinimumVersion != null && repoMod.MinimumVersion > Version.Parse(Globals.Version))
                continue;
            
            updates.Add(new PackageUpdate
            {
                Uid = repoMod.Uid!.Value,
                Name = mod.Name,
                Icon = repoMod.Icon,
                CurrentVersion = mod.Revision.ToString(),
                LatestVersion = repoMod.Revision.ToString()
            });
            
        }
        return updates;
    }

    /// <summary>
    /// Checks for Script updates
    /// </summary>
    /// <param name="repository">the repository</param>
    /// <returns>any updates</returns>
    private async Task<List<PackageUpdate>> CheckForScriptUpdates(FileFlowsRepository repository)
    {
        var service = ServiceLoader.Load<ScriptService>();
        var scripts = await service.GetAll();
        List<PackageUpdate> updates = new();
        foreach (var script in scripts)
        {
            if (script.Repository == false)
                continue;
            var repoScript = repository.SharedScripts.FirstOrDefault(x => x.Uid == script.Uid) ??
                             repository.FlowScripts.FirstOrDefault(x => x.Uid == script.Uid) ??
                             repository.SystemScripts.FirstOrDefault(x => x.Uid == script.Uid);
            if (repoScript == null)
                continue;
            if (script.Revision >= repoScript.Revision)
                continue;
            if(script.MinimumVersion != null && script.MinimumVersion > Version.Parse(Globals.Version))
                continue;
            
            updates.Add(new PackageUpdate
            {
                Uid = repoScript.Uid!.Value,
                Name = script.Name,
                Icon = repoScript.Icon,
                CurrentVersion = script.Revision.ToString(),
                LatestVersion = repoScript.Revision.ToString()
            });
            
        }
        return updates;
    }
}