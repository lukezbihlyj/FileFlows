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

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.09.2
/// </summary>
public class Upgrade_24_09_2
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
        using var db = connector.GetDb(true).Result;

        UpdateLibraries(logger, connector, db);
        return true;
    }

    /// <summary>
    /// Updates the libraries
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the DB connector</param>
    /// <param name="db">the DB connection</param>
    private void UpdateLibraries(ILogger logger, IDatabaseConnector connector, DatabaseConnection db)
    {
        var libraries = db.Db.Fetch<UpgradeLibrary>(
            $"select {connector.WrapFieldName("Uid")}, {connector.WrapFieldName("Name")}, {connector.WrapFieldName("Data")} from" +
            $" {connector.WrapFieldName("DbObject")} where {connector.WrapFieldName("Type")} = 'FileFlows.Shared.Models.Library'");
        foreach (var lib in libraries)
        {
            // Parse the JSON data into a JsonDocument
            using var document = JsonDocument.Parse(lib.Data);
            var root = document.RootElement;

            // Convert to a dictionary to allow modifications
            var jsonDict = new Dictionary<string, JsonElement>();

            foreach (var property in root.EnumerateObject())
            {
                jsonDict[property.Name] = property.Value;
            }

            bool changes = false;

            // Handle the "Filter" to "Filters" transformation
            if (jsonDict.TryGetValue("Filter", out var filterProperty))
            {
                changes = true;
                var filter = filterProperty.GetString();
                // Replace "Filter" with "Filters" as an array of strings
                jsonDict.Remove("Filter");
                if(string.IsNullOrWhiteSpace(filter))
                    jsonDict["Filters"] = JsonSerializer.SerializeToElement(new string[] {  });
                else
                    jsonDict["Filters"] = JsonSerializer.SerializeToElement(new string[] { $"/{filter}/" });
            }

            // Handle the "ExclusionFilter" to "ExclusionFilters" transformation
            if (jsonDict.TryGetValue("ExclusionFilter", out var exclusionFilterProperty))
            {
                changes = true;
                var exclusionFilter = exclusionFilterProperty.GetString();
                // Replace "ExclusionFilter" with "ExclusionFilters" as an array of strings
                jsonDict.Remove("ExclusionFilter");
                if(string.IsNullOrWhiteSpace(exclusionFilter))
                    jsonDict["ExclusionFilters"] = JsonSerializer.SerializeToElement(new string[] {  });
                else
                    jsonDict["ExclusionFilters"] = JsonSerializer.SerializeToElement(new string[] { $"/{exclusionFilter}/" });
            }

            if (changes == false)
            {
                logger.ILog($"Library {lib.Name} no changes required");
                continue;
            }

            // Convert the modified dictionary back to a JSON string
            var updatedJson = new Dictionary<string, object>();
            foreach (var kvp in jsonDict)
            {
                updatedJson[kvp.Key] = kvp.Value;
            }

            // Serialize the updated JSON data
            string updatedData = JsonSerializer.Serialize(updatedJson);
            
            db.Db.Execute(
                $"update {connector.WrapFieldName("DbObject")} set {connector.WrapFieldName("Data")} = @0 " +
                $"where {connector.WrapFieldName("Uid")} = '{lib.Uid}'", updatedData);
            logger.ILog($"Library {lib.Name} updated");
        }
    }

    /// <summary>
    /// The library to upgrade
    /// </summary>
    private class UpgradeLibrary
    {
        /// <summary>
        /// Gets or sets the UID of the library
        /// </summary>
        public Guid Uid { get; set; }

        /// <summary>
        /// Gets or sets the name of the library
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data
        /// </summary>
        public string Data { get; set; } = string.Empty;
    }
}