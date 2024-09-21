using System.Web;

namespace FileFlowsTests.Helpers.UiComponents;

public class Inputs:UiComponent
{
    public Inputs(TestBase test) : base(test)
    {
    }

    private ILocator Input(string name, string inputType)
        => Page.Locator($"div[x-id='{name}'] {inputType}");

    public Task SetText(string name, string value)
        => Input(name, "input[type=text]").FillAsync(value);
    
    public Task SetTextArea(string name, string value)
        => Input(name, "textarea").FillAsync(value);
    
    public Task SetSelect(string name, string value)
        => Input(name, "select").SelectOptionAsync(new SelectOptionValue { Label = value});
    public Task SetNumber(string name, int value)
        => Input(name, "input[type=number]").FillAsync(value.ToString());


    /// <summary>
    /// Sest the values in a array
    /// </summary>
    /// <param name="name">the name of the field</param>
    /// <param name="values">the values</param>
    public async Task SetArray(string name, string[] values)
    {
        var input = Input(name, "input[type=text]");
        foreach (var value in values)
        {
            await input.FillAsync(value);
            await input.PressAsync("Enter");
        }
    }
    
    public async Task SetToggle(string name, bool value)
    {
        var input = Input(name, "input[type=checkbox]");
        var toggle = Input(name, ".slider");
        bool current = await input.IsCheckedAsync();
        if (value == current)
            return;
        await toggle.ClickAsync();
        current = await input.IsCheckedAsync();
        if (current != value)
            throw new Exception("Failed to set toggle");
    }

    public async Task SetCode(string code)
    {   
        var modifier = "Control";
        var newPage = await Context.NewPageAsync();
        var html = HttpUtility.HtmlEncode(code).Replace("\n", "<br>");
        await newPage.SetContentAsync($"<div contenteditable>{html}</div>");
        await newPage.FocusAsync("div");
        await newPage.Keyboard.PressAsync($"{modifier}+KeyA");
        await newPage.Keyboard.PressAsync($"{modifier}+KeyC");
        await newPage.Keyboard.PressAsync($"{modifier}+KeyV");
        await newPage.CloseAsync();
        
        var editor = Page.Locator(".monaco-editor-container textarea").First;
        await editor.FocusAsync();
        await Page.Keyboard.PressAsync($"{modifier}+KeyA");
        await Page.Keyboard.PressAsync($"{modifier}+KeyV");
        await editor.EvaluateAsync("e => e.blur()");
    }

    public Task Error(string name, string expected)
        =>  Expect(Page.Locator($"div[x-id='error-{name}'] .error-text >> text='{expected}'")).ToHaveCountAsync(1);
}