using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;

namespace CityMixedGolf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ResultsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ResultsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Enter(int competitionId)
    {
        var competition = await _db.Competitions
            .Include(c => c.Draws).ThenInclude(d => d.Pairs)
                .ThenInclude(p => p.GreenBandPlayer)
            .Include(c => c.Draws).ThenInclude(d => d.Pairs)
                .ThenInclude(p => p.RedBandPlayer)
            .FirstOrDefaultAsync(c => c.Id == competitionId);

        if (competition == null) return NotFound();

        var publishedDraw = competition.Draws.FirstOrDefault(d => d.IsPublished);
        if (publishedDraw == null)
        {
            TempData["Error"] = "No published draw found. Publish the draw before entering results.";
            return RedirectToAction("Index", "Competition");
        }

        ViewBag.Competition = competition;
        ViewBag.Draw = publishedDraw;
        return View(publishedDraw.Pairs.OrderBy(p => p.PairNumber).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(int competitionId, List<PairResultInput> results)
    {
        foreach (var result in results)
        {
            var pair = await _db.DrawPairs.FindAsync(result.PairId);
            if (pair == null) continue;
            pair.Score = result.Score;
        }
        await _db.SaveChangesAsync();

        // Calculate positions by ranking scores descending
        var draw = await _db.GroupDraws
            .Include(d => d.Pairs)
            .FirstOrDefaultAsync(d => d.CompetitionId == competitionId && d.IsPublished);

        if (draw != null)
        {
            var ranked = draw.Pairs
                .Where(p => p.Score.HasValue)
                .OrderByDescending(p => p.Score)
                .ToList();

            // Points scale: 20, 16, 12, 10, 8, 6, 4, 2 for top 8; 1 for remainder
            var pointsScale = new[] { 20, 16, 12, 10, 8, 6, 4, 2 };
            for (int i = 0; i < ranked.Count; i++)
            {
                ranked[i].Position = i + 1;
                ranked[i].OrderOfMeritPoints = i < pointsScale.Length ? pointsScale[i] : 1;
            }

            var competition = await _db.Competitions.FindAsync(competitionId);
            if (competition != null)
                competition.Status = CompetitionStatus.ResultsEntered;

            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "Results saved and order of merit points calculated.";
        return RedirectToAction("Index", "Competition");
    }
}

public class PairResultInput
{
    public int PairId { get; set; }
    public int? Score { get; set; }
}
