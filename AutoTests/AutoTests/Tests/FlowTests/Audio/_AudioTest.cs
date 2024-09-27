using System.Diagnostics;

namespace FileFlowsTests.Tests.FlowTests.Audio;

/// <summary>
/// Base Class for audio tests
/// </summary>
public abstract class AudioTest : FlowTest
{
    /// <summary>
    /// Generates a random audio file (white noise) using FFmpeg.
    /// </summary>
    /// <param name="outputFilePath">The path where the audio file will be saved.</param>
    /// <param name="durationInSeconds">The duration of the audio file in seconds. Default is 30 seconds.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task GenerateRandomAudio(string outputFilePath, int durationInSeconds = 5)
    {
        // Ensure the duration is a positive value
        if (durationInSeconds <= 0)
            throw new ArgumentException("Duration must be a positive integer", nameof(durationInSeconds));

        var fileInfo = new FileInfo(outputFilePath);
        if (fileInfo.Directory?.Exists == false)
            fileInfo.Directory.Create();

        // Execute FFmpeg to generate the audio file
        await RunFFmpegCommandAsync(["-f", "lavfi", "-i", $"anoisesrc=d={durationInSeconds}:c=pink", "-ac", "2", "-ar", "44100", outputFilePath]);
    }

    /// <summary>
    /// Runs an FFmpeg command asynchronously.
    /// </summary>
    /// <param name="arguments">The FFmpeg arguments for generating audio.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RunFFmpegCommandAsync(string[] arguments)
    {
        var ffmpeg = File.Exists("/tools/ffmpeg/ffmpeg") ? "/tools/ffmpeg/ffmpeg" : "ffmpeg";
        using var process = new Process();
        process.StartInfo.FileName = "ffmpeg";
        // Add the argument list
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        await process.WaitForExitAsync();
            
        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"FFmpeg failed with error: {error}");
        }
    }
}