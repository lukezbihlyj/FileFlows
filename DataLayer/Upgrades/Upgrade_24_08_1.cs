using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.ServerShared.Helpers;
using FileFlows.Shared;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.08.1
/// </summary>
public class Upgrade_24_08_1
{
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public Result<bool> Run(ILogger logger, DatabaseType dbType, string connectionString)
    {
        var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
        AddCustomVariablesField(logger, connector);
        return true;
    }
    
    /// <summary>
    /// Adds CustomVariables field to the LibraryFile table
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the connector</param>
    private void AddCustomVariablesField(ILogger logger, IDatabaseConnector connector)
    {
        if (connector.ColumnExists("LibraryFile", "CustomVariables").Result)
        {
            logger.ILog("LibraryFile.CustomVariables column already exists");
            return;
        }

        connector.CreateColumn("LibraryFile", "CustomVariables", "TEXT", "");
        logger.ILog("LibraryFile.CustomVariables column added");
    }
}