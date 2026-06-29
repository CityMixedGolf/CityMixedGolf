using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CityMixedGolf.Web.Services;

public interface INotificationService
{
    Task SendDrawPublishedAsync(int competitionId);
    Task SendEntryConfirmedAsync(int entryId);
    Task SendEntryAmendedAsync(int entryId);
    Task SendEntryCancelledAsync(int entryId);
    Task SendReminderAsync(int competitionId);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ISendGridClient _sendGrid;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext db,
        ISendGridClient sendGrid,
        IConfiguration config,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _sendGrid = sendGrid;
        _config = config;
        _logger = logger;
    }

    public async Task SendDrawPublishedAsync(int competitionId)
    {
        var competition = await _db.Competitions
            .Include(c => c.Draws).ThenInclude(d => d.Pairs)
                .ThenInclude(p => p.GreenBandPlayer)
            .Include(c => c.Draws).ThenInclude(d => d.Pairs)
                .ThenInclude(p => p.RedBandPlayer)
            .Include(c => c.Entries).ThenInclude(e => e.Player)
            .FirstOrDefaultAsync(c => c.Id == competitionId)
            ?? throw new InvalidOperationException("Competition not found");

        var publishedDraw = competition.Draws.FirstOrDefault(d => d.IsPublished)
            ?? throw new InvalidOperationException("No published draw found");

        foreach (var entry in competition.Entries.Where(e => e.Status == EntryStatus.Entered))
        {
            var player = entry.Player;
            if (!player.EmailNotifications) continue;

            var pair = publishedDraw.Pairs.FirstOrDefault(p =>
                p.GreenBandPlayerId == player.Id || p.RedBandPlayerId == player.Id);

            var partnerName = pair == null ? "TBC" :
                pair.GreenBandPlayerId == player.Id
                    ? pair.RedBandPlayer.FullName
                    : pair.GreenBandPlayer.FullName;

            var teeInfo = pair?.AssignedTee == TeePreference.Early ? "Early tee" :
                          pair?.AssignedTee == TeePreference.Late ? "Late tee" : "Tee time TBC";

            var subject = $"Draw Published – {competition.Name} ({competition.CompetitionDate:dd MMM yyyy})";
            var body = $"Hi {player.FirstName},\n\n" +
                       $"The draw for {competition.Name} has been published.\n\n" +
                       $"Your playing partner: {partnerName}\n" +
                       $"Tee: {teeInfo}\n\n" +
                       $"Competition date: {competition.CompetitionDate:dddd dd MMMM yyyy}\n\n" +
                       $"Good luck!\nCity of Newcastle GC – Mixed Section";

            await SendEmailAsync(player.Email!, subject, body);
            await LogNotificationAsync(player.Id, competitionId, NotificationType.DrawPublished, body);
        }
    }

    public async Task SendEntryConfirmedAsync(int entryId)
    {
        var entry = await _db.CompetitionEntries
            .Include(e => e.Player)
            .Include(e => e.Competition)
            .Include(e => e.PreferredPartner)
            .FirstOrDefaultAsync(e => e.Id == entryId)
            ?? throw new InvalidOperationException("Entry not found");

        var tee = entry.TeePreference == TeePreference.Early ? "Early" :
                  entry.TeePreference == TeePreference.Late ? "Late" : "No preference";
        var partner = entry.EnteringAsSingle ? "Entering as single" :
                      entry.PreferredPartner != null ? entry.PreferredPartner.FullName : "No preference";

        var subject = $"Entry Confirmed – {entry.Competition.Name}";
        var body = $"Hi {entry.Player.FirstName},\n\n" +
                   $"Your entry for {entry.Competition.Name} ({entry.Competition.CompetitionDate:dd MMM yyyy}) has been confirmed.\n\n" +
                   $"Tee preference: {tee}\n" +
                   $"Partner preference: {partner}\n\n" +
                   $"You can amend or cancel your entry before {entry.Competition.EntryCloseDate:dd MMM yyyy}.\n\n" +
                   $"City of Newcastle GC – Mixed Section";

        await SendEmailAsync(entry.Player.Email!, subject, body);
        await LogNotificationAsync(entry.Player.Id, entry.CompetitionId, NotificationType.EntryConfirmed, body);
    }

    public async Task SendEntryAmendedAsync(int entryId) =>
        await SendEntryConfirmedAsync(entryId); // reuse same template

    public async Task SendEntryCancelledAsync(int entryId)
    {
        var entry = await _db.CompetitionEntries
            .Include(e => e.Player)
            .Include(e => e.Competition)
            .FirstOrDefaultAsync(e => e.Id == entryId)
            ?? throw new InvalidOperationException("Entry not found");

        var subject = $"Entry Cancelled – {entry.Competition.Name}";
        var body = $"Hi {entry.Player.FirstName},\n\n" +
                   $"Your entry for {entry.Competition.Name} ({entry.Competition.CompetitionDate:dd MMM yyyy}) has been cancelled.\n\n" +
                   $"If this was a mistake, you can re-enter before {entry.Competition.EntryCloseDate:dd MMM yyyy}.\n\n" +
                   $"City of Newcastle GC – Mixed Section";

        await SendEmailAsync(entry.Player.Email!, subject, body);
        await LogNotificationAsync(entry.Player.Id, entry.CompetitionId, NotificationType.EntryCancelled, body);
    }

    public async Task SendReminderAsync(int competitionId)
    {
        var competition = await _db.Competitions
            .Include(c => c.Entries).ThenInclude(e => e.Player)
            .FirstOrDefaultAsync(c => c.Id == competitionId)
            ?? throw new InvalidOperationException("Competition not found");

        foreach (var entry in competition.Entries.Where(e => e.Status == EntryStatus.Entered))
        {
            if (!entry.Player.EmailNotifications) continue;
            var subject = $"Reminder – {competition.Name} Tomorrow";
            var body = $"Hi {entry.Player.FirstName},\n\n" +
                       $"Just a reminder that {competition.Name} is tomorrow, {competition.CompetitionDate:dddd dd MMMM yyyy}.\n\n" +
                       $"City of Newcastle GC – Mixed Section";

            await SendEmailAsync(entry.Player.Email!, subject, body);
            await LogNotificationAsync(entry.Player.Id, competitionId, NotificationType.Reminder, body);
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var from = new EmailAddress(_config["SendGrid:FromEmail"], "City of Newcastle GC Mixed");
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, body, null);
        var response = await _sendGrid.SendEmailAsync(msg);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("SendGrid failed for {Email}: {Status}", toEmail, response.StatusCode);
    }

    private async Task LogNotificationAsync(string playerId, int? competitionId, NotificationType type, string message)
    {
        _db.Notifications.Add(new Notification
        {
            PlayerId = playerId,
            CompetitionId = competitionId,
            Channel = NotificationChannel.Email,
            Type = type,
            Status = NotificationStatus.Sent,
            Message = message,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}