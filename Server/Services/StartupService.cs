using FileFlows.Managers.InitializationManagers;
using FileFlows.Node.Workers;
using FileFlows.Plugin;
using FileFlows.RemoteServices;
using FileFlows.Server.DefaultTemplates;
using FileFlows.Server.Helpers;
using FileFlows.Server.Workers;
using FileFlows.ServerShared.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Service used for status up work
/// All startup code, db initialization, migrations, upgrades should be done here,
/// so the UI apps can be shown with a status of what is happening
/// </summary>
public class StartupService
{
    /// <summary>
    /// A delegate that is used when there is a status update
    /// </summary>
    /// <param name="status">the status</param>
    /// <param name="subStatus">the sub status</param>
    /// <param name="details">any extra details for this statue, ie a log</param>
    public delegate void StartupStatusEvent(string status, string subStatus, string details);
    
    /// <summary>
    /// An event that is called when there is status update
    /// </summary>
    public event StartupStatusEvent OnStatusUpdate;
    
    /// <summary>
    /// Gets the current status
    /// </summary>
    public string CurrentStatus { get; private set; }


    private AppSettingsService appSettingsService;

    /// <summary>
    /// Run the startup commands
    /// </summary>
    public Result<bool> Run(string serverUrl)
    {
        UpdateStatus("Starting...");
        try
        {
            appSettingsService = ServiceLoader.Load<AppSettingsService>();

            string error;

            CheckLicense();

            CleanDefaultTempDirectory();

            BackupSqlite();

            if (CanConnectToDatabase().Failed(out error))
            {
                error = "Database Connection Error: " + error;
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }
            
            if (DatabaseExists()) // only upgrade if it does exist
            {
                if (Upgrade().Failed(out error))
                {
                    error = "Database Upgrade Error: " + error;
                    UpdateStatus(error);
                    return Result<bool>.Fail(error);
                }
            }
            else if (CreateDatabase().Failed(out error))
            {
                error = "Create Database Error: " + error;
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            if (PrepareDatabase().Failed(out error))
            {
                error = "Prepare Database Error: " + error;
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }

            if (SetVersion().Failed(out error))
            {
                error = "Set Version Error: " + error;
                UpdateStatus(error);
                return Result<bool>.Fail(error);
            }
        
            UpdateStatus("Updating Templates...");
            UpdateTemplates();
            // do this so the settings object is loaded
            var settings = ServiceLoader.Load<ISettingsService>().Get().Result;
            var appSettings = ServiceLoader.Load<AppSettingsService>().Settings;


            if (Globals.IsDocker && appSettings.DockerModsOnServer)
                RunnerDockerMods();

            ScanForPlugins();

            ServiceLoader.Load<LanguageService>().Initialize().Wait();

            LibraryWorker.ResetProcessing(internalOnly: true);

            DataLayerDelegates.Setup();
            
            Complete(settings, serverUrl);

            // Start workers right at the end, so the ServerUrl is set in case the worker needs BaseServerUrl
            StartupWorkers();
            
            WebServer.FullyStarted = true;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Startup failure: " + ex.Message + Environment.NewLine + ex.StackTrace);
            #if(DEBUG)
            UpdateStatus("Startup failure: " + ex.Message + Environment.NewLine + ex.StackTrace);
            #else
            UpdateStatus("Startup failure: " + ex.Message);
            #endif
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Final startup code
    /// </summary>
    /// <param name="settings">the settings</param>
    /// <param name="serverUrl">the server URL</param>
    private void Complete(Settings settings, string serverUrl)
    {
        string protocol = serverUrl[..serverUrl.IndexOf(":", StringComparison.Ordinal)];

        Application.ServerUrl = $"{protocol}://localhost:{WebServer.Port}";
        // update the client with the proper ServiceBaseUrl
        Shared.Helpers.HttpHelper.Client =
            Shared.Helpers.HttpHelper.GetDefaultHttpClient(Application.ServerUrl);

        RemoteService.ServiceBaseUrl = Application.ServerUrl;
        RemoteService.AccessToken = settings.AccessToken;
        RemoteService.NodeUid = Application.RunningUid;

        WebServer.FullyStarted = true;
    }

    /// <summary>
    /// Scans for plugins
    /// </summary>
    private void ScanForPlugins()
    {
        UpdateStatus("Scanning for Plugins");
        // need to scan for plugins before initing the translater as that depends on the plugins directory
        PluginScanner.Scan();
    }

    /// <summary>
    /// Starts the workers
    /// </summary>
    private void StartupWorkers()
    {
        UpdateStatus("Starting Workers");
        WorkerManager.StartWorkers(
            new StartupWorker(),
            new LicenseValidatorWorker(),
            new SystemMonitor(),
            new LibraryWorker(),
            new LogFileCleaner(),
            new FlowWorker(string.Empty, isServer: true),
            new ConfigCleaner(),
            new PluginUpdaterWorker(),
            new LibraryFileLogPruner(),
            new LogConverter(),
            new TelemetryReporter(),
            new ServerUpdater(),
            new TempFileCleaner(string.Empty),
            new FlowRunnerMonitor(),
            new ObjectReferenceUpdater(),
            new FileFlowsTasksWorker(),
            new RepositoryUpdaterWorker(),
            new ScheduledReportWorker(),
            new StatisticSyncer(),
            new UpdateWorker()
            //new LibraryFileServiceUpdater()
        );
    }

    /// <summary>
    /// Looks for a DockerMods output file and if found, logs its contents
    /// </summary>
    private void RunnerDockerMods()
    {
        UpdateStatus("Running DockerMods");
        var mods = ServiceLoader.Load<DockerModService>().GetAll().Result.Where(x => x.Enabled).ToList();
        foreach (var mod in mods)
        {
            UpdateStatus("Running DockerMods", mod.Name);
            DockerModHelper.Execute(mod, outputCallback: (output) =>
            {
                UpdateStatus("Running DockerMods", mod.Name, output);
            }).Wait();
        }

        // var output = Path.Combine(DirectoryHelper.DockerModsDirectory, "output.log");
        // if (File.Exists(output) == false)
        //     return;
        // var content = File.ReadAllText(output);
        // Logger.Instance.ILog("DockerMods: \n" + content);
        // File.Delete(output);
    }

    /// <summary>
    /// Tests a connection to a database
    /// </summary>
    private Result<bool> CanConnectToDatabase()
        => MigrationManager.CanConnect(appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Checks if the database exists
    /// </summary>
    /// <returns>true if it exists, otherwise false</returns>
    private Result<bool> DatabaseExists()
        => MigrationManager.DatabaseExists(appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Creates the database
    /// </summary>
    /// <returns>true if it exists, otherwise false</returns>
    private Result<bool> CreateDatabase()
        => MigrationManager.CreateDatabase(Logger.Instance, appSettingsService.Settings.DatabaseType,
            appSettingsService.Settings.DatabaseConnection);

    /// <summary>
    /// Backups the database file if using SQLite and not migrating
    /// </summary>
    private void BackupSqlite()
    {
        if (appSettingsService.Settings.DatabaseType is DatabaseType.Sqlite or DatabaseType.SqliteNewConnection == false)
            return;
        if (appSettingsService.Settings.DatabaseMigrateType != null)
            return;
        try
        {
            string dbfile = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");
            if (File.Exists(dbfile))
                File.Copy(dbfile, dbfile + ".backup", true);
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to backup SQLite database file: " + ex.Message);
        }
    }

    /// <summary>
    /// Sends a message update
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="subStatus">sub status</param>
    /// <param name="details">additional details</param>
    void UpdateStatus(string message, string subStatus = null, string details = null)
    {
        Logger.Instance.ILog(message);
        CurrentStatus = message;
        OnStatusUpdate?.Invoke(message, subStatus, details);
    }
    

    /// <summary>
    /// Checks the license key
    /// </summary>
    void CheckLicense()
    {
        LicenseHelper.Update().Wait();
    }
    
    /// <summary>
    /// Clean the default temp directory on startup
    /// </summary>
    private void CleanDefaultTempDirectory()
    {
        UpdateStatus("Cleaning temporary directory");
        
        string tempDir = Application.Docker
            ? Path.Combine(DirectoryHelper.DataDirectory, "temp") // legacy reasons docker uses lowercase temp
            : Path.Combine(DirectoryHelper.BaseDirectory, "Temp");
        DirectoryHelper.CleanDirectory(tempDir);
    }

    /// <summary>
    /// Runs an upgrade
    /// </summary>
    /// <returns>the upgrade result</returns>
    Result<bool> Upgrade()
    {
        string error;
        var upgrader = new Upgrade.Upgrader();
        var upgradeRequired = upgrader.UpgradeRequired(appSettingsService.Settings);
        if (upgradeRequired.Failed(out error))
            return Result<bool>.Fail(error);

        bool needsUpgrade = upgradeRequired.Value.Required;

        if (needsUpgrade == false)
        {
            upgrader.EnsureColumnsExist(appSettingsService);
            return true;
        }


        UpdateStatus("Backing up old database...");
        upgrader.Backup(upgradeRequired.Value.Current, appSettingsService.Settings, (details) =>
        {
            UpdateStatus("Backing up old database...", details);
        });
        
        UpdateStatus("Upgrading Please Wait...");
        var upgradeResult = upgrader.Run(upgradeRequired.Value.Current, appSettingsService,(details) =>
        {
            UpdateStatus("Upgrading Please Wait...", details);
        });
        if(upgradeResult.Failed(out error))
            return Result<bool>.Fail(error);
        
        
        return true;
    }

    /// <summary>
    /// Updates the templates
    /// </summary>
    private void UpdateTemplates()
    {
        var libraryTemplates = TemplateLoader.GetLibraryTemplates();
        if (libraryTemplates?.Any() == true)
        {
            Logger.Instance.ILog("Extracting Library Templates");
            foreach (var template in libraryTemplates)
            {
                Logger.Instance.ILog("Embedded Library Template: " + GetShortName(template));
                TemplateLoader.ExtractTo(template, DirectoryHelper.TemplateDirectoryLibrary);
            }
        }
        else
        {
            Logger.Instance.WLog("No embedded library templates found");
        }

        // delete the old flow template files
        var oldFlowTemplates = Directory.GetFiles(DirectoryHelper.TemplateDirectoryFlow, "Templates_*.json");
        foreach(var file in oldFlowTemplates)
             File.Delete(file);
        
        var flowTemplates = TemplateLoader.GetFlowTemplates();
        if (flowTemplates?.Any() == true)
        {
            Logger.Instance.ILog("Extracting Flow Templates");
            foreach (var template in flowTemplates)
            {
                Logger.Instance.ILog("Embedded Flow Template: " + GetShortName(template));
                TemplateLoader.ExtractTo(template, DirectoryHelper.TemplateDirectoryFlow);
            }
        }
        else
        {
            Logger.Instance.WLog("No embedded flow templates found");
        }

        string GetShortName(string path)
        {
            int index = path.IndexOf("DefaultTemplates.");
            return path[(index + "DefaultTemplates.".Length)..];
        }
    }

    /// <summary>
    /// Prepares the database
    /// </summary>
    /// <returns>the result</returns>
    Result<bool>PrepareDatabase()
    {
        UpdateStatus("Initializing database...");

        string error;
        
        var service = ServiceLoader.Load<DatabaseService>();

        if (service.MigrateRequired())
        {
            UpdateStatus("Migrating database, please wait this may take a while.");
            if (service.MigrateDatabase().Failed(out error))
                return Result<bool>.Fail(error);
        }
        
        if (service.PrepareDatabase().Failed(out error))
            return Result<bool>.Fail(error);

        return true;
    }


    /// <summary>
    /// Sets the version in the database
    /// </summary>
    /// <returns>true if successful</returns>
    private Result<bool> SetVersion()
    {
        try
        {
            var service = ServiceLoader.Load<DatabaseService>();
            service.SetVersion().Wait();
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

}