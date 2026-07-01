using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;
using CityMixedGolf.Web.ViewModels;

namespace CityMixedGolf.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<GolfPlayer> _userManager;
    private readonly SignInManager<GolfPlayer> _signInManager;
    private readonly ApplicationDbContext _db;

    public AccountController(
        UserManager<GolfPlayer> userManager,
        SignInManager<GolfPlayer> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    // ── Login ──────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home", new { area = "Public" });

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Entry", new { area = "Member" });
        }

        if (result.IsLockedOut)
            ModelState.AddModelError("", "Account locked. Please try again in a few minutes.");
        else
            ModelState.AddModelError("", "Invalid email or password.");

        return View(model);
    }

    // ── Register ───────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home", new { area = "Public" });

        await LoadRegisterViewBag(null);
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadRegisterViewBag(model.PlayerId);
            return View(model);
        }

        // Find the existing unregistered player record
        var player = await _db.Users.FirstOrDefaultAsync(p =>
            p.Id == model.PlayerId && p.UserName == null);

        if (player == null)
        {
            ModelState.AddModelError("PlayerId", "Player not found or already registered. Please contact the admin.");
            await LoadRegisterViewBag(model.PlayerId);
            return View(model);
        }

        // Check email isn't already taken
        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError("Email", "An account with this email already exists.");
            await LoadRegisterViewBag(model.PlayerId);
            return View(model);
        }

        // Claim the existing player record by setting Identity credentials
        player.UserName = model.Email;
        player.NormalizedUserName = model.Email.ToUpperInvariant();
        player.Email = model.Email;
        player.NormalizedEmail = model.Email.ToUpperInvariant();
        player.EmailConfirmed = true;
        player.UsualPartnerId = string.IsNullOrEmpty(model.UsualPartnerId) ? null : model.UsualPartnerId;

        var passwordResult = await _userManager.AddPasswordAsync(player, model.Password);
        if (!passwordResult.Succeeded)
        {
            foreach (var error in passwordResult.Errors)
                ModelState.AddModelError("", error.Description);
            await LoadRegisterViewBag(model.PlayerId);
            return View(model);
        }

        var updateResult = await _userManager.UpdateAsync(player);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError("", error.Description);
            await LoadRegisterViewBag(model.PlayerId);
            return View(model);
        }

        await _signInManager.SignInAsync(player, isPersistent: false);
        TempData["Success"] = $"Welcome, {player.FirstName}! Your account is ready.";
        return RedirectToAction("Index", "Entry", new { area = "Member" });
    }

    private async Task LoadRegisterViewBag(string? selectedPlayerId)
    {
        // Only show players who have no UserName set (i.e. not yet registered)
        var unregistered = await _db.Users
            .Where(p => p.UserName == null && p.IsActive)
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .ToListAsync();

        ViewBag.Players = new SelectList(unregistered, "Id", "FullName", selectedPlayerId);

        // Load the selected player's gender so we can filter partners client-side
        GolfPlayer? selected = null;
        if (!string.IsNullOrEmpty(selectedPlayerId))
            selected = unregistered.FirstOrDefault(p => p.Id == selectedPlayerId);

        ViewBag.SelectedPlayer = selected;

        // Potential partners — all active players of opposite gender
        // We'll load all and filter client-side based on player selection
        var allPlayers = await _db.Users
            .Where(p => p.IsActive)
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .ToListAsync();

        ViewBag.AllPartners = allPlayers;
    }

    // ── Logout ─────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "Public" });
    }

    // ── Profile ────────────────────────────────────────────────

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var player = await _userManager.GetUserAsync(User)
            ?? throw new UnauthorizedAccessException();

        var partners = await _db.Users
            .Where(p => p.IsActive && p.Gender != player.Gender && p.Id != player.Id)
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .ToListAsync();

        ViewBag.Partners = new SelectList(partners, "Id", "FullName", player.UsualPartnerId);

        return View(new ProfileViewModel
        {
            FullName = player.FullName,
            Email = player.Email ?? "",
            MobileNumber = player.MobileNumber,
            WhatsAppOptIn = player.WhatsAppOptIn,
            EmailNotifications = player.EmailNotifications,
            UsualPartnerId = player.UsualPartnerId
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var player = await _userManager.GetUserAsync(User)
            ?? throw new UnauthorizedAccessException();

        player.MobileNumber = model.MobileNumber;
        player.WhatsAppOptIn = model.WhatsAppOptIn;
        player.EmailNotifications = model.EmailNotifications;
        player.UsualPartnerId = string.IsNullOrEmpty(model.UsualPartnerId) ? null : model.UsualPartnerId;

        await _userManager.UpdateAsync(player);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction("Index", "Entry", new { area = "Member" });
    }

    public IActionResult AccessDenied() => View();
}
