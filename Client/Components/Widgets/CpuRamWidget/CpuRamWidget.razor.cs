using System.Collections.Concurrent;
using FileFlows.Client.Helpers;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Widgets;

public partial class CpuRamWidget : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    
    private int _Mode = 0;
    private string Color = "yellow";
    private string Label = "CPU";
    private double CpuValue = 0;
    private double RamValue = 0;
    private double CpuMax = 0;
    private double RamMax = 0;
    private string Max;
    private string Value;
    private double[] Data = [];//[10, 20, 30, 20, 15, 16, 27, 45.34, 41.2, 38.2];
    private ConcurrentQueue<double> CpuValues = new();
    private ConcurrentQueue<double> MemoryValues = new();
    /// <summary>
    /// Gets or sets the selected mode
    /// </summary>
    private int Mode
    {
        get => _Mode;
        set
        {
            _Mode = value;
            SetValues();

            StateHasChanged();
        }
    }

    /// <summary>
    /// Gets if in the CPU mode
    /// </summary>
    private bool CpuMode => _Mode == 0;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ClientService.SystemInfoUpdated += OnSystemInfoUpdated;
    }

    /// <summary>
    /// Handles the system info updated event
    /// </summary>
    /// <param name="info">the system info</param>
    private void OnSystemInfoUpdated(SystemInfo info)
    {
        CpuValue = info.CpuUsage;
        RamValue = info.MemoryUsage;
        CpuValues.Enqueue(CpuValue);
        MemoryValues.Enqueue(RamValue);
        while (CpuValues.Count > 30)
            CpuValues.TryDequeue(out _);

        while (MemoryValues.Count > 30)
            MemoryValues.TryDequeue(out _);
        
        CpuMax = !CpuValues.IsEmpty ? CpuValues.Max() : 0;
        RamMax = !MemoryValues.IsEmpty ? MemoryValues.Max() : 0;

        SetValues();

        StateHasChanged();
    }

    private void SetValues()
    {
        
        if (CpuMode)
        {
            Color = "yellow";
            Label = "CPU";
            Value = $"{CpuValue:F1}%";
            Max = $"{CpuMax:F1}% Peak";
            Data = CpuValues.ToArray();
        }
        else
        {
            Color = "purple";
            Label = "RAM";
            Value = FileSizeFormatter.FormatSize((long)RamValue);
            Max = FileSizeFormatter.FormatSize((long)RamMax) + " Peak";
            Data = MemoryValues.ToArray();
        }
    }

    /// <summary>
    /// Disposes of the object
    /// </summary>
    public void Dispose()
    {
        ClientService.SystemInfoUpdated -= OnSystemInfoUpdated;
    }
}