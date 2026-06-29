namespace CityMixedGolf.Web.Models;

public class Notification
{
    public int Id { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public int? CompetitionId { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }

    public virtual GolfPlayer Player { get; set; } = null!;
    public virtual Competition? Competition { get; set; }
}

public enum NotificationChannel { Email, WhatsApp, Both }
public enum NotificationType { EntryConfirmed, EntryAmended, EntryCancelled, DrawPublished, ResultsPublished, Reminder }
public enum NotificationStatus { Queued, Sent, Failed, OptedOut }