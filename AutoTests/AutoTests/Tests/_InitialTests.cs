using FileFlowsTests.Helpers.UiComponents;

namespace FileFlowsTests.Tests;

/// <summary>
/// Initial tests the configures FileFlows.
/// And performs test on an un-configured system
/// </summary>
//[NonParallelizable]
public class InitialTests() : TestBase("")
{
    /// <summary>
    /// Test the license shows unlicensed
    /// </summary>
    [Test, Order(1)]
    public async Task InitialConfiguration()
    {
        await Page.WaitForURLAsync(FileFlows.BaseUrl + "initial-config");
        Assert.IsTrue(await FileFlows.InitialConfiguration.Shown(), "The Initial Configuration should be shown.");
        Assert.AreEqual("Welcome to FileFlows", await FileFlows.InitialConfiguration.GetPageTitle());
        Assert.IsTrue(await FileFlows.InitialConfiguration.PageEnabled("EULA"), "The EULA page should be enabled");
        Assert.IsFalse(await FileFlows.InitialConfiguration.PageEnabled("Plugins"), "The Plugins page should be disabled");
        Assert.IsFalse(await FileFlows.InitialConfiguration.PageEnabled("DockerMods"), "The DockerMods page should be disabled");
        Assert.IsFalse(await FileFlows.InitialConfiguration.PageEnabled("Finish"), "The Finish page should be disabled");
        Assert.IsFalse(await FileFlows.InitialConfiguration.PreviousButtonShown(), "The Previous button should not be shown");
        Assert.IsFalse(await FileFlows.InitialConfiguration.FinishButtonShown(), "The Finish button should not be shown");
        Assert.IsTrue(await FileFlows.InitialConfiguration.NextButtonShown(), "The Next button should be shown");
            
        await FileFlows.InitialConfiguration.NextClick();
        Assert.AreEqual("End-User License Agreement of FileFlows", await FileFlows.InitialConfiguration.GetPageTitle());
        Assert.IsTrue(await FileFlows.InitialConfiguration.PreviousButtonShown(), "The previous button should be shown and it is not.");
        Assert.IsTrue(await FileFlows.InitialConfiguration.NextDisabled(), "Next Button should be disabled until the EULA is accepted.");
        await FileFlows.InitialConfiguration.AcceptEula();
        Assert.IsFalse(await FileFlows.InitialConfiguration.NextDisabled(), "Next Button should not be disabled");
        await FileFlows.InitialConfiguration.NextClick();
        
        Assert.IsTrue(await FileFlows.InitialConfiguration.PageEnabled("Plugins"), "Plugins Page is not enabled");
        Assert.IsTrue(await FileFlows.InitialConfiguration.PageEnabled("DockerMods"), "DockerMods Page is not enabled");
        Assert.IsTrue(await FileFlows.InitialConfiguration.PageEnabled("Finish"), "Finish Page is not enabled");
        
        Assert.AreEqual("Choose which plugins to install", await FileFlows.InitialConfiguration.GetPageTitle());
        var plugins = await FileFlows.InitialConfiguration.GetItems();
        var basic = plugins.FirstOrDefault(x => x.Name == "Basic");
        Assert.IsNotNull(basic, "Basic Plugin is not found.");
        Assert.IsTrue(basic!.Checked, "Basic Plugin is not checked by default.");
        await FileFlows.InitialConfiguration.NextClick();

        Assert.AreEqual("Choose which DockerMods to install", await FileFlows.InitialConfiguration.GetPageTitle());
        var dockerMods = await FileFlows.InitialConfiguration.GetItems();
        var ffmpeg6 = dockerMods.FirstOrDefault(x => x.Name == "FFmpeg6");
        Assert.IsNotNull(ffmpeg6, "FFmpeg6 DockerMod is not found.");
        Assert.IsTrue(ffmpeg6!.Checked, "FFmpeg6 DockerMod is not checked by default.");
        await FileFlows.InitialConfiguration.ClearAllItems(); // we dont want this DockerMod installed
        await FileFlows.InitialConfiguration.NextClick();

        Assert.IsFalse(await FileFlows.InitialConfiguration.NextButtonShown(), "Next Button is shown when it should not be present");
        Assert.IsTrue(await FileFlows.InitialConfiguration.FinishButtonShown(), "Finish Button is not shown.");

        await FileFlows.InitialConfiguration.FinishClick();
        await Page.WaitForSelectorAsync(".sidebar .nav-menu-footer .version-info");
    }

