using FileFlows.Server.Cli;
using FileFlows.Server.Services;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server;

/// <summary>
/// Main entry point for server
/// </summary>
public class Program
{
    /// <summary>
    /// General cache used by the server
    /// </summary>
    internal static CacheStore GeneralCache = new ();

    [STAThread] // need for Photino.net on windows
    public static void Main(string[] args)
    {
        #if(DEBUG)
        var i18nDirectory = Path.Combine("..", "Client", "wwwroot", "i18n");
        var jsonFiles = Directory.GetFiles(i18nDirectory, "*.json");

        foreach (var jsonFile in jsonFiles)
        {
            if (jsonFile.Contains("plugins.", StringComparison.InvariantCultureIgnoreCase))
                continue;
            var jsonContent = File.ReadAllText(jsonFile);
            var reorderedJson = Translater.ReorderJson(jsonContent);
            File.WriteAllText(jsonFile, reorderedJson);
        }
        #endif
        
        if (CommandLine.Process(args))
            return;
        
        Application app = ServiceLoader.Provider.GetRequiredService<Application>();
        ServerShared.Services.SharedServiceLoader.Loader = type => ServiceLoader.Provider.GetRequiredService(type);
        app.Run(args);
    }
    
}
