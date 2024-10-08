namespace FileFlows.Shared.Models;

/// <summary>
/// Represents the hardware information of the system.
/// </summary>
public class HardwareInfo
{
    /// <summary>
    /// Gets or sets the operating system name.
    /// </summary>
    public string OperatingSystem { get; set; }

    /// <summary>
    /// Gets or sets the operating system type
    /// </summary>
    public OperatingSystemType OperatingSystemType { get; set; }
    
    /// <summary>
    /// Gets or sets the operating system version.
    /// </summary>
    public string OperatingSystemVersion { get; set; }

    /// <summary>
    /// Gets or sets the architecture of the operating system.
    /// </summary>
    public string Architecture { get; set; }

    /// <summary>
    /// Gets or sets the list of GPUs installed on the system.
    /// </summary>
    public List<GpuInfo> Gpus { get; set; } = new();

    /// <summary>
    /// Gets or sets the name of the processor.
    /// </summary>
    public string Processor { get; set; }
    
    /// <summary>
    /// Gets or sets the memory size of the system.
    /// </summary>
    public long Memory { get; set; }

    /// <summary>
    /// Gets or sets the number of processor cores.
    /// </summary>
    public int CoreCount { get; set; }
    
    /// <summary>
    /// Returns a string representation of the hardware information.
    /// </summary>
    /// <returns>A formatted string containing the hardware information.</returns>
    public override string ToString()
    {
        var gpuInfoStrings = Gpus?.Select(gpu => gpu.ToString())?.ToArray() ?? [];
        return $"Operating System: {OperatingSystem}\n" +
               $"OS Version: {OperatingSystemVersion}\n" +
               $"Architecture: {Architecture}\n" +
               $"Processor: {Processor}\n" +
               $"Core Count: {CoreCount}\n" +
               $"GPUs:\n{string.Join("\n", gpuInfoStrings)}";
    }
}

/// <summary>
/// Represents the information of a GPU.
/// </summary>
public class GpuInfo
{
    /// <summary>
    /// Gets or sets the vendor/make of the GPU.
    /// </summary>
    public string Vendor { get; set; }

    /// <summary>
    /// Gets or sets the model of the GPU.
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the memory size of the GPU.
    /// </summary>
    public long Memory { get; set; }

    /// <summary>
    /// Gets or sets the driver version of the GPU.
    /// </summary>
    public string DriverVersion { get; set; }
    
    /// <summary>
    /// Returns a string representation of the GPU information.
    /// </summary>
    /// <returns>A formatted string containing the GPU information.</returns>
    public override string ToString()
    {
        return $"Vendor: {Vendor}\n" +
               $"Model: {Model}\n" +
               $"Memory: {Memory} bytes\n" +
               $"Driver Version: {DriverVersion}";
    }
}