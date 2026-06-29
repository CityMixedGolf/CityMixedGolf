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
        var player = await _userManager.GetUserAsync(User) ?? throw new UnauthorizedAccessException();

        var entries = await _db.CompetitionEntries
            .Include(e => e.Competition)
            .Include(e => e.PreferredPartner)
            .Where(e => e.PlayerId == player.Id)
            .OrderByDescending(e => e.Competition.CompetitionDate)
            .ToListAsync();

        var openCompetitions = await _db.Competitions
            .Where(c => c.Status == CompetitionStatus.Open
                && c.EntryOpenDate <= DateTime.UtcNow
                && c.EntryCloseDate >= DateTime.UtcNow)
            .ToListAsync();

        var history = await _db.DrawPairs
            .Include(dp => dp.GreenBandPlayer)
            .Include(dp => dp.RedBandPlayer)
            .Include(dp => dp.GroupDraw).ThenInclude(gd => gd.Competition)
            .Where(dp => (dp.GreenBandPlayerId == player.Id || dp.RedBandPlayerId == player.Id)
                && dp.GroupDraw.IsPublished)
            .OrderByDescending(dp => dp.GroupDraw.Competition.CompetitionDate)
            .ToListAsync();

        ViewBag.Player = player;
        ViewBag.Entries = entries;
        ViewBag.OpenCompetitions = openCompetitions;
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

        // Check if already entered
        var existing = await _db.CompetitionEntries
            .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.CompetitionId == competitionId && e.Status == EntryStatus.Entered);

        // Build partner list — opposite gender only, who have also entered
        var oppositeGender = player.Gender == Gender.Lady ? Gender.Gent : Gender.Lady;
        var eligiblePartners = await _db.CompetitionEntries
            .Include(e => e.Player)
            .Where(e => e.CompetitionId == competitionId
                && e.Status == EntryStatus.Entered
                && e.Player.Gender == oppositeGender
                && e.PlayerId != player.Id)
            .Select(e => e.Player)
            .ToListAsync();

        // Get previous partners ordered by most recent
        var previousPartnerIds = await _db.DrawPairs
            .Include(dp => dp.GroupDraw)
            .Where(dp => dp.GroupDraw.IsPublished
                && (dp.GreenBandPlayerId == player.Id || dp.RedBandPlayerId == player.Id))
            .OrderByDescending(dp => dp.GroupDraw.DrawnAt)
            .Select(dp => dp.GreenBandPlayerId == player.Id ? dp.RedBandPlayerId : dp.GreenBandPlayerId)
            .Distinct()
            .ToListAsync();

        // Sort eligible partners: previous partners first (most recent first), then others
        var sortedPartners = eligiblePartners
            .OrderBy(p => {
                var idx = previousPartnerIds.IndexOf(p.Id);
                return idx == -1 ? int.MaxValue : idx;
            })
            .ToList();

        // Default to most recent previous partner if available
        var defaultPartnerId = sortedPartners.FirstOrDefault(p => previousPartnerIds.Contains(p.Id))?.Id;

        ViewBag.Competition = competition;
        ViewBag.ExistingEntry = existing;
        ViewBag.EligiblePartners = sortedPartners;
        ViewBag.PreviousPartnerIds = previousPartnerIds;
        ViewBag.DefaultPartnerId = defaultPartnerId;

        return View(existing ?? new CompetitionEntry { CompetitionId = competitionId, PreferredPartnerId = defaultPartnerId });
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
            .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.CompetitionId == model.CompetitionId && e.Status == EntryStatus.Entered);

        bool isNew = existing == null;

        if (existing == null)
        {
            existing = new CompetitionEntry { PlayerId = player.Id, CompetitionId = model.CompetitionId, CreatedAt = DateTime.UtcNow };
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
}