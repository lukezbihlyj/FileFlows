﻿namespace FileFlows.Managers;

/// <summary>
/// An instance of the Settings Service which allows accessing of the system settings
/// </summary>
public class SettingsManager
{
    private static FairSemaphore _semaphore = new(1);
    // Special case, we always cache the settings, as it is constantly looked up
    private static Settings Instance;

    static SettingsManager()
    {
        Instance = DatabaseAccessManager.Instance.FileFlowsObjectManager.Single<Settings>().Result;
    }

    /// <summary>
    /// Gets or sets if caching should be used
    /// </summary>
    internal static bool UseCache => Instance.Cache?.UseCache != false;
    
    /// <summary>
    /// Gets the system settings
    /// </summary>
    /// <returns>the system settings</returns>
    public Task<Settings> Get() => Task.FromResult(Instance);

    /// <summary>
    /// Gets the current configuration revision number
    /// </summary>
    /// <returns>the current configuration revision number</returns>
    public Task<int> GetCurrentConfigurationRevision()
        => Task.FromResult(Instance.Revision);
    
    /// <summary>
    /// Increments the revision
    /// </summary>
    public async Task RevisionIncrement()
    {
        await _semaphore.WaitAsync();
        try
        {
            Instance.Revision += 1;
            await DatabaseAccessManager.Instance.FileFlowsObjectManager.Update(Instance);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}