namespace FileFlows.Client.Helpers;

/// <summary>
/// Helper class for working with schedules
/// </summary>
public class ScheduleHelper
{
    /// <summary>
    /// Converts a number of seconds into a human readable string
    /// </summary>
    /// <param name="utcDate">the date when it is back in schedule in UTC</param>
    /// <returns>the human readable string</returns>
    public static string HumanReadable(DateTime? utcDate)
    {
        if (utcDate == null || utcDate < DateTime.UtcNow)
            return Translater.Instant("Pages.Dashboard.Widgets.Status.OutOfSchedule");
        int seconds = (int)utcDate.Value.Subtract(DateTime.UtcNow).TotalSeconds;
        return HumanReadable(seconds);
    }
    
    /// <summary>
    /// Converts a number of seconds into a human readable string
    /// </summary>
    /// <param name="seconds">the number of seconds</param>
    /// <returns>the human readable string</returns>
    public static string HumanReadable(int seconds)
    {
        if (seconds == 0)
            return string.Empty;
        
        // Calculate time units
        int minutes = seconds / 60;
        int hours = minutes / 60;
        
        // Translation logic
        if (minutes < 60) // Less than an hour
        {
            return Translater.Instant(
                "Pages.Dashboard.Widgets.Status.OutOfScheduleMinutes", 
                new { minutes });
        }
        if (hours < 24) // Less than a day
        {
            return Translater.Instant(
                "Pages.Dashboard.Widgets.Status.OutOfScheduleHours", 
                new { hours });
        }
        
        // More than a day
        var day = DateTime.Now.AddSeconds(seconds).ToString("dddd"); // Day of the week
        var time = RoundToNearest15Minutes(DateTime.Now.AddSeconds(seconds)).ToShortTimeString(); // Time of day
        return Translater.Instant(
            "Pages.Dashboard.Widgets.Status.OutOfScheduleDay", 
            new { day, time });
    }
    
    static DateTime RoundToNearest15Minutes(DateTime dateTime)
    {
        // Calculate total minutes from the start of the hour
        int totalMinutes = (int)(dateTime.TimeOfDay.TotalMinutes + 7.5); // Add 7.5 minutes for rounding
        int roundedMinutes = (totalMinutes / 15) * 15; // Get the nearest 15-minute interval
        return dateTime.Date.AddMinutes(roundedMinutes);
    }
}