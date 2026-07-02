using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;
using CityMixedGolf.Web.Services;

namespace CityMixedGolf.Web.Areas.Member.Controllers;

[Area("Member")]
[Authorize]
public class EntryController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<GolfPlayer> _userManager;
    private readonly INotificationService _notifications;

    public EntryController(ApplicationDbContext db, UserManager<GolfPlayer> userManager, INotificationService notifications)
    {
        _db = db;
        _userManager = userManager;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index()
    {
        var playerId = _userManager.GetUserId(User)!;
        var player = await _db.Users
            .Include(p => p.PlayerRecord)
            .FirstOrDefaultAsync(p => p.Id == playerId)
            ?? throw new UnauthorizedAccessException();

        // This player's entries
        var myEntries = await _db.CompetitionEntries
            .Include(e => e.Competition)
            .Include(e => e.PreferredPartner)
            .Where(e => e.PlayerId == player.Id)
            .OrderByDescending(e => e.Competition.CompetitionDate)
            .ToListAsync();

        // All open competitions
        var openCompetitions = await _db.Competitions
            .Where(c => c.Status == CompetitionStatus.Open
                && c.EntryOpenDate <= DateTime.UtcNow
                && c.EntryCloseDate >= DateTime.UtcNow)
            .ToListAsync();

        // All entrants for each open competition (so dashboard shows full entry list)
        var openCompIds = openCompetitions.Select(c => c.Id).ToList();
        var allEntrants = await _db.CompetitionEntries
            .Include(e => e.Player)
            .Include(e => e.Competition)
            .Where(e => openCompIds.Contains(e.CompetitionId) && e.Status == EntryStatus.Entered)
            .OrderBy(e => e.Player.LastName)
            .ToListAsync();

        // History
        var history = await _db.DrawPairs
            .Include(dp => dp.GreenBandPlayer)
            .Include(dp => dp.RedBandPlayer)
            .Include(dp => dp.GroupDraw).ThenInclude(gd => gd.Competition)
            .Where(dp => (dp.GreenBandPlayerId == player.Id || dp.RedBandPlayerId == player.Id)
                && dp.GroupDraw.IsPublished)
            .OrderByDescending(dp => dp.GroupDraw.Competition.CompetitionDate)
            .ToListAsync();

        ViewBag.Player = player;
        ViewBag.MyEntries = myEntries;
        ViewBag.OpenCompetitions = openCompetitions;
        ViewBag.AllEntrants = allEntrants;
        ViewBag.History = history;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Enter(int competitionId)
    {
        var player = await _userManager.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
        var competition = await _db.Competitions.FindAsync(competitionId);

        if (competition == null || competition.Status != CompetitionStatus.Open
            || competition.EntryCloseDate < DateTime.UtcNow)
            return NotFound();

        var existing = await _db.CompetitionEntries
            .FirstOrDefaultAsync(e => e.PlayerId == player.Id
                && e.CompetitionId == competitionId
                && e.Status == EntryStatus.Entered);

        // Eligible partners: opposite gender, have entered this competition
        // Gender is on PlayerRecord — load all entered players then filter in memory
        var allEnteredPlayers = await _db.CompetitionEntries
            .Include(e => e.Player).ThenInclude(p => p.PlayerRecord)
            .Where(e => e.CompetitionId == competitionId
                && e.Status == EntryStatus.Entered
                && e.PlayerId != player.Id)
            .Select(e => e.Player)
            .ToListAsync();

        var oppositeGender = player.Gender == Gender.Lady ? Gender.Gent : Gender.Lady;
        var eligiblePartners = allEnteredPlayers
            .Where(p => p.Gender == oppositeGender)
            .ToList();

        // Previous partners ordered by most recent
        var previousPartnerIds = await _db.DrawPairs
            .Include(dp => dp.GroupDraw)
            .Where(dp => dp.GroupDraw.IsPublished
                && (dp.GreenBandPlayerId == player.Id || dp.RedBandPlayerId == player.Id))
            .OrderByDescending(dp => dp.GroupDraw.DrawnAt)
            .Select(dp => dp.GreenBandPlayerId == player.Id ? dp.RedBandPlayerId : dp.GreenBandPlayerId)
            .Distinct()
            .ToListAsync();

        var sortedPartners = eligiblePartners
            .OrderBy(p => {
                var idx = previousPartnerIds.IndexOf(p.Id);
                return idx == -1 ? int.MaxValue : idx;
            })
            .ToList();

        // Default: usual partner if entered, else most recent previous partner
        var defaultPartnerId = existing?.PreferredPartnerId
            ?? (sortedPartners.Any(p => p.Id == player.UsualPartnerId) ? player.UsualPartnerId : null)
            ?? sortedPartners.FirstOrDefault(p => previousPartnerIds.Contains(p.Id))?.Id;

        ViewBag.Competition = competition;
        ViewBag.ExistingEntry = existing;
        ViewBag.EligiblePartners = sortedPartners;
        ViewBag.PreviousPartnerIds = previousPartnerIds;
        ViewBag.DefaultPartnerId = defaultPartnerId;

        return View(existing ?? new CompetitionEntry
        {
            CompetitionId = competitionId,
            PreferredPartnerId = defaultPartnerId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(CompetitionEntry model)
    {
        var player = await _userManager.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
        var competition = await _db.Competitions.FindAsync(model.CompetitionId);

        if (competition == null || competition.EntryCloseDate < DateTime.UtcNow)
            return BadRequest("Entry is closed.");

        var existing = await _db.CompetitionEntries
            .FirstOrDefaultAsync(e => e.PlayerId == player.Id
                && e.CompetitionId == model.CompetitionId
                && e.Status == EntryStatus.Entered);

        bool isNew = existing == null;

        if (existing == null)
        {
            existing = new CompetitionEntry
            {
                PlayerId = player.Id,
                CompetitionId = model.CompetitionId,
                CreatedAt = DateTime.UtcNow
            };
            _db.CompetitionEntries.Add(existing);
        }

        existing.TeePreference = model.TeePreference;
        existing.EnteringAsSingle = model.EnteringAsSingle;
        existing.PreferredPartnerId = model.EnteringAsSingle ? null : model.PreferredPartnerId;
        existing.SpecialRequests = model.SpecialRequests;
        existing.Status = EntryStatus.Entered;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        if (isNew)
            await _notifications.SendEntryConfirmedAsync(existing.Id);
        else
            await _notifications.SendEntryAmendedAsync(existing.Id);

        TempData["Success"] = isNew ? "You are entered!" : "Entry updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int entryId)
    {
        var player = await _userManager.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
        var entry = await _db.CompetitionEntries
            .Include(e => e.Competition)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.PlayerId == player.Id);

        if (entry == null || entry.Competition.EntryCloseDate < DateTime.UtcNow)
            return BadRequest();

        entry.Status = EntryStatus.Cancelled;
        entry.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notifications.SendEntryCancelledAsync(entry.Id);
        TempData["Success"] = "Entry cancelled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNotifications(bool emailNotifications, bool whatsAppOptIn)
    {
        var player = await _userManager.GetUserAsync(User) ?? throw new UnauthorizedAccessException();
        player.EmailNotifications = emailNotifications;
        player.WhatsAppOptIn = whatsAppOptIn;
        await _userManager.UpdateAsync(player);
        TempData["Success"] = "Notification preferences saved.";
        return RedirectToAction(nameof(Index));
    }
}
