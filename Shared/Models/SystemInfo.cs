namespace FileFlows.Shared.Models;

/// <summary>
/// Gets the system information about FileFlows
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// Gets the amount of memory used by FileFlows
    /// </summary>
    public long[] MemoryUsage { get; set; }
    
    /// <summary>
    /// Gets the how much CPU is used by FileFlows
    /// </summary>
    public float[] CpuUsage { get; set; }

    /// <summary>
    /// Gets or sets if the system is paused
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets when the system is paused until
    /// </summary>
    public DateTime PausedUntil { get; set; }

    /// <summary>
    /// Gets the current time on the server
    /// </summary>
    public DateTime CurrentTime => DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the status of the system
    /// </summary>
    public List<NodeStatus> NodeStatuses { get; set; }
    
}

/// <summary>
/// Node statuse
/// </summary>
public class NodeStatus
{
    /// <summary>
    /// Gets or sets the UID of the node
    /// </summary>
    public Guid Uid { get; set; }
    /// <summary>
    /// Gets or sets the name of the node
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets if this node is out of schedule
    /// </summary>
    public bool OutOfSchedule { get; set; }
    /// <summary>
    /// Gets or sets the number of minutes until the node is in schedule
    /// </summary>
    public int? MinutesUntilInSchedule { get; set; }
    /// <summary>
    /// Gets or sets the version of the node
    /// </summary>
    public string Version { get; set; }
}