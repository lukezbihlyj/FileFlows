using FileFlows.Managers.InitializationManagers;
using FileFlows.Plugin;
using FileFlows.Server.Services;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Validates the database and
/// </summary>
/// <param name="logger">the logger to use</param>
/// <param name="settingsService">the settings service</param>
/// <param name="upgradeManager">the upgrade manager</param>
public class DatabaseValidator(FileFlows.Plugin.ILogger logger, AppSettingsService settingsService, UpgradeManager upgradeManager)
    : UpgradeBase(logger, settingsService, upgradeManager)
{

    /// <summary>
    /// Ensures all the required columns exist
    /// </summary>
    /// <returns>the result of the upgrade</returns>
    public async Task<Result<bool>> EnsureColumnsExist()
        => await UpgradeManager.EnsureColumnsExist(Logger, DbType, ConnectionString);
}