    /// <summary>
    /// Sets the FFmpeg paths
    /// </summary>
    [Test, Order(10)] 
    public async Task SetFFmpegPath()
    {
        await FileFlows.GotoPage("Variables");
        await DoubleClickItem("ffmpeg");
        await SetTextArea("Value", "/tools/ffmpeg/ffmpeg");
        await ButtonClick("Save");
        await Task.Delay(500);
        await DoubleClickItem("ffprobe");
        await SetTextArea("Value", "/tools/ffmpeg/ffprobe");
        await ButtonClick("Save");
        await Task.Delay(500);
    }
    
    /// <summary>
    /// Test the license shows unlicensed
    /// </summary>
    [Test, Order(11)]
    public async Task LicenseUnlicensed()
    {
        await FileFlows.GotoPage("Settings");
        await FileFlows.Tab.Click("License");
    
        var txtStatus = Page.Locator("input[placeholder='Status']");
        await Expect(txtStatus).ToHaveCountAsync(1);
        await Expect(txtStatus).ToHaveValueAsync("Unlicensed");
    }
    
    /// <summary>
    /// Tests the tabs in settings are only the expected tabs
    /// </summary>
    [Test, Order(12)]
    public async Task UnlicensedTabs()
    {
        await FileFlows.GotoPage("Settings");
        await FileFlows.Tab.Exists("Logging");
        await FileFlows.Tab.Exists("Database");
        await FileFlows.Tab.Exists("Advanced");
        await FileFlows.Tab.Exists("Email");
        await FileFlows.Tab.Exists("License");
        
        await FileFlows.Tab.DoesntExists("Updates");
        await FileFlows.Tab.DoesntExists("File Server");
        await FileFlows.Tab.DoesntExists("Security");
    
        await FileFlows.Tab.Click("Logging");
        await Expect(Page.Locator("label >> text=Log Every Request")).ToHaveCountAsync(0);
        await Expect(Page.Locator("label >> text=Log File Retention")).ToHaveCountAsync(0);
    }
    
    /// <summary>
    /// Tests that if a user tries to add a library before adding a flow, they are stopped with a toast error message
    /// </summary>
    [Test, Order(20)]
    public async Task LibraryNoFlows()
    {
        await FileFlows.GotoPage("Libraries");
        await FileFlows.Table.ButtonClick("Add");
        Assert.AreEqual("There are no flows configured. Create a flow before adding or updating a library.", 
            await FileFlows.Toast.GetError());
    }
    
    /// <summary>
    /// Tests the pointer is shown for step 1.
    /// </summary>
    [Test, Order(30)]
    public async Task FlowPointer()
    {
        await Expect(Page.Locator(".nav-item.flows .not-configured-pointer")).ToHaveCountAsync(1);
        await FileFlows.GotoPage("Flows");
        await Expect(Page.Locator(".pointer-add >> text='Add'")).ToHaveCountAsync(1);
    }
    
    /// <summary>
    /// Tests creating a flow
    /// </summary>
    [Test, Order(31)]
    public async Task FlowCreate()
    {
        await FileFlows.GotoPage("Flows");
        await FileFlows.Table.ButtonClick("Add");
    
        var templates = await FileFlows.FlowTemplateDialog.GetTemplates();
        var templateFile = templates.FirstOrDefault(x => x.Name == "File");
        Assert.IsNotNull(templateFile);
    
        await FileFlows.FlowTemplateDialog.Select("File");
    
        await FileFlows.Flow.SetTitle(Constants.Flow_Basic);
        await FileFlows.Flow.Save();
        
        await FileFlows.GotoPage("Flows");
    
        await Expect(Page.Locator(".nav-item.libraries .not-configured-pointer")).ToHaveCountAsync(1);
    }
    
    /// <summary>
    /// Creates a library
    /// </summary>
    [Test, Order(40)]
    public async Task LibraryCreate()
    {
        await FileFlows.GotoPage("Libraries");
        await Expect(Page.Locator(".pointer-add >> text='Add'")).ToHaveCountAsync(1);
        await FileFlows.Table.ButtonClick("Add");
        await FileFlows.Editor.Title("Library");
        await FileFlows.Inputs.SetText("Name", Constants.Library_Basic);
        await FileFlows.Editor.ButtonClick("Save");
        await FileFlows.Inputs.Error("Path", "Required");
        await FileFlows.Inputs.Error("Flow", "Required");
    
        await FileFlows.Inputs.SetSelect("Template", "Video Library");
        await FileFlows.Inputs.SetText("Path", "/media/basic");
        await FileFlows.Inputs.SetSelect("Flow", Constants.Flow_Basic);
        await FileFlows.Editor.ButtonClick("Save");
    
        await Expect(Page.Locator(".pointer-add >> text='Add'")).ToHaveCountAsync(0);
    }
    
