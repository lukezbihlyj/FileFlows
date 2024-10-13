using System.Text.Json;
using FileFlows.Client.Pages;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Flow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Flow Viewer
/// </summary>
public partial class InputExecutedFlowsRenderer : ComponentBase, IAsyncDisposable
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
    
    private List<ExecutedNode> ExecutedFlowElements = [];
    private LibraryFileAdditional Additional = new();
    private List<FlowElement> FlowElements = [];
    private readonly Guid Uid = Guid.NewGuid();
    private bool initDone;
    private bool ready = false;
    private Flow builtFlow;


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var dict = (Model as IDictionary<string, object> ?? new Dictionary<string, object>());
        if (dict.TryGetValue(nameof(LibraryFile.ExecutedNodes), out var oExecutedFlowElements) == false ||
            oExecutedFlowElements == null)
            return;
        if (dict.TryGetValue(nameof(LibraryFile.Additional), out var oAdditional) == false ||
            oAdditional == null)
            return;

        ExecutedFlowElements = oExecutedFlowElements switch
        {
            JsonElement jsExecutedFlowElements => jsExecutedFlowElements.Deserialize<List<ExecutedNode>>(),
            List<ExecutedNode> listElements => listElements,
            _ => ExecutedFlowElements
        };
        Additional = oAdditional switch
        {
            JsonElement jeExecutedFlows => jeExecutedFlows.Deserialize<LibraryFileAdditional>(),
            LibraryFileAdditional additional => additional,
            _ => Additional
        };

        if(Additional?.ExecutedFlows == null || Additional.ExecutedFlows.Count == 0 || ExecutedFlowElements.Count == 0)
            return;

        FlowElements = await ClientService.GetAllFlowElements();

        builtFlow = BuildFlow();
        ready = true;
        StateHasChanged();
    }

    /// <summary>
    /// Builds the complete flow from the executed parts
    /// </summary>
    private Flow? BuildFlow()
    {
        var allParts = Additional.ExecutedFlows.SelectMany(x => x.Parts).ToDictionary(x => x.Uid, x => x);
        
        Flow builtFlow = new();
        builtFlow.Parts = [];
        float xOffset = 0;
        float yOffset = 0;
        var currentFlow = Additional.ExecutedFlows[0];
        FlowPart? last = null;
        ExecutedNode? lastExecuted = null;
        
        foreach (var executed in ExecutedFlowElements)
        {
            var part = allParts.GetValueOrDefault(executed.FlowPartUid);
            if (part == null)
                break;
            if (IsStartup(part))
            {
                part.Icon = "fas fa-sitemap";
                part.CustomColor = "#a428a7";
            }
            else if (IsDownloader(part))
            {
                part.Icon = "fas fa-download";
                part.CustomColor = "#a428a7";
            }

            var flow = Additional.ExecutedFlows.First(x => x.Parts.Contains(part));
            if (flow != currentFlow)
            {
                // it was a subflow/goto 
                if (last != null && IsStartup(last))
                {
                    last.xPos = part.xPos;
                    last.yPos = part.yPos;
                    if (last.xPos > 50)
                        last.xPos = 50;
                    if (last.yPos > 20)
                        last.yPos = 20;
                    yOffset = last.yPos - part.yPos + 100;
                    xOffset = last.xPos - part.xPos;
                }
                else if(last != null && part is { xPos: 0, yPos: 0 })
                {
                    // could be s startup flow element like the downloader
                    part.yPos = last.yPos - part.yPos + 100;
                    part.xPos = last.xPos;
                }
                else
                {
                    // last really is the previous connection, subflows arent recorded, so we need to get the last part of the previous flow
                    var lastConnection = last.OutputConnections.FirstOrDefault(x => x.Output == lastExecuted.Output);
                    if (lastConnection != null)
                    {
                        var lastPartActual = allParts.GetValueOrDefault(lastConnection.InputNode);
                        xOffset += (lastPartActual?.xPos ?? last?.xPos ?? 0) - part.xPos;
                        yOffset += (lastPartActual?.yPos ?? last?.yPos ?? 0) - part.yPos;
                    }
                }

                currentFlow = flow;
            }

            if (last != null)
            {
                part.Inputs = 1; // if there was one before, then this has an input
                last.OutputConnections =
                [
                    new()
                    {
                        Input = 1,
                        Output = lastExecuted.Output,
                        InputNode = part.Uid
                    }
                ];
            }

            var element = FlowElements.FirstOrDefault(x => x.Uid == part.FlowElementUid);
            if (element != null)
            {
                part.Icon = element.Icon;
                part.CustomColor = element.CustomColor;
            }

            part.xPos += xOffset;
            part.yPos += yOffset;
            
            builtFlow.Parts.Add(part);
            last = part;
            lastExecuted = executed;
        }
        Logger.Instance.ILog(builtFlow);
        return builtFlow;
    }
    
    /// <summary>
    /// If the flow part if the startup flow element
    /// </summary>
    /// <param name="part">the part to check</param>
    /// <returns>true if it's the startup flow element</returns>
    private bool IsStartup(FlowPart part)
        => part.FlowElementUid?.Contains(".Startup") == true;
    /// <summary>
    /// If the flow part if the downloader flow element
    /// </summary>
    /// <param name="part">the part to check</param>
    /// <returns>true if it's the downloader flow element</returns>
    private bool IsDownloader(FlowPart part)
        => part.FlowElementUid?.Contains(".RunnerFlowElements.FileDownloader") == true;


    private ffFlowWrapper ffFlow;
    private IJSObjectReference? jsObjectReference;
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (jsObjectReference == null)
        {
            var dotNetObjRef = DotNetObjectReference.Create(this);
            var js = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
                $"./Components/Inputs/InputExecutedFlowsRenderer/InputExecutedFlowsRenderer.razor.js?v={Globals.Version}");
            jsObjectReference = await js.InvokeAsync<IJSObjectReference>("createInputExecutedFlowsRenderer", dotNetObjRef, this.Uid);
        }

        if (initDone == false && builtFlow != null && ready)
        {
            bool isVisible = await jsObjectReference.InvokeAsync<bool>("isElementVisible");
            if (isVisible == false)
                return;
            initDone = true;
            ffFlow = await ffFlowWrapper.Create(jsRuntime, Uid, true);
            await ffFlow.InitModel(builtFlow);
            await ffFlow.init(builtFlow.Parts, FlowElements.ToArray());

            //Flow.Parts, FlowPage.Available);
            //await WaitForRender();
            await ffFlow.redrawLines();
        }
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