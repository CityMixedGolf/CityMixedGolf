using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;
using CityMixedGolf.Web.Services;

namespace CityMixedGolf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PlayersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IPlayerImportService _importer;

    public PlayersController(ApplicationDbContext db, IPlayerImportService importer)
    {
        _db = db;
        _importer = importer;
    }

    public async Task<IActionResult> Index()
    {
        var players = await _db.GolfPlayerRecords
            .Include(p => p.LinkedAccount)
            .OrderBy(p => p.FullName)
            .ToListAsync();

        return View(players);
    }

    [HttpGet]
    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            ModelState.AddModelError("", "Please select a CSV file.");
            return View();
        }

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "File must be a .csv");
            return View();
        }

        using var stream = csvFile.OpenReadStream();
        var result = await _importer.ImportFromCsvAsync(stream);

        TempData["ImportResult"] = $"Import complete: {result.Added} added, {result.Updated} updated.";
        if (result.Errors.Any())
            TempData["ImportErrors"] = string.Join("|", result.Errors);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetBand(int id, BandColour band)
    {
        var player = await _db.GolfPlayerRecords.FindAsync(id);
        if (player == null) return NotFound();
        player.BandColour = band;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var player = await _db.GolfPlayerRecords.FindAsync(id);
        if (player == null) return NotFound();
        player.IsActive = !player.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