    [Test, Order(80)]
    public async Task CheckUnLicensedPages()
    {
        await Expect(Page.Locator("a[href='tasks']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("a[href='revisions']")).ToHaveCountAsync(0);
    }
    
    /// <summary>
    /// Tests language can be changed
    /// </summary>
    [Test, Order(81)]
    public async Task ChangeLanguage()
    {
        await FileFlows.GotoPage("Settings");
        await FileFlows.Tab.Click("Advanced");
    
        await FileFlows.Inputs.SetSelect("Language", "Español");
        await Page.Locator("#settings-save").ClickAsync();
        await Task.Delay(500);
        await FileFlows.WaitForBlockerToDisappear();
        
        await FileFlows.GotoPage("Configuraciones");
        await FileFlows.Tab.Click("Avanzado");
        
        await FileFlows.Inputs.SetSelect("Language", "Deutsch");
        await Page.Locator("#settings-save").ClickAsync();
        await Task.Delay(500);
        await FileFlows.WaitForBlockerToDisappear();
        
        await FileFlows.GotoPage("Einstellungen");
        await FileFlows.Tab.Click("Erweitert");
        
        await FileFlows.Inputs.SetSelect("Language", "English");
        await Page.Locator("#settings-save").ClickAsync();
        await FileFlows.GotoPage("Settings");
        await Task.Delay(500);
        await FileFlows.WaitForBlockerToDisappear();
    }
    
    [Test, Order(90)]
    public async Task EnterLicense()
    {
        await FileFlows.GotoPage("Settings");
        await FileFlows.Tab.Click("License");
    
        var licenseEmail = Environment.GetEnvironmentVariable("FF_LICENSE_EMAIL") ?? string.Empty;
        var licenseKey = Environment.GetEnvironmentVariable("FF_LICENSE_KEY") ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(licenseEmail), "License Email is not set");
        Assert.IsFalse(string.IsNullOrWhiteSpace(licenseKey), "License Key is not set");
    
        await Page.Locator("input[placeholder='License Email']").FillAsync(licenseEmail);
        await Page.Locator("input[placeholder='License Key']").FillAsync(licenseKey);
        await Page.Locator("button >> text=Save").ClickAsync();
    
        var txtStatus = Page.Locator("input[placeholder='Status']");
        await Expect(txtStatus).ToHaveValueAsync("Valid", new() { Timeout = 20_000 });
    
        await FileFlows.Tab.Exists("Logging");
        await FileFlows.Tab.Exists("License");
        await FileFlows.Tab.Exists("Database");
        await FileFlows.Tab.Exists("Updates");
        await FileFlows.Tab.Exists("Advanced");
    
        await Expect(Page.Locator("a[href='tasks']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("a[href='revisions']")).ToHaveCountAsync(1);
    }
    
    [Test, Order(100)]
    public async Task TasksNoScript()
    {
        await GotoPage("Tasks");
        await TableButtonClick("Add");
        await ToastError("No scripts found to create a task for.");
    }
    
    [Test, Order(101)]
    public async Task GotifyFileProcessed()
    {
        await GotoPage("Scripts");
        const string name = "Gotify - Notify File Processed";
        await SkyBox("SystemScripts");
        await TableButtonClick("Repository");
        await SelectItem(name, sideEditor: true);
        await TableButtonClick("Download", sideEditor: true);
        await FileFlows.Editor.ButtonClick("Close");
        await SelectItem(name);
    
        await GotoPage("Variables");
        await TableButtonClick("Add");
        await SetText("Name", "Gotify.Url");
        await SetTextArea("Value",
            Environment.GetEnvironmentVariable("GotifyUrl") ?? "http://gotify.lan/");
        await ButtonClick("Save");
        await TableButtonClick("Add");
        await SetText("Name", "Gotify.AccessToken");
        await SetTextArea("Value", Environment.GetEnvironmentVariable("GotifyAccessToken") ?? Guid.NewGuid().ToString());
        await ButtonClick("Save");
    
        await GotoPage("Tasks");
        await TableButtonClick("Add");
        await SetText("Name", "Gotify File Processed");
        await SetSelect("Script", name);
        await SetSelect("Type", "File Processed");
        await ButtonClick("Save");
    }
}