using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using FileFlows.Shared;

namespace FileFlows.FlowRunner.Helpers.ArchiveHelpers;

public class RarArchiveHelper : IArchiveHelper
{
    /// <summary>
    /// The Rar executable
    /// </summary>
    private readonly string RarExecutable;
    /// <summary>
    /// The Unrar Executable
    /// </summary>
    private readonly string UnrarExecutable;
    /// <summary>
    /// The logger used for logging
    /// </summary>
    private readonly ILogger Logger;
    
    /// <summary>
    /// Initializes a new instance of the Rar Archive Helper
    /// </summary>
    /// <param name="args">The Node parameters</param>
    public RarArchiveHelper(NodeParameters args)
    {
        RarExecutable = args.GetToolPath("rar") ?? "rar";
        UnrarExecutable = args.GetToolPath("unrar") ?? "unrar";
        Logger = args.Logger;
    }
    
    /// <inheritdoc />
    public Result<bool> FileExists(string archivePath, string file)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<bool>.Fail("Archive file not found: " + archivePath);

        try
        {
            // Open the zip archive
            using ZipArchive archive = ZipFile.OpenRead(archivePath);
            return archive.Entries.Any(x => x.FullName.ToLowerInvariant().Equals(file.ToLowerInvariant()));
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> AddToArchive(string archivePath, string file)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = RarExecutable,
                ArgumentList = {"a", archivePath, file },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
                return Result<bool>.Fail("Failed to start rar process.");

            process.WaitForExit();

            if (process.ExitCode == 0) return true;
            
            var error = process.StandardError.ReadToEnd();
            return Result<bool>.Fail($"Failed to add file to RAR archive: {error}");

        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to add file to rar archive: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Result<int> GetFileCount(string archivePath, string pattern)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<int>.Fail("Archive file not found: " + archivePath);
        
        var process = new Process();
        process.StartInfo.FileName = UnrarExecutable;
        process.StartInfo.ArgumentList.Add("list");
        process.StartInfo.ArgumentList.Add(archivePath);
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        string output = process.StandardError.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception(error?.EmptyAsNull() ?? "Failed to open rar file");

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var rgxFiles = new Regex(pattern, RegexOptions.IgnoreCase);
        return lines.Count(x => rgxFiles.IsMatch(x.Trim()));
    }

    /// <inheritdoc />
    public Result<bool> Compress(string path, string output, string pattern = "",
        bool allDirectories = true, Action<float>? percentCallback = null)
    {
        // Implement RAR compression using the rar executable
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = RarExecutable,
                ArgumentList = { "a", output,string.IsNullOrWhiteSpace(pattern) ? path :  @$"{path}\{pattern}" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return Result<bool>.Fail("Failed to start rar process.");
            }

            process.WaitForExit();

            if (process.ExitCode == 0) return true;
            
            var error = process.StandardError.ReadToEnd();
            return Result<bool>.Fail($"Failed to create RAR archive: {error}");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to create RAR archive: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Result<bool> Extract(string archivePath, string destinationPath,
        Action<float>? percentCallback = null)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = UnrarExecutable,
                ArgumentList = { "x", archivePath, destinationPath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return Result<bool>.Fail("Failed to start unrar process.");
            }

            process.WaitForExit();

            if (process.ExitCode == 0) return true;
            
            var error = process.StandardError.ReadToEnd();
            return Result<bool>.Fail($"Failed to extract RAR archive: {error}");

        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to extract RAR archive: {ex.Message}");
        }
    }
}