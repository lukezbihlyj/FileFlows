using FileFlows.Plugin;
using FileFlows.Server.Hubs;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Dashboard File Overview Service
/// </summary>
public class DashboardFileOverviewService
{

    private readonly FairSemaphore updateFileDataSemaphore = new (1, 1);

    // Dictionary for last 24 hours, 7 days, and 31 days
    private readonly Dictionary<DateTime, DashboardFileData> last24HoursData = new();
    private readonly Dictionary<DateTime, DashboardFileData> last7DaysData = new();
    private readonly Dictionary<DateTime, DashboardFileData> last31DaysData = new();

    private readonly LibraryFileService _libraryFileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardFileOverviewService"/> class.
    /// </summary>
    /// <param name="libraryFileService">Service to interact with the library file database.</param>
    public DashboardFileOverviewService(LibraryFileService libraryFileService)
    {
        _libraryFileService = libraryFileService;
        InitializeDictionaries();
        RefreshAsync().Wait();
    }

    /// <summary>
    /// Initializes the dictionaries with empty data for the last 24 hours, 7 days, and 31 days.
    /// </summary>
    private void InitializeDictionaries()
    {
        // Initialize last 24 hours data
        for (int i = 0; i < 24; i++)
        {
            last24HoursData[DateTime.UtcNow.AddHours(-i)] = new DashboardFileData();
        }

        // Initialize last 7 days data
        for (int i = 0; i < 7; i++)
        {
            last7DaysData[DateTime.UtcNow.Date.AddDays(-i)] = new DashboardFileData();
        }

        // Initialize last 31 days data
        for (int i = 0; i < 31; i++)
        {
            last31DaysData[DateTime.UtcNow.Date.AddDays(-i)] = new DashboardFileData();
        }
    }

    /// <summary>
    /// Updates the file data for the current hour/day when a file finishes processing.
    /// </summary>
    /// <param name="file">The processed file to update the statistics for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateFileDataAsync(LibraryFile file)
    {
        await updateFileDataSemaphore.WaitAsync();
        try
        {
            long savings = file.OriginalSize - file.FinalSize;

            // Get the current hour and date
            var currentHour = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);
            var currentDate = DateTime.UtcNow.Date;

            // Update today's hourly data
            UpdateDictionary(last24HoursData, currentHour, 24, savings, file.OriginalSize, file.FinalSize);

            // Update 7-day bucket
            UpdateDictionary(last7DaysData, currentDate, 7, savings, file.OriginalSize, file.FinalSize);

            // Update 31-day bucket
            UpdateDictionary(last31DaysData, currentDate, 31, savings, file.OriginalSize, file.FinalSize);

            TriggerUpdateNotification();
        }
        finally
        {
            updateFileDataSemaphore.Release();
        }
    }

    /// <summary>
    /// Refreshes the cached file data from the database every 3 hours.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RefreshAsync()
    {
        var data = await _libraryFileService.Search(new()
        {
            Status = FileStatus.Processed,
            FinishedProcessingFrom = DateTime.UtcNow.AddDays(-31),
            FinishedProcessingTo = DateTime.UtcNow
        });

        foreach (var file in data)
        {
            long savings = file.OriginalSize - file.FinalSize;
            DateTime fileHour = file.ProcessingEnded.Date.AddHours(file.ProcessingEnded.Hour);
            DateTime fileDate = file.ProcessingEnded.Date;

            // Update today's hourly data
            UpdateDictionary(last24HoursData, fileHour, 24, savings, file.OriginalSize, file.FinalSize);

            // Update 7-day bucket
            UpdateDictionary(last7DaysData, fileDate, 7, savings, file.OriginalSize, file.FinalSize);

            // Update 31-day bucket
            UpdateDictionary(last31DaysData, fileDate, 31, savings, file.OriginalSize, file.FinalSize);
        }

        TriggerUpdateNotification();
    }

    /// <summary>
    /// Triggers an update notification
    /// </summary>
    private void TriggerUpdateNotification()
    {
        ClientServiceManager.Instance.FileOverviewUpdate(GetData());
    }

    /// <summary>
    /// Gets the file data for the last 24 hours.
    /// </summary>
    /// <returns>A dictionary containing file statistics for the last 24 hours.</returns>
    public Dictionary<DateTime, DashboardFileData> GetToday()
    {
        return new Dictionary<DateTime, DashboardFileData>(last24HoursData);
    }

    /// <summary>
    /// Gets the file data for the last 7 days.
    /// </summary>
    /// <returns>A dictionary containing file statistics for the last 7 days.</returns>
    public Dictionary<DateTime, DashboardFileData> GetLast7Days()
    {
        return new Dictionary<DateTime, DashboardFileData>(last7DaysData);
    }

    /// <summary>
    /// Gets the file data for the last 31 days.
    /// </summary>
    /// <returns>A dictionary containing file statistics for the last 31 days.</returns>
    public Dictionary<DateTime, DashboardFileData> GetLast31Days()
    {
        return new Dictionary<DateTime, DashboardFileData>(last31DaysData);
    }

    /// <summary>
    /// Helper method to update a dictionary and maintain its size limit.
    /// </summary>
    /// <param name="dataDictionary">The dictionary to update.</param>
    /// <param name="dateKey">The date key for the data.</param>
    /// <param name="maxSize">The maximum size of the dictionary.</param>
    /// <param name="savings">The file size savings to add.</param>
    /// <param name="originalSize">The original file size</param>
    /// <param name="finalSize">The final file size</param>
    private void UpdateDictionary(Dictionary<DateTime, DashboardFileData> dataDictionary, DateTime dateKey, int maxSize,
        long savings, long originalSize, long finalSize)
    {
        if (!dataDictionary.ContainsKey(dateKey))
        {
            // Remove oldest entry if the dictionary exceeds the max size
            if (dataDictionary.Count >= maxSize)
            {
                var oldestDate = dataDictionary.Keys.OrderBy(k => k).First();
                dataDictionary.Remove(oldestDate);
            }

            // Add new data entry
            dataDictionary[dateKey] = new DashboardFileData
            {
                FileCount = 1,
                StorageSaved = savings,
                OriginalStorage = originalSize,
                FinalStorage = finalSize
            };
        }
        else
        {
            // Update existing entry
            dataDictionary[dateKey].FileCount += 1;
            dataDictionary[dateKey].StorageSaved += savings;
            dataDictionary[dateKey].OriginalStorage += originalSize;
            dataDictionary[dateKey].FinalStorage += finalSize;
        }
    }

    /// <summary>
    /// Gets the current data
    /// </summary>
    /// <returns>the current data</returns>
    public FileOverviewData GetData()
        => new()
        {
            Last24Hours = last24HoursData,
            Last7Days = last7DaysData,
            Last31Days = last31DaysData
        };
}