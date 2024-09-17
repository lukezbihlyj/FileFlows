namespace FileFlowsTests.Tests.CacheControllers;

/// <summary>
/// Test base for cache controller tests
/// </summary>
public abstract class CacheControllerTestBase
{
    static CacheControllerTestBase()
    {
        DirectoryHelper.Init();
    }
}