using System.Diagnostics;
using System.Runtime.InteropServices;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Provides methods to retrieve hardware information across different operating systems.
/// </summary>
public class HardwareInfoService
{
    /// <summary>
    /// Gets the hardware information of the system.
    /// </summary>
    /// <returns>A <see cref="HardwareInfo"/> instance containing the system's hardware information.</returns>
    public HardwareInfo GetHardwareInfo()
    {
        var hardwareInfo = new HardwareInfo
        {
            OperatingSystem = GetOperatingSystem(),
            OperatingSystemVersion = Environment.OSVersion.VersionString,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            Gpus = GetGPUs(),
            Processor = GetProcessor(),
            CoreCount = Environment.ProcessorCount
        };

        return hardwareInfo;
    }

    /// <summary>
    /// Retrieves the operating system name.
    /// </summary>
    /// <returns>The name of the operating system.</returns>
    private string GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "MacOS";
        return "Unknown OS";
    }

    /// <summary>
    /// Retrieves the names of the GPUs installed on the system along with their details.
    /// </summary>
    /// <returns>A list of <see cref="GpuInfo"/> instances representing the GPUs.</returns>
    private List<GpuInfo> GetGPUs()
    {
        var gpus = new List<GpuInfo>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Using wmic command to get GPU info
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "wmic";
                process.StartInfo.Arguments = "path win32_VideoController get name, adapterram, driverversion";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                foreach (var line in output.Split(Environment.NewLine))
                {
                    var parts = line.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        gpus.Add(new GpuInfo
                        {
                            Vendor = parts[0].Trim(),
                            Model = parts[1].Trim(),
                            Memory = long.Parse(parts[2].Trim()),
                            DriverVersion = parts[3].Trim()
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Check for both NVIDIA and AMD GPUs
            // You may need additional permissions to access some of the files
            try
            {
                // NVIDIA GPU detection
                var result =ExecuteCommand("lspci | grep -i nvidia");
                if (result.IsFailed == false)
                {
                    var nvidiaInfo = result.Value.Trim();
                    foreach (var line in nvidiaInfo.Split(Environment.NewLine))
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains("VGA"))
                        {
                            var gpu = ParseNvidiaGpu(line);
                            if(gpu != null)
                                gpus.Add(gpu);
                        }
                    }
                }

                // AMD GPU detection
                var amdInfoResult = ExecuteCommand("lspci | grep -i amd");
                if (amdInfoResult.IsFailed == false)
                {
                    var amdInfo = amdInfoResult.Value.Trim();
                    foreach (var line in amdInfo.Split(Environment.NewLine))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            gpus.Add(new GpuInfo
                            {
                                Vendor = "AMD",
                                Model = line.Trim(),
                                Memory = GetAmdMemory(line.Trim()),
                                DriverVersion = GetAmdDriverVersion()
                            });
                        }
                    }
                }
            }
            catch
            {
                // Handle if the command fails
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Using system_profiler to get GPU info on macOS
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "system_profiler";
                process.StartInfo.Arguments = "SPDisplaysDataType";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                foreach (var line in output.Split(Environment.NewLine))
                {
                    if (line.Contains("Chipset Model"))
                    {
                        var model = line.Split(':')[1].Trim();
                        gpus.Add(new GpuInfo
                        {
                            Vendor = "Apple/Intel",
                            Model = model,
                            Memory = GetMacMemory(),
                            DriverVersion = GetMacDriverVersion()
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        return gpus;
    }
    
    /// <summary>
    /// Parses the NVIDIA GPU info
    /// </summary>
    /// <param name="gpuInfo">the line to parse</param>
    /// <returns>The GpuInfo or null if invalid</returns>
    private GpuInfo? ParseNvidiaGpu(string gpuInfo)
    {
        // Example input: "01:00.0 VGA compatible controller: NVIDIA Corporation AD107M [GeForce RTX 4060 Max-Q / Mobile] (rev a1)"
    
        var parts = gpuInfo.Split(new[] { ':' }, 2); // Split at the first colon
        if (parts.Length < 2)
        {
            return null; // Not a valid format
        }

        var description = parts[1].Trim(); // Get the description part

        // Extract model
        var modelStartIndex = description.IndexOf('[');
        var modelEndIndex = description.IndexOf(']');
        var model = modelStartIndex >= 0 && modelEndIndex > modelStartIndex
            ? description.Substring(modelStartIndex + 1, modelEndIndex - modelStartIndex - 1).Trim()
            : "Unknown Model";

        return new GpuInfo
        {
            Vendor = "NVIDIA",
            Model = model,
            Memory = GetNvidiaMemory(gpuInfo), // Call your existing method to get memory
            DriverVersion = GetNvidiaDriverVersion() // Call your existing method to get driver version
        };
    }


    /// <summary>
    /// Executes a command
    /// </summary>
    /// <param name="command">the command to execute</param>
    /// <returns>the result of the command</returns>
    private Result<string> ExecuteCommand(string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            return process.StandardOutput.ReadToEnd();
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }
    
    /// <summary>
    /// Retrieves the memory of NVIDIA GPUs in bytes.
    /// </summary>
    /// <param name="gpuInfo">Information about the GPU.</param>
    /// <returns>The memory size of the GPU in bytes.</returns>
    private long GetNvidiaMemory(string gpuInfo)
    {
        // Use the name of the GPU to query its memory size
        // Command to get memory size for a specific GPU based on its name
        string command = $"nvidia-smi --query-gpu=memory.total --format=csv,noheader,nounits -i {gpuInfo.Split(' ')[0]}";

        // Execute the command and get the output
        var result = ExecuteCommand(command);
        if (result.IsFailed)
            return 0;
        string output = result.Value.Trim();

        // Try to parse the output as a long value
        if (long.TryParse(output, out long memorySize))
        {
            return memorySize * 1024 * 1024; // Convert MB to Bytes
        }

        return 0; // Default to 0 if parsing fails
    }

    /// <summary>
    /// Retrieves the driver version of NVIDIA GPUs.
    /// </summary>
    /// <returns>The driver version of the NVIDIA GPU.</returns>
    private string GetNvidiaDriverVersion()
    {
        var result = ExecuteCommand("nvidia-smi --query-gpu=driver_version --format=csv,noheader");
        return result.IsFailed ? string.Empty : result.Value.Trim();
    }

    /// <summary>
    /// Retrieves the memory of AMD GPUs in bytes.
    /// </summary>
    /// <param name="gpuInfo">Information about the GPU.</param>
    /// <returns>The memory size of the AMD GPU in bytes.</returns>
    private long GetAmdMemory(string gpuInfo)
    {
        // Command to get memory size for a specific AMD GPU based on its name
        string command = $"lspci -v -s {gpuInfo} | grep 'Memory size' | awk '{{print $3}}'";

        // Execute the command and get the output
        var result = ExecuteCommand(command);
        if (result.IsFailed)
            return 0;
        string output = result.Value.Trim();

        // Try to parse the output as a long value (assumes output is in MB)
        if (long.TryParse(output, out long memorySize))
        {
            return memorySize * 1024 * 1024; // Convert MB to Bytes
        }

        return 0; // Default to 0 if parsing fails
    }

    /// <summary>
    /// Retrieves the driver version of AMD GPUs.
    /// </summary>
    /// <returns>The driver version of the AMD GPU.</returns>
    private string GetAmdDriverVersion()
    {
        // Command to get the driver version for AMD GPUs
        var result = ExecuteCommand("glxinfo | grep 'OpenGL version'");
        return result.IsFailed ? string.Empty : result.Value.Trim();
    }


    /// <summary>
    /// Retrieves the memory size of the GPU on macOS in bytes.
    /// </summary>
    /// <returns>The memory size of the macOS GPU in bytes.</returns>
    private long GetMacMemory()
    {
        // Command to get GPU memory size on macOS
        string command = "system_profiler SPDisplaysDataType | grep 'VRAM' | awk '{print $2}'";

        // Execute the command and get the output
        var result = ExecuteCommand(command);
        if (result.IsFailed)
            return 0;
        string output = result.Value.Trim();

        // Try to parse the output as a long value (assumes output is in MB)
        if (long.TryParse(output, out long memorySize))
        {
            return memorySize * 1024 * 1024; // Convert MB to Bytes
        }

        return 0; // Default to 0 if parsing fails
    }

    /// <summary>
    /// Retrieves the driver version of the GPU on macOS.
    /// </summary>
    /// <returns>The driver version of the macOS GPU.</returns>
    private string GetMacDriverVersion()
    {
        // Command to get GPU driver version on macOS
        string command = "system_profiler SPDisplaysDataType | grep 'Driver Version' | awk '{print $3}'";

        // Execute the command and get the output
        var result = ExecuteCommand(command);
        if(result.IsFailed)
            return string.Empty;
        string output = result.Value.Trim();

        return string.IsNullOrEmpty(output) ? "Unknown Driver" : output; // Return "Unknown Driver" if output is empty
    }


    /// <summary>
    /// Retrieves the name of the processor.
    /// </summary>
    /// <returns>The name of the processor.</returns>
    private string GetProcessor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Using wmic command to get Processor info
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "wmic";
                process.StartInfo.Arguments = "cpu get name";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                return process.StandardOutput.ReadToEnd().Split(Environment.NewLine)[1].Trim();
            }
            catch (Exception)
            {
                // Ignored
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Using /proc/cpuinfo to get Processor info
            var result = ExecuteCommand("cat /proc/cpuinfo | grep 'model name' | uniq | awk -F: '{print $2}'");
            if (result.IsFailed == false)
                return result.Value.Trim();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Using sysctl to get Processor info
            var result = ExecuteCommand("sysctl -n machdep.cpu.brand_string");
            if (result.IsFailed == false)
                return result.Value.Trim();
        }
        return "Unknown Processor";
    }
}
