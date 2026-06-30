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

        // Order of merit — pull every published, scored pair this year, then split by
        // each player's actual Gender rather than which side (green/red) of the pair
        // they happened to be drawn into. Band colour reflects handicap grouping, not
        // gender, so a lady can sit in the red band and vice versa.
        var publishedPairs = await _db.DrawPairs
            .Include(dp => dp.GreenBandPlayer)
            .Include(dp => dp.RedBandPlayer)
            .Include(dp => dp.GroupDraw).ThenInclude(gd => gd.Competition)
            .Where(dp => dp.GroupDraw.IsPublished
                && dp.OrderOfMeritPoints != null
                && dp.GroupDraw.Competition.CompetitionDate.Year == currentYear)
            .ToListAsync();

        var oomEntries = publishedPairs
            .SelectMany(dp => new[]
            {
                new { Player = dp.GreenBandPlayer, Points = dp.OrderOfMeritPoints ?? 0, Score = dp.Score ?? 0 },
                new { Player = dp.RedBandPlayer, Points = dp.OrderOfMeritPoints ?? 0, Score = dp.Score ?? 0 }
            })
            .ToList();

        var oomLadies = oomEntries
            .Where(e => e.Player.Gender == Gender.Lady)
            .GroupBy(e => e.Player)
            .Select(g => new { Player = g.Key, Points = g.Sum(e => e.Points), Best = g.Max(e => e.Score) })
            .OrderByDescending(x => x.Points)
            .Take(10)
            .ToList();

        var oomGents = oomEntries
            .Where(e => e.Player.Gender == Gender.Gent)
            .GroupBy(e => e.Player)
            .Select(g => new { Player = g.Key, Points = g.Sum(e => e.Points), Best = g.Max(e => e.Score) })
            .OrderByDescending(x => x.Points)
            .Take(10)
            .ToList();

        // Upcoming competitions
        var upcoming = await _db.Competitions
            .Where(c => c.CompetitionDate >= DateTime.UtcNow
                && c.Status != CompetitionStatus.Archived)
            .Include(c => c.Entries)
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
