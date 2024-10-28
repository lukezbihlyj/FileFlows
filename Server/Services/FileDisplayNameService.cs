using FileFlows.Plugin;
using FileFlows.Shared.Models;
using Jint;

namespace FileFlows.Server.Services;

/// <summary>
/// Service to convert a file name to a display name
/// </summary>
public class FileDisplayNameService
{
    /// <summary>
    /// If no script is loaded and should not be called
    /// </summary>
    private static bool NoScript = true;
    /// <summary>
    /// The engine to execute the script
    /// </summary>
    private static Engine jsGetDisplayName;
    
    /// <summary>
    /// Initializes the service
    /// </summary>
    public FileDisplayNameService()
    {
        Reinitialize();
    }

    /// <summary>
    /// Reinitializes the service with a modified script
    /// </summary>
    public void Reinitialize()
    {
        string? code;
        try
        {
            var service = new ScriptService();
            var script = service.GetByName(CommonVariables.FILE_DISPLAY_NAME).Result;
            code = script?.Code;
        }
        catch(Exception)
        {
            jsGetDisplayName = null;
            NoScript = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            jsGetDisplayName = null;
            NoScript = true;
            return;
        }

        try
        {
            jsGetDisplayName= new Engine().Execute(code);
            NoScript = false;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Error in {CommonVariables.FILE_DISPLAY_NAME} script: " + ex.Message);
            NoScript = true;
        }
    }

    /// <summary>
    /// Gets the display name for the file
    /// </summary>
    /// <param name="libFile">the library file</param>
    /// <returns>the display name</returns>
    public string GetDisplayName(LibraryFile libFile)
    {
        return GetDisplayName(libFile.Name, libFile.RelativePath, libFile.LibraryName);
    }
    
    /// <summary>
    /// Gets the display name for the file
    /// </summary>
    /// <param name="name">the original name</param>
    /// <param name="relativePath">the relative path</param>
    /// <param name="libraryName">the name of the library</param>
    /// <returns>the display name</returns>
    public string GetDisplayName(string name, string relativePath, string libraryName)
    {
        if (NoScript || jsGetDisplayName == null)
            return relativePath?.EmptyAsNull() ?? name;
        try
        {
            lock (jsGetDisplayName)
            {
                return jsGetDisplayName.Invoke("getDisplayName", name, relativePath, libraryName)?.ToString()?.EmptyAsNull() ?? name;
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ILog("Error getting display name: " + ex.Message);
            return relativePath;
        }
    }
}