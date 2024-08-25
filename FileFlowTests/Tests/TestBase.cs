namespace FileFlowTests.Tests;

/// <summary>
/// Test base file
/// </summary>
public class TestBase
{
    /// <summary>
    /// The test context instance
    /// </summary>
    private TestContext testContextInstance;

    /// <summary>
    /// Gets or sets the test context
    /// </summary>
    public TestContext TestContext
    {
        get => testContextInstance;
        set => testContextInstance = value;
    }

    /// <summary>
    /// When the test starts
    /// </summary>
    [TestInitialize]
    public void TestStarted()
    {
        Logger.Writer = (message) => TestContext.WriteLine(message);

        TestStarting();
    }

    protected virtual void TestStarting()
    {

    }

    /// <summary>
    /// The test logger
    /// </summary>
    public readonly TestLogger Logger = new ();

    /// <summary>
    /// The directory with the test files
    /// </summary>
    protected readonly string TestFilesDir = "/home/john/src/ff-files/test-files";
    
    /// <summary>
    /// The resources test file directory
    /// </summary>
    protected readonly string ResourcesTestFilesDir = "Resources/TestFiles";

    /// <summary>
    /// The temp path to use during testing
    /// </summary>
    protected readonly string TempPath = System.IO.Path.GetTempPath().TrimEnd('/');
}