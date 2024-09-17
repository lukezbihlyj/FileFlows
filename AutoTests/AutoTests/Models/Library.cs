namespace FileFlowsTests.Models;

public class Library
{
    public string Name { get; init; }
    public string? Template { get; set; }
    public string Path { get; init; }
    public string Flow { get; init; }
    public LibraryPriority? Priority { get; set; }
    public LibraryProcessingOrder? ProcessingOrder { get; set; }
    public int? HoldMinutes { get; set; }
    public bool? Enabled { get; set; }
    public string? Filter { get; set; }
    public string ExclusionFilter { get; set; }
    public bool? UseFingerprinting { get; set; }
    public bool? Scan { get; set; }
}

public enum LibraryPriority
{
    Lowest,
    Low, 
    Normal,
    High,
    Highest
}

public enum LibraryProcessingOrder
{
    AsFound,
    LargestFirst,
    NewestFirst,
    Random,
    SmallestFirst
}