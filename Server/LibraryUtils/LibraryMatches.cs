using FileFlows.Shared.Models;

namespace FileFlows.Server.LibraryUtils;

/// <summary>
/// Library matches methods
/// </summary>
public class LibraryMatches
{
    /// <summary>
    /// Tests if a file in a library matches the detection settings for hte library
    /// </summary>
    /// <param name="library">the library to test</param>
    /// <param name="info">the info for the file or folder to test</param>
    /// <param name="size">the size of the file or folder in bytes</param>
    /// <returns>true if matches detection, otherwise false</returns>
    public static bool MatchesDetection(Library library, FileSystemInfo info, long size)
    {
        if(MatchesValue((int)DateTime.UtcNow.Subtract(info.CreationTimeUtc).TotalMinutes, library.DetectFileCreation, library.DetectFileCreationLower, library.DetectFileCreationUpper, info.CreationTimeUtc, library.DetectFileCreationDate) == false)
            return false;

        if(MatchesValue((int)DateTime.UtcNow.Subtract(info.LastWriteTimeUtc).TotalMinutes, library.DetectFileLastWritten, library.DetectFileLastWrittenLower, library.DetectFileLastWrittenUpper, info.LastWriteTimeUtc, library.DetectFileLastWrittenDate) == false)
            return false;
        
        if(MatchesValue(size, library.DetectFileSize, library.DetectFileSizeLower, library.DetectFileSizeUpper, null, null) == false)
            return false;
        
        return true;
        
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="range"></param>
    /// <param name="low"></param>
    /// <param name="high"></param>
    /// <param name="dateValue"></param>
    /// <param name="dateTest"></param>
    /// <returns></returns>
    private static bool MatchesValue(long value, MatchRange range, long low, long high, DateTime? dateValue, DateTime? dateTest)
    {
        if (range == MatchRange.Any)
            return true;
        if (range == MatchRange.After)
            return dateValue > dateTest;
        if (range == MatchRange.Before)
            return dateValue < dateTest;
        
        if (range == MatchRange.GreaterThan)
            return value > low;
        if (range == MatchRange.LessThan)
            return value < low;
        bool between = value >= low && value <= high;
        return range == MatchRange.Between ? between : !between;
    }

}