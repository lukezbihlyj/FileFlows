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

    /// <summary>
    /// Reference to JS Report class
    /// </summary>
    //private IJSObjectReference jsObjectReference;
    
    private List<ExecutedNode> ExecutedNodes = [];
    private List<FlowElement> FlowElements = [];
    private readonly Guid Uid = Guid.NewGuid();
    private bool initDone;
    private bool ready = false;
    private List<FlowPart> parts = [];


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        (Model as IDictionary<string, object> ?? new Dictionary<string, object>()).TryGetValue(nameof(LibraryFile.ExecutedNodes), out var oExecutedNodes);
        if(oExecutedNodes == null)
            return;
        if(oExecutedNodes is JsonElement jsonElement)
            ExecutedNodes = jsonElement.Deserialize<List<ExecutedNode>>();
        else if(oExecutedNodes is List<ExecutedNode> list)
            ExecutedNodes = list;
        FlowElements = await ClientService.GetAllFlowElements();

        
        ready = true;
        StateHasChanged();
    }

    private ffFlowWrapper ffFlow;
    private IJSObjectReference? jsObjectReference;
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (jsObjectReference == null)
        {
            var dotNetObjRef = DotNetObjectReference.Create(this);
            var js = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
                $"./Components/Inputs/InputExecutedFlowElementsRenderer/InputExecutedFlowElementsRenderer.razor.js?v={Globals.Version}");
            jsObjectReference = await js.InvokeAsync<IJSObjectReference>("createExecutedFlowElementsRenderer", dotNetObjRef, this.Uid);
        }

        if (initDone == false && ExecutedNodes.Count > 0 && ready)
        {
            bool isVisible = await jsObjectReference.InvokeAsync<bool>("isElementVisible");
            if (isVisible == false)
                return;
            int height = await jsObjectReference.InvokeAsync<int>("getVisibleHeight") - 200;
            if (height < 200)
                height = 880;
            
            initDone = true;
            var flow = BuildFlow(height);
            ffFlow = await ffFlowWrapper.Create(jsRuntime, Uid, true);
            await ffFlow.InitModel(flow);
            await ffFlow.init(parts, FlowElements.ToArray());

            //Flow.Parts, FlowPage.Available);
            //await WaitForRender();
            await ffFlow.redrawLines();
        }
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
        for(int i=1;i<ExecutedNodes.Count;i++ )
        {
            var node = ExecutedNodes[i];
            var element = FlowElements.FirstOrDefault(x => x.Uid == node.NodeUid);
            var part = new FlowPart
            {
                Uid = nextUid,
                Name = node.NodeName,
                ReadOnly = true,
                Inputs = element?.Inputs ?? 0,
                Outputs = element?.Outputs ?? (node.NodeUid?.Contains(".Startup") == true ? 1 : 0),
                Type = element?.Type ?? FlowElementType.Input,
                Color = element?.CustomColor ?? "",
                Icon = element?.Icon ?? "",
                FlowElementUid = node.NodeUid,
                Label = node.NodeName,
                xPos = xPos,
                yPos = yPos
            };
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
                xPos += 220;
            }
        }
        Flow flow = new();
        flow.Parts = parts;
        return flow;
    }

    public async ValueTask DisposeAsync()
    {
        if (jsObjectReference != null)
        {
            await ffFlow.dispose();
            await jsObjectReference.DisposeAsync();
        }
    }
}