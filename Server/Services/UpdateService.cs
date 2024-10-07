using FileFlows.Server.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service that monitors for updates
/// </summary>
public class UpdateService : BackgroundService
{
    /// <summary>
    /// Gets the update information
    /// </summary>
    public UpdateInfo Info { get; private init; } = new UpdateInfo();

    /// <summary>
    /// Constructs a new instance of the service
    /// </summary>
    public UpdateService()
    {
        // Trigger on startup
        _ = Trigger();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Runs the service until the app is stopped.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Do the work you need to do every minute
                await Trigger();

                // Wait for 1 hour, or until cancellation is requested
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                // Handle any exceptions (log or retry logic)
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Triggers a scan
    /// </summary>
    public async Task Trigger()
    {
        var repository = await ServiceLoader.Load<RepositoryService>().GetRepository();
        Info.FileFlowsVersion = await CheckForFileFlowsUpdate();
        Info.PluginUpdates = await CheckForPluginUpdates();
        Info.DockerModUpdates = repository == null ? [] : await CheckForDockerModUpdates(repository);
        Info.ScriptUpdates = repository == null ? [] : await CheckForScriptUpdates(repository);
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
            if (mod.Revision == repoMod.Revision)
                continue;
            
            updates.Add(new PackageUpdate
            {
                Name = mod.Name,
                Icon = repoMod.Icon,
                CurrentVersion = repoMod.Revision.ToString(),
                LatestVersion = mod.Revision.ToString()
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
            if (script.Revision == repoScript.Revision)
                continue;
            
            updates.Add(new PackageUpdate
            {
                Name = script.Name,
                Icon = repoScript.Icon,
                CurrentVersion = repoScript.Revision.ToString(),
                LatestVersion = script.Revision.ToString()
            });
            
        }
        return updates;
    }
}