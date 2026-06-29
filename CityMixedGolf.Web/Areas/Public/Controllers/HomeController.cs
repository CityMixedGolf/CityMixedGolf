using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;

namespace CityMixedGolf.Web.Areas.Public.Controllers;

[Area("Public")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var currentYear = DateTime.UtcNow.Year;

        // Latest published draw results
        var latestResults = await _db.DrawPairs
            .Include(dp => dp.GreenBandPlayer)
            .Include(dp => dp.RedBandPlayer)
            .Include(dp => dp.GroupDraw).ThenInclude(gd => gd.Competition)
            .Where(dp => dp.GroupDraw.IsPublished
                && dp.Score != null
                && dp.GroupDraw.Competition.CompetitionDate.Year == currentYear)
            .OrderByDescending(dp => dp.GroupDraw.Competition.CompetitionDate)
            .ThenBy(dp => dp.Position)
            .Take(8)
            .ToListAsync();

        // Order of merit — ladies
        var oomLadies = await _db.DrawPairs
            .Include(dp => dp.GreenBandPlayer)
            .Include(dp => dp.GroupDraw).ThenInclude(gd => gd.Competition)
            .Where(dp => dp.GreenBandPlayer.Gender == Gender.Lady
                && dp.GroupDraw.IsPublished
                && dp.OrderOfMeritPoints != null
                && dp.GroupDraw.Competition.CompetitionDate.Year == currentYear)
            .GroupBy(dp => dp.GreenBandPlayer)
            .Select(g => new { Player = g.Key, Points = g.Sum(dp => dp.OrderOfMeritPoints ?? 0), Best = g.Max(dp => dp.Score ?? 0) })
            .OrderByDescending(x => x.Points)
            .Take(10)
            .ToListAsync();

        // Order of merit — gents
        var oomGents = await _db.DrawPairs
            .Include(dp => dp.RedBandPlayer)
            .Include(dp => dp.GroupDraw).ThenInclude(gd => gd.Competition)
            .Where(dp => dp.RedBandPlayer.Gender == Gender.Gent
                && dp.GroupDraw.IsPublished
                && dp.OrderOfMeritPoints != null
                && dp.GroupDraw.Competition.CompetitionDate.Year == currentYear)
            .GroupBy(dp => dp.RedBandPlayer)
            .Select(g => new { Player = g.Key, Points = g.Sum(dp => dp.OrderOfMeritPoints ?? 0), Best = g.Max(dp => dp.Score ?? 0) })
            .OrderByDescending(x => x.Points)
            .Take(10)
            .ToListAsync();

        // Upcoming competitions
        var upcoming = await _db.Competitions
            .Where(c => c.CompetitionDate >= DateTime.UtcNow
                && c.Status != CompetitionStatus.Draft
                && c.Status != CompetitionStatus.Archived)
            .OrderBy(c => c.CompetitionDate)
            .Take(5)
            .ToListAsync();

        // Season stats
        var seasonStats = new
        {
            TotalCompetitions = await _db.Competitions.CountAsync(c => c.CompetitionDate.Year == currentYear && c.Status != CompetitionStatus.Draft),
            Completed = await _db.Competitions.CountAsync(c => c.CompetitionDate.Year == currentYear && c.Status == CompetitionStatus.ResultsEntered),
            TotalMembers = await _db.Users.CountAsync(u => u.IsActive),
            Remaining = await _db.Competitions.CountAsync(c => c.CompetitionDate >= DateTime.UtcNow && c.Status != CompetitionStatus.Draft && c.Status != CompetitionStatus.Archived)
        };

        ViewBag.LatestResults = latestResults;
        ViewBag.OomLadies = oomLadies;
        ViewBag.OomGents = oomGents;
        ViewBag.Upcoming = upcoming;
        ViewBag.SeasonStats = seasonStats;
        ViewBag.CurrentYear = currentYear;

        return View();
    }
}