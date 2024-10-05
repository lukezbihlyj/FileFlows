using FileFlows.Server.Hubs;
using FileFlows.ServerShared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// The paused service
/// </summary>
public class PausedService
{
    private SettingsService service;
    /// <summary>
    /// Gets when the system is paused until
    /// </summary>
    public DateTime PausedUntil { get; private set; }
    
    /// <summary>
    /// Gets if the system is paused
    /// </summary>
    public bool IsPaused => DateTime.UtcNow < PausedUntil;
    
    /// <summary>
    /// Constructs an new instance of the Paused Service
    /// </summary>
    public PausedService()
    {
        service = (SettingsService)ServiceLoader.Load<ISettingsService>();
        var settings = service.Get().Result;
        PausedUntil = settings.PausedUntil;

    }
    
    /// <summary>
    /// Pauses the system
    /// </summary>
    /// <param name="minutes">the number of minutes to pause the system for</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task Pause(int minutes, AuditDetails? auditDetails = null )
    {
        if (minutes < 0)
        {
            // resuming
            if (IsPaused == false)
                return;
        }
        else if (IsPaused)
            return; // already paused
        
        var settings = await service.Get();
        PausedUntil = minutes < 1 ? DateTime.MinValue : DateTime.UtcNow.AddMinutes(minutes);
        settings.PausedUntil = PausedUntil;
        ClientServiceManager.Instance.SystemPaused(minutes);
        await service.Save(settings, auditDetails);
    }
}