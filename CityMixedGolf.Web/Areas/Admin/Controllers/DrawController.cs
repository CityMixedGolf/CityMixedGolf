using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;
using CityMixedGolf.Web.Services;

namespace CityMixedGolf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DrawController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IDrawService _drawService;
    private readonly INotificationService _notifications;
    private readonly UserManager<GolfPlayer> _userManager;

    public DrawController(ApplicationDbContext db, IDrawService drawService, INotificationService notifications, UserManager<GolfPlayer> userManager)
    {
        _db = db;
        _drawService = drawService;
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int competitionId)
    {
        var competition = await _db.Competitions
            .Include(c => c.Entries).ThenInclude(e => e.Player)
            .Include(c => c.Entries).ThenInclude(e => e.PreferredPartner)
            .Include(c => c.Draws).ThenInclude(d => d.Pairs).ThenInclude(p => p.GreenBandPlayer)
            .Include(c => c.Draws).ThenInclude(d => d.Pairs).ThenInclude(p => p.RedBandPlayer)
            .FirstOrDefaultAsync(c => c.Id == competitionId);

        if (competition == null) return NotFound();

        var activeDraw = competition.Draws.OrderByDescending(d => d.DrawnAt).FirstOrDefault();
        var singles = competition.Entries
            .Where(e => e.EnteringAsSingle && e.Status == EntryStatus.Entered)
            .ToList();

        ViewBag.Competition = competition;
        ViewBag.Draw = activeDraw;
        ViewBag.Singles = singles;
        ViewBag.AllPlayers = await _db.Users.Where(u => u.IsActive).OrderBy(u => u.LastName).ToListAsync();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GenerateDraw(int competitionId)
    {
        var userId = _userManager.GetUserId(User)!;
        var draw = await _drawService.GenerateDrawAsync(competitionId, userId);
        TempData["Success"] = "Draw generated. Review pairs below before publishing.";
        return RedirectToAction(nameof(Index), new { competitionId });
    }

    [HttpPost]
    public async Task<IActionResult> SwapPairs(int pairId1, int pairId2)
    {
        await _drawService.SwapPlayersAsync(pairId1, pairId2);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> ManualOverride(int pairId, string greenPlayerId, string redPlayerId)
    {
        await _drawService.ManualOverrideAsync(pairId, greenPlayerId, redPlayerId);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CombineSingles(string greenPlayerId, string redPlayerId, int groupDrawId)
    {
        // Admin combines two singles into a pair
        var draw = await _db.GroupDraws.Include(d => d.Pairs).FirstOrDefaultAsync(d => d.Id == groupDrawId);
        if (draw == null) return NotFound();

        var pairNumber = draw.Pairs.Any() ? draw.Pairs.Max(p => p.PairNumber) + 1 : 1;

        _db.DrawPairs.Add(new DrawPair
        {
            GroupDrawId = groupDrawId,
            GreenBandPlayerId = greenPlayerId,
            RedBandPlayerId = redPlayerId,
            PairNumber = pairNumber,
            PairStatus = DrawPairStatus.ManualOverride,
            AssignedTee = TeePreference.NoPreference
        });

        await _db.SaveChangesAsync();
        return Ok(new { pairNumber });
    }

    [HttpPost]
    public async Task<IActionResult> Publish(int groupDrawId)
    {
        await _drawService.PublishDrawAsync(groupDrawId);
        var draw = await _db.GroupDraws.FindAsync(groupDrawId);
        if (draw != null)
            await _notifications.SendDrawPublishedAsync(draw.CompetitionId);

        TempData["Success"] = "Draw published and members notified.";
        var competition = await _db.GroupDraws.Where(d => d.Id == groupDrawId).Select(d => d.CompetitionId).FirstOrDefaultAsync();
        return RedirectToAction(nameof(Index), new { competitionId = competition });
    }
}