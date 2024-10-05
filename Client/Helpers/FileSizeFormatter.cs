namespace FileFlows.Client.Helpers;

/// <summary>
/// File Size Formatter
/// </summary>
public class FileSizeFormatter
{
    static string lblIncrease, lblDecrease;

    /// <summary>
    /// Constructs the file size formatter
    /// </summary>
    static FileSizeFormatter()
    {
        lblIncrease = Translater.Instant("Labels.Increase");
        lblDecrease= Translater.Instant("Labels.Decrease");
    }
    
    /// <summary>
    /// Formats a byte value as a string
    /// </summary>
    /// <param name="size">The size in bytes</param>
    /// <param name="decimalPoints">the number of decimal points</param>
    /// <returns>The size in a formatted string</returns>
    public static string FormatSize(long size, int decimalPoints = 2)
        => FileFlows.Shared.Formatters.FileSizeFormatter.Format(size, decimalPoints);

    /// <summary>
    /// Formats the shrinkage of a file
    /// </summary>
    /// <param name="original">the original size of the file</param>
    /// <param name="final">the final size of the file</param>
    /// <returns>the shrinkage string</returns>
    public static string FormatShrinkage(long original, long final)
    {
        long diff = Math.Abs(original - final);
        return FormatSize(diff) + (original < final ? " " + lblIncrease : " " + lblDecrease) +
                        "\n" + FormatSize(final) + " / " + FormatSize(original);
    }
}