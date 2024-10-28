namespace FileFlowsTests.Tests;

//[TestClass]
public class Basic :TestBase
{

    
    [Test]
    public async Task BasicTest()
    {
        await Page.GotoAsync("http://goolge.com");
    }
}     