﻿namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// The time helper provides help methods regarding scheduling
/// </summary>
public class TimeHelper
{
    /// <summary>
    /// Gets the integer index of the current time quarter
    /// A time quarter is a 15minute block, starting on Sunday at midnight.
    /// </summary>
    /// <returns>The integer index of the current time quarter</returns>
    public static int GetCurrentQuarter()
    {
        DateTime date = DateTime.Now; // schedules are local to the server time, only thing left in non UTC dates

        int quarter = (((int)date.DayOfWeek) * 96) + (date.Hour * 4);
        if (date.Minute >= 45)
            quarter += 3;
        else if (date.Minute >= 30)
            quarter += 2;
        else if (date.Minute >= 15)
            quarter += 1;
        return quarter;
    }

    /// <summary>
    /// Gets the integer index of the a specific date
    /// A time quarter is a 15minute block, starting on Sunday at midnight.
    /// </summary>
    /// <param name="date">the date to get the quarter index for</param>
    /// <returns>The integer index of the time quarter</returns>
    public static int GetQuarter(DateTime date)
    {
        int quarter = (((int)date.DayOfWeek) * 96) + (date.Hour * 4);
        if (date.Minute >= 45)
            quarter += 3;
        else if (date.Minute >= 30)
            quarter += 2;
        else if (date.Minute >= 15)
            quarter += 1;
        return quarter;
    }

    /// <summary>
    /// Checks if the current time is in the supplied schedule
    /// </summary>
    /// <param name="schedule">The schedule to check</param>
    /// <returns>true if the current time is within the schedule</returns>
    public static bool InSchedule(string schedule)
    {
        if (string.IsNullOrEmpty(schedule) || schedule.Length != 672)
            return true; // bad schedule treat as always in schedule

        int quarter = GetCurrentQuarter();
        return schedule[quarter] == '1';
    }

    /// <summary>
    /// Gets the date until in schedule
    /// </summary>
    /// <param name="schedule">the schedule</param>
    /// <returns>the data when it will be in schedule</returns>
    public static DateTime? UtcDateUntilInSchedule(string schedule)
    {
        int? seconds = SecondsOutOfSchedule(schedule);
        if (seconds == 0)
            return DateTime.MinValue;
        if (seconds == null)
            return null;
        return DateTime.UtcNow.AddSeconds(seconds.Value);
    }

    /// <summary>
    /// Gets the number of seconds until the next quarter in the schedule
    /// </summary>
    /// <param name="schedule">the schedule to check</param>
    /// <returns>the number of seconds until this is in schedule</returns>
    public static int? SecondsOutOfSchedule(string schedule)
    {
        if (string.IsNullOrEmpty(schedule) || schedule.Length != 672)
            return 0; // bad schedule treat as always in schedule

        int currentQuarter = GetCurrentQuarter();
        if(schedule[currentQuarter] == '1')
            return 0; // in schedule
        schedule = schedule[currentQuarter..] + schedule[..currentQuarter]; // rotate schedule to current time
        
        // need to adjust for the minutes now past the quarter, so 43 would be 13 minutes past 30, 52 would be 7 past 45, 15 woud be 0 past 15 
        int secondsPastQuarter = (DateTime.Now.Minute % 15) * 60 + DateTime.Now.Second;

        for (int i = 1; i < 672; i++)
        {
            if (schedule[i] == '1')
            {
                // -1 here since this is in schedule, so we dont count it
                return (i * 15 * 60) - secondsPastQuarter;
            }
        }

        // no '1', so never in schedule
        return null; 
    }

}