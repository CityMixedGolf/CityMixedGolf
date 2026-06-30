namespace CityMixedGolf.Web.Services;

/// <summary>
/// No-op implementation of INotificationService used when SendGrid is not configured
/// (e.g. local development). All methods succeed silently.
/// </summary>
public class NoOpNotificationService : INotificationService
{
    public Task SendDrawPublishedAsync(int competitionId) => Task.CompletedTask;
    public Task SendEntryConfirmedAsync(int entryId) => Task.CompletedTask;
    public Task SendEntryAmendedAsync(int entryId) => Task.CompletedTask;
    public Task SendEntryCancelledAsync(int entryId) => Task.CompletedTask;
    public Task SendReminderAsync(int competitionId) => Task.CompletedTask;
}