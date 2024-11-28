using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input Text component
/// </summary>
public partial class InputIconPicker : Input<string>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();
    Microsoft.AspNetCore.Components.Forms.InputFile fileInput;
    private bool ModalOpened = false;
    private string SelectedIcon;
    private string Filter = string.Empty;
    private string Color;
    private string Icon;
    private string IconColor;
    private string lblPickIcon, lblFilter, lblSelect,lblUpload, lblCancel, lblClear;
    
    /// <summary>
    /// Gets or sets if the SVGs should be included
    /// </summary>
    [Parameter]
    public bool IncludeSvgs { get; set; }
    
    /// <summary>
    /// Gets or sets if the icon can be cleared
    /// </summary>
    [Parameter]
    public bool AllowClear { get; set; }


    private bool SvgSelected = false;

    // the SVG icons
    private static string[] Svgs =
    [
        "amd", "apple", "docker", "dos", "intel", "linux", "nvidia", "windows"
    ];
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblPickIcon = Translater.Instant("Dialogs.IconPicker.Title");
        lblFilter = Translater.Instant("Labels.Filter");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblUpload = Translater.Instant("Labels.Upload");
        lblSelect = Translater.Instant("Labels.Select");
        lblClear = Translater.Instant("Labels.Clear");
        if (Value?.StartsWith("data:") != true)
        {
            var parts = (Value ?? string.Empty).Split(':');
            if (parts[0] == "svg")
            {
                Icon = parts[1];
                SvgSelected = true;
            }
            else
            {
                
                Icon = parts.FirstOrDefault();
                IconColor = parts.Length > 1 ? parts[1] : string.Empty;
                Color = IconColor;
            }
        }
    }

    /// <inheritdoc />
    protected override void ValueUpdated()
    {
        ClearError();
        var parts = (Value ?? string.Empty).Split(':');
        Icon = parts.FirstOrDefault();
        IconColor = parts.Length > 1 ? parts[1] : string.Empty;
    }
    
    /// <summary>
    /// Shows a dialog to choose a built-in font
    /// </summary>
    void Choose()
    {
        if (ReadOnly) 
            return;
    
        Filter = string.Empty;
        SelectedIcon = string.Empty;
        ModalOpened = true;
    }
    
    /// <summary>
    /// Shows a dialog to upload a file
    /// </summary>
    async Task Upload()
    {    
        // Programmatically trigger the file input dialog using JSInterop
        await jsRuntime.InvokeVoidAsync("eval", "document.getElementById('fileInput').click()");
    }

    /// <summary>
    /// Clears the icon
    /// </summary>
    void Clear()
    {
        this.Value = null;
        this.ModalOpened = false;
    }

    async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file != null)
        {
            // Read the file as base64 string
            Value= await ConvertToBase64(file);
            ModalOpened = false;
            StateHasChanged();
        }
    }
    public static async Task<string> ConvertToBase64(IBrowserFile file)
    {
        using (var memoryStream = new MemoryStream())
        {
            await file.OpenReadStream().CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var base64String = Convert.ToBase64String(bytes);
            return $"data:{file.ContentType};base64,{base64String}";
        }
    }

    private void SelectIcon(string icon, bool svg = false)
    {
        SvgSelected = svg;
        SelectedIcon = icon;
    }

    private void DblClick(string icon, bool svg = false)
    {
        SvgSelected = svg;
        this.Value = svg ? $"svg:{icon}" : icon + (string.IsNullOrWhiteSpace(Color) ? string.Empty : ":" + Color);
        ModalOpened = false;
    }

    private void Cancel()
    {
        ModalOpened = false;
    }
}