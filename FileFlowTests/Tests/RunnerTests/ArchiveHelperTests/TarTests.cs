#if(DEBUG)
using System.Diagnostics;
using System.IO;
using FileFlows.FlowRunner.Helpers;

namespace FileFlowTests.Tests.RunnerTests.ArchiveHelperTests;

/// <summary>
/// Tests TAR files
/// </summary>
[TestClass]
public class TarTests : TestBase
{
    private string tarFilePath;
    
    [TestInitialize]
    public void Setup()
    {
        // Create sample files to be included in the TAR file
        string sampleFile1 = Path.Combine(TempPath, "sample1.txt");
        File.WriteAllText(sampleFile1, "This is a test file.");

        string sampleFile2 = Path.Combine(TempPath, "sample2.txt");
        File.WriteAllText(sampleFile2, "This is another test file.");

        // Create a TAR file including the sample files
        tarFilePath = CreateTarFile(TempPath, "archives/tar.tar", sampleFile1, sampleFile2);
    }
    
    private string CreateTarFile(string directory, string tarFileName, params string[] files)
    {
        string tarFilePath = Path.Combine(directory, tarFileName);

        // Use 7z to create a TAR file
        var startInfo = new ProcessStartInfo
        {
            FileName = "7z",
            Arguments = $"a -ttar \"{tarFilePath}\" {string.Join(" ", files.Select(f => $"\"{f}\""))}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Failed to create TAR file. Output: {output}. Error: {error}");
            }
        }

        return tarFilePath;
    }
    
    /// <summary>
    /// Tests a tar file can be extracted
    /// </summary>
    [TestMethod]
    public void Basic()
    {
        var helper = new ArchiveHelper(Logger, "rar", "unrar", "7z");
        var result = helper.Extract(tarFilePath, TempPath, (percent) =>
        {
            Logger.ILog("Percent: " + percent);
        });
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
    
    /// <summary>
    /// Tests a tar file can be extracted 2
    /// </summary>
    [TestMethod]
    public void Basic2()
    {
        var helper = new ArchiveHelper(Logger, "rar", "unrar", "7z");
        var result = helper.Extract(tarFilePath, TempPath);
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
}
#endif