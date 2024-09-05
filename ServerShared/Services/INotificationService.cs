using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Interface for sending a notification
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Records a new notification with the specified severity, title, and message.
    /// </summary>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content of the notification.</param>
    Task Record(NotificationSeverity severity, string title, string? message = null);
}