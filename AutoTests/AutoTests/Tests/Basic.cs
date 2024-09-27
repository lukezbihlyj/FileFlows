namespace FileFlowsTests.Tests;

[TestClass]
public class Basic :TestBase
{

    
    [TestMethod]
    public async Task BasicTest()
    {
        await Page.GotoAsync("http://goolge.com");
    }
}     