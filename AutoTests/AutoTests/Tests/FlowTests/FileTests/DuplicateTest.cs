namespace FileFlowsTests.Tests.FlowTests.FileTests;

/// <summary>
/// Tests duplicates are detected and can be run
/// </summary>
public class DuplicateTest : FileTest
{
    static string duplicateFile = Guid.NewGuid() + ".duplicate";
    
    /// <summary>
    /// Manually creates an audiobook flow and tests it
    /// </summary>
    [Test, Order(1)]
    public async Task DuplicateDetected()
    {
        var flowName = RandomName("Duplicate Flow");
        string libName = "Library For " + flowName;
        await CreateFlow(flowName, "File", []);
        
        // Create library
        string libPath = await CreateDuplicateLibrary(duplicateFile);
        await CreateBasicLibrary(libName, flowName, libPath: libPath, scan: true, template: "Custom", fingerprinting: true);
        await Task.Delay(10_000);
        await GotoPage("Files");
        await SkyBox("Duplicate", waitFor: true);
        Assert.IsTrue(await WaitForExists(duplicateFile), $"Failed to locate duplicate item '{duplicateFile}'");
    }
    
    /// <summary>
    /// Manually creates an audiobook flow and tests it
    /// </summary>
    [Test, Order(2)]
    public async Task DuplicateProcessed()
    {
        await GotoPage("Files");
        await SkyBox("Duplicate", waitFor: true);
        Assert.IsTrue(await WaitForExists(duplicateFile), $"Failed to locate duplicate item '{duplicateFile}'");
        
        await SelectItem(duplicateFile);
        await TableButtonClick("Process");
        await MessageBoxButton("Save");
        
        await CheckFileProcessed(duplicateFile);
    }
    
    private async Task<string> CreateDuplicateLibrary(string duplicateFile)
    {
        var libPath = Path.Combine(TempPath, "lib-duplicate-" + Guid.NewGuid());
        Directory.CreateDirectory(libPath);
        string file1 = Path.Combine(libPath, "file1.txt");
        await File.WriteAllTextAsync(file1, "this is a test file");
        File.Copy(file1, Path.Combine(libPath, duplicateFile));
        return libPath;
    }
}