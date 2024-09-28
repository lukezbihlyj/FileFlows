namespace FileFlowsTests.Tests.FlowTests.FileTests;

/// <summary>
/// Tests a failure flow
/// </summary>
//[TestClass]
public class FailureTest : FileTest
{
    /// <summary>
    /// Manually creates an audiobook flow and tests it
    /// </summary>
    [Test, Order(1)]
    public async Task FailureFlow()
    {
        string failureOutput = Path.Combine(TempPath, $"failure-output-{Guid.NewGuid()}.log");
        string failureReason = "FailureReasonText: " + Guid.NewGuid();
        var flowName = RandomName("Basic Flow for Failing");
        string libName = "Library For " + flowName;
        await CreateFlow(flowName, "File", []);
        await FileFlows.Flow.AddFlowElement("FileFlows.BasicNodes.Functions.FailFlow", 250, 220);
        await SetText("Reason", failureReason);
        await ButtonClick("Save");
        await FileFlows.Flow.Connect("Input File", "Fail Flow");
        await FileFlows.Flow.Save();
        
        var failFlowName = RandomName("Fail Flow");
        await CreateFlow(failFlowName, "Failure Flow", []);
        
        await FileFlows.Flow.AddFlowElement("FileFlows.BasicNodes.Functions.Matches", 450, 200);
        await Page.Locator(".flow-element-editor .input-keyvalue-wrapper input").First.FillAsync("{FailureReason}");
        await Page.Locator(".flow-element-editor .input-keyvalue-wrapper input").Last.FillAsync(failureReason);
        await Page.Locator(".flow-element-editor .input-keyvalue-wrapper .fa-plus").ClickAsync();
        await ButtonClick("Save");
        await Task.Delay(250);
        await FileFlows.Flow.Connect("Flow Failure", "Matches");
        
        await FileFlows.Flow.AddFlowElement("FileFlows.BasicNodes.Functions.Function", 450, 340);
        await SetCode($@"
System.IO.File.AppendAllText(""{failureOutput}"", ""Failure Reason: "" + Variables.FailureReason + ""\n"");
System.IO.File.AppendAllText(""{failureOutput}"", ""Failed Element: "" + Variables.FailedElement + ""\n"");
System.IO.File.AppendAllText(""{failureOutput}"", ""Flow Name: "" + Variables.FlowName + ""\n"");
return 1");
        await ButtonClick("Save");
        await Task.Delay(250);
        await FileFlows.Flow.Connect("Matches", "Function");
        await FileFlows.Flow.Save();
        await GotoPage("Flows");
        await SkyBox("Failure Flows");
        await SelectItem(failFlowName);
        await TableButtonClick("Default");
        
        // Create library
        string libPath = await CreateFileLibrary($"fail-file-{Guid.NewGuid()}.txt");
        await CreateBasicLibrary(libName, flowName, libPath: libPath, scan: true, template: "Custom", fingerprinting: true);

        int count = 0;
        while (count < 30 * 4)
        {
            if (File.Exists(failureOutput))
                break;
            await Task.Delay(250);
            count++;
        }
        Assert.IsTrue(File.Exists(failureOutput), "Failed to create failure file: " + failureOutput);
        string content = await File.ReadAllTextAsync(failureOutput);
        Logger.ILog("Failure Output: " + Environment.NewLine + new string('-', 100) + Environment.NewLine + content.Trim() + Environment.NewLine + new string('-', 100));
        
        Assert.IsTrue(content.Contains($"Failure Reason: {failureReason}"), $"Failure reason not found in file: {failureReason}");
    }
}