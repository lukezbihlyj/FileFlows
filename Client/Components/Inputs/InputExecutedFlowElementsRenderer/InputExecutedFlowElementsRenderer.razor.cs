using System.Text.Json;
using FileFlows.Client.Pages;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Flow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Executed Flow Elements Renderer Viewer
/// </summary>
public partial class InputExecutedFlowElementsRenderer : ComponentBase, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the model
    /// </summary>
    [Parameter] public ExpandoObject Model { get; set; }
    
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] private ClientService ClientService { get; set; }
    
    private List<ExecutedNode> ExecutedNodes = [];
    private List<FlowElement> FlowElements = [];
    private readonly Guid Uid = Guid.NewGuid();
    private List<FlowPart> parts = [];
    private bool _needsRendering = false;

    private ffFlowWrapper ffFlow;
    private IJSObjectReference? jsObjectReference;


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _ = Initialize();
    }

    private async Task Initialize()
    {
        (Model as IDictionary<string, object> ?? new Dictionary<string, object>()).TryGetValue(nameof(LibraryFile.ExecutedNodes), out var oExecutedNodes);
        if(oExecutedNodes == null)
            return;
        if(oExecutedNodes is JsonElement jsonElement)
            ExecutedNodes = jsonElement.Deserialize<List<ExecutedNode>>();
        else if(oExecutedNodes is List<ExecutedNode> list)
            ExecutedNodes = list;
        FlowElements = await ClientService.GetAllFlowElements();

        var dotNetObjRef = DotNetObjectReference.Create(this);
        var js = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
            $"./Components/Inputs/InputExecutedFlowElementsRenderer/InputExecutedFlowElementsRenderer.razor.js?v={Globals.Version}");
        jsObjectReference = await js.InvokeAsync<IJSObjectReference>("createExecutedFlowElementsRenderer", dotNetObjRef, this.Uid);
        
        int height = await jsObjectReference.InvokeAsync<int>("getVisibleHeight") - 200;
        if (height < 200)
            height = 880;
        //ready = true;
        var flow = BuildFlow(height);
        ffFlow = await ffFlowWrapper.Create(jsRuntime, Uid, true);
        await ffFlow.InitModel(flow);
        await ffFlow.init(parts, FlowElements.ToArray());

        //Flow.Parts, FlowPage.Available);
        await WaitForRender();
        await ffFlow.redrawLines();
        await jsObjectReference.InvokeVoidAsync("captureDoubleClicks");
        StateHasChanged();
    }
    
    /// <summary>
    /// Waits for the component to render
    /// </summary>
    protected async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        _needsRendering = false;
    }

    /// <summary>
    /// Builds the flow
    /// </summary>
    /// <param name="height">the maximum height of the flow</param>
    /// <returns>the built flow</returns>
    private Flow BuildFlow(int height)
    {
        int xPos = 50;
        int yPos = 20;
        Guid nextUid = Guid.NewGuid();
        for(int i=0;i<ExecutedNodes.Count;i++ )
        {
            var node = ExecutedNodes[i];
            var element = FlowElements.FirstOrDefault(x => x.Uid == node.NodeUid);
            var part = new FlowPart
            {
                Uid = nextUid,
                Name = node.NodeName,
                ReadOnly = true,
                Inputs = i > 0 ? 1 : 0,
                Outputs = element?.Outputs ?? (node.NodeUid?.Contains(".Startup") == true ? 1 : 0),
                Type = element?.Type ?? FlowElementType.Input,
                FlowElementUid = node.NodeUid,
                Label = node.NodeName,
                xPos = xPos,
                yPos = yPos
            };
            SetIcon(part, node, element);
            if(part.Outputs < node.Output)
                part.Outputs = node.Output;
            
            nextUid = Guid.NewGuid();
            if(i < ExecutedNodes.Count - 1)
            {
                part.OutputConnections =
                [
                    new ()
                    {
                        Input = 1,
                        Output = node.Output,
                        InputNode = nextUid
                    }
                ];
            }
            parts.Add(part);
            yPos += 120;
            if (yPos > height)
            {
                yPos = 20;
                xPos += 250;
            }
        }
        Flow flow = new();
        flow.Parts = parts;
        return flow;
    }

    /// <summary>
    /// Sets the icon for the element
    /// </summary>
    /// <param name="element">the element</param>
    private void SetIcon(FlowPart part, ExecutedNode node, FlowElement? element)
    {
        if (string.IsNullOrWhiteSpace(element?.Icon) == false)
        {
            part.Icon = element.Icon;
            if (string.IsNullOrWhiteSpace(element.CustomColor) == false)
                part.CustomColor = element.CustomColor;
        }
        else if (node.NodeUid?.Contains(".Startup") == true)
        {
            part.Icon = "fas fa-sitemap";
            part.CustomColor = "#a428a7";
        }
        else if (node.NodeUid?.Contains(".RunnerFlowElements.FileDownloader") == true)
        {
            part.Icon = "fas fa-download";
            part.CustomColor = "#a428a7";
        }
    }

    /// <summary>
    /// A flow element was double clicked
    /// </summary>
    /// <param name="uid">the UID of the flow element</param>
    [JSInvokable]
    public void OnDoubleClick(string uid)
    {
        Logger.Instance.ILog("Double clicked on: " + uid);
    }

    public async ValueTask DisposeAsync()
    {
        if (jsObjectReference != null)
        {
            await ffFlow.dispose();
            await jsObjectReference.InvokeVoidAsync("dispose");
            await jsObjectReference.DisposeAsync();
        }
    }
}