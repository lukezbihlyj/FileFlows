namespace FileFlowsTests.Tests.FlowTests.FileTests;

/// <summary>
/// Tests that run with basic text files
/// </summary>
public abstract class FileTest : FlowTest
{
    
    
    /// <summary>
    /// Creates a file library
    /// </summary>
    /// <param name="fileName">the name of the file</param>
    /// <returns>the path of the library</returns>
    protected async Task<string> CreateFileLibrary(string fileName)
    {
        var libPath = Path.Combine(TempPath, "lib-file-" + Guid.NewGuid());
        Directory.CreateDirectory(libPath);
        string file1 = Path.Combine(libPath,fileName);
        await File.WriteAllTextAsync(file1, "this is a test file");
        return libPath;
    }
}