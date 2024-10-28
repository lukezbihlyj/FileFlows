using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Flow Icon Value component
/// </summary>
public partial class FlowIconValue : ComponentBase
{
    private string _icon = string.Empty;
    private string _color = string.Empty;
    private string _value = string.Empty;
    private static string _InternalProcessingNode;

    static FlowIconValue()
    {
        _InternalProcessingNode = Translater.Instant("Labels.InternalProcessingNode");
    }
    
    /// <summary>
    /// Gets or sets the Icon
    /// </summary>
    [Parameter]
    public string Icon
    {
        get => _icon;
        set => _icon = value;
    }

    /// <summary>
    /// Gets or set the string value
    /// </summary>
    [Parameter] public string Value 
    {
        get => _value;
        set => _value = value;
    }
    
    /// <summary>
    /// Gets or sets the color
    /// </summary>
    [Parameter] public string Color
    {
        get => _color;
        set => _color = value;
    }

    
    /// <summary>
    /// Gets or sets the on click event
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }
    
    /// <summary>
    /// Gets if this is clickable
    /// </summary>
    private bool Clickable => OnClick.HasDelegate;
    
    /// <summary>
    /// Handles the click event
    /// </summary>
    private void ClickHandler()
        => _ = OnClick.InvokeAsync();

    protected override void OnParametersSet()
    {
        _icon = _icon.ToLowerInvariant();
        if (_icon == "library")
        {
            _icon = "fas fa-folder";
            _color = _color?.EmptyAsNull() ?? "green";
        }
        else if (_icon == "flow")
        {
            _icon = "fas fa-sitemap";
            _color = _color?.EmptyAsNull() ?? "blue";
        }
        else if (_icon.StartsWith("node"))
        {
            _color = _color?.EmptyAsNull() ?? "purple";
            _icon = _icon switch
            {
                "node:docker" => "fab fa-docker",
                "node:windows" => "fab fa-windows",
                "node:apple" or "node:mac" or "node:macos" => "fab fa-apple",
                "node:linux" => "fab fa-linux",
                _ => "fas fa-desktop"
            };
            if (_value == "FileFlowsServer")
                _value = _InternalProcessingNode;
        }
        
    }
}