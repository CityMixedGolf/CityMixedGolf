using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;

namespace CityMixedGolf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CompetitionController : Controller
{
    private readonly ApplicationDbContext _db;

    public CompetitionController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var competitions = await _db.Competitions
            .Include(c => c.Entries)
            .OrderByDescending(c => c.CompetitionDate)
            .ToListAsync();
        return View(competitions);
    }

    [HttpGet]
    public IActionResult Create() => View(new Competition { CompetitionDate = DateTime.Today.AddDays(30) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Competition model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Competitions.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Competition created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var comp = await _db.Competitions.FindAsync(id);
        return comp == null ? NotFound() : View(comp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Competition model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Competitions.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Competition updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, CompetitionStatus status)
    {
        var comp = await _db.Competitions.FindAsync(id);
        if (comp == null) return NotFound();
        comp.Status = status;
        await _db.SaveChangesAsync();
        return Ok();
    }
}