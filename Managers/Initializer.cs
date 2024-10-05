using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;

namespace FileFlows.Managers;

/// <summary>
/// Initializes any manager settings
/// </summary>
public class Initializer
{
    // may move this
    public static Result<bool> Init(ILogger logger,DatabaseType dbType, string connectionString, string encryptionKey)
    {
        DataLayer.Helpers.Decrypter.EncryptionKey = encryptionKey;
        var dbLogger = new Logger();
        dbLogger.RegisterWriter(new FileLogger(DirectoryHelper.LoggingDirectory, "Database", false));
        return DatabaseAccessManager.Initialize(dbLogger, dbType, connectionString);
    }
}