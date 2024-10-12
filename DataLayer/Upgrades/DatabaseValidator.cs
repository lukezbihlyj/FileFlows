using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Models;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using FileFlows.ServerShared.Helpers;
using FileFlows.Shared;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Database Valdiator
/// </summary>
public class DatabaseValidator
{
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public async Task<Result<bool>> EnsureColumnsExist(ILogger logger, DatabaseType dbType, string connectionString)
    {
        var connector = DatabaseConnectorLoader.LoadConnector(logger, dbType, connectionString);
        try
        {
            if (await connector.ColumnExists("LibraryFile", "FailureReason") == false)
            {
                logger.ILog("Adding LibraryFile.FailureReason column");
                await connector.CreateColumn("LibraryFile", "FailureReason", "VARCHAR(512)", "''");
            }

            if (await connector.ColumnExists("LibraryFile", "ProcessOnNodeUid") == false)
            {
                logger.ILog("Adding LibraryFile.ProcessOnNodeUid column");
                await connector.CreateColumn("LibraryFile", "ProcessOnNodeUid", "varchar(36)", "''");
            }

            if (await connector.ColumnExists("LibraryFile", "CustomVariables") == false)
            {
                logger.ILog("Adding LibraryFile.CustomVariables column");
                await connector.CreateColumn("LibraryFile", "CustomVariables", "TEXT", "");
            }

            if (await connector.ColumnExists("LibraryFile", "Additional") == false)
            {
                logger.ILog("Adding LibraryFile.Additional column");
                await connector.CreateColumn("LibraryFile", "Additional", "TEXT", "");
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.ELog("Failed ensuring columns exist: " + ex.Message + Environment.NewLine + ex.StackTrace);
            return Result<bool>.Fail(ex.Message);
        }
    }
}