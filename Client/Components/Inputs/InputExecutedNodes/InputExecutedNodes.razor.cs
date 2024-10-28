using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Humanizer;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input Executed Nodes
/// </summary>
public partial class InputExecutedNodes: ExecuteFlowElementView
{
    /// <summary>
    /// Translated labels
    /// </summary>
    private string lblName, lblFlowElement, lblTime, lblOutput;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblName = Translater.Instant("Labels.Name");
        lblFlowElement = Translater.Instant("Labels.FlowElement");
        lblTime = Translater.Instant("Labels.Time");
        lblOutput = Translater.Instant("Labels.Output");
    }
}
