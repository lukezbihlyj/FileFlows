using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;
using FileFlows.ServerShared.Models;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

/// <summary>
/// Migrates one database to another database
/// </summary>
public class DbMigrator
{
    /// <summary>
    /// The logger to use
    /// </summary>
    private readonly ILogger Logger;
    
    /// <summary>
    /// Initialises a new instance of the Database Migrator
    /// </summary>
    /// <param name="logger">the logger to use</param>
    public DbMigrator(ILogger logger)
    {
        Logger = logger;
    }
    
    /// <summary>
    /// Migrates data from one database to another
    /// </summary>
    /// <param name="sourceInfo">the source database</param>
    /// <param name="destinationInfo">the destination database</param>
    /// <returns>if the migration was successful</returns>
    public Result<bool> Migrate(DatabaseInfo sourceInfo, DatabaseInfo destinationInfo)
    {
        try
        {
            Logger?.ILog("Database Migration started");

            var source = DatabaseAccessManager.FromType(Logger!, sourceInfo.Type, sourceInfo.ConnectionString);
            var dest = DatabaseAccessManager.FromType(Logger!, destinationInfo.Type, destinationInfo.ConnectionString);

            if (dest.Type == DatabaseType.Sqlite)
            {
                // move the db if it exists so we can create a new one
                SQLiteConnector.MoveFileFromConnectionString(destinationInfo.ConnectionString);
            }

            var destCreator = DatabaseCreators.DatabaseCreator.Get(dest.Type, Logger!, destinationInfo.ConnectionString);
            var result = destCreator.CreateDatabase(recreate: true);
            if (result.Failed(out string error))
                return Result<bool>.Fail("Failed creating destination database: " + error);
            if(result.Value == DbCreateResult.Failed)
                return Result<bool>.Fail("Failed creating destination database");
            
            var structureResult = destCreator.CreateDatabaseStructure();
            if(structureResult.Failed(out error))
                return Result<bool>.Fail("Failed creating destination database structure: " + error);
            if(structureResult.Value == false)
                return Result<bool>.Fail("Failed creating destination database structure");
            
            MigrateDbObjects(source, dest);
            MigrateLibraryFiles(source, dest);
            MigrateDbStatistics(source, dest);
            MigrateRevisions(source, dest);
            // log messages, we dont care if these are migrated
            //MigrateDbLogs(source, dest);

            Logger?.ILog("Database Migration complete");
            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to migrate data: " + ex.Message);
        }
    }

    /// <summary>
    /// Migrates the DbObjects from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateDbObjects(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var dbObjects = source.DbObjectManager.GetAll().Result.ToArray();
        if (dbObjects?.Any() != true)
            return;

        foreach (var obj in dbObjects)
        {
            Logger?.DLog($"Migrating [{obj.Uid}][{obj.Type}]: {obj.Name ?? string.Empty}");

            try
            {
                dest.DbObjectManager.Insert(obj).Wait();
            }
            catch (Exception ex)
            {
                Logger?.ELog("Failed migrating: " +  ex.Message);
                Logger?.ELog("Migration Object: " + JsonSerializer.Serialize(obj));
                throw;
            }
        }
    }

    /// <summary>
    /// Migrates database statistics from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateDbStatistics(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var dbStatistics = source.DbStatisticManager.GetAll().Result;
        if (dbStatistics?.Any() != true)
            return;

        foreach (var obj in dbStatistics)
        {
            try
            {
                dest.DbStatisticManager.Insert(obj).Wait();
            }
            catch (Exception ex)
            {
                Logger?.WLog("Failed migrating database statistic: " + ex.Message);
            }
        }
    }
    
    /// <summary>
    /// Migrates revisions from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateRevisions(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var dbRevisions = source.DbRevisionManager.GetAll().Result;
        if (dbRevisions?.Any() != true)
            return;

        foreach (var obj in dbRevisions)
        {
            try
            {
                dest.DbRevisionManager.Insert(obj).Wait();
            }
            catch (Exception ex)
            {
                Logger?.WLog("Failed migrating object revision: " + ex.Message);
            }
        }
    }

    
    /// <summary>
    /// Migrates database log messages from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateDbLogs(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        if (source.Type == DatabaseType.Sqlite || dest.Type == DatabaseType.Sqlite)
            return;
        
        var dbLogMessages = source.DbLogMessageManager.GetAll().Result;
        if (dbLogMessages?.Any() != true)
            return;

        foreach (var obj in dbLogMessages)
        {
            try
            {
                dest.DbLogMessageManager.Insert(obj).Wait();
            }
            catch (Exception)
            {
                // we really dont care if these arent migrated
            }
        }
    }
    
    /// <summary>
    /// Migrates library files from one database to another
    /// </summary>
    /// <param name="source">the source database</param>
    /// <param name="dest">the destination database</param>
    private void MigrateLibraryFiles(DatabaseAccessManager source, DatabaseAccessManager dest)
    {
        var items = source.LibraryFileManager.GetAll().Result;
        if (items?.Any() != true)
            return;

        foreach (var obj in items)
        {
            try
            {
                dest.LibraryFileManager.Insert(obj).Wait();
            }
            catch (Exception ex)
            {
                Logger.ELog($"Failed migrating library file '{obj.Name}': " + ex.Message);
            }
        }
    }
}