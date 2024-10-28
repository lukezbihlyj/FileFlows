namespace FileFlowsTests.Helpers.UiComponents;

public abstract class UiComponent
{ 
    protected IPage Page { get; }
    protected IBrowserContext Context { get; }
    
    public UiComponent(TestBase test)
    {
        this.Page = test.Page;
        this.Context = test.Context;
    }
    
    protected ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    protected IPageAssertions Expect(IPage page) => Assertions.Expect(page);

    protected IAPIResponseAssertions Expect(IAPIResponse response) => Assertions.Expect(response);

}