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

        ModelState.AddModelError("", result.IsLockedOut
            ? "Account locked. Please try again in a few minutes."
            : "Invalid email or password.");

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

        // Find the GolfPlayerRecord they selected
        var playerRecord = await _db.GolfPlayerRecords
            .FirstOrDefaultAsync(p => p.Id == model.PlayerId && p.IsActive);

        if (playerRecord == null)
        {
            ModelState.AddModelError("PlayerId", "Player not found. Please contact the admin.");
            await LoadRegisterViewBag(model.PlayerId);
            return View(model);
        }

        // Check this player record isn't already claimed
        var alreadyClaimed = await _db.Users
            .AnyAsync(u => u.GolfPlayerRecordId == model.PlayerId);

        if (alreadyClaimed)
        {
            ModelState.AddModelError("PlayerId", "This player already has a registered account. Contact admin if this is an error.");
            await LoadRegisterViewBag(model.PlayerId);
            return View(model);
        }

        // Check email isn't taken
        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError("Email", "An account with this email already exists.");
            await LoadRegisterViewBag(model.PlayerId);
            return View(model);
        }

        // Split FullName into First/Last for display
        var nameParts = playerRecord.FullName.Trim().Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        // Resolve UsualPartnerId — store as the GolfPlayer identity Id of the partner
        // We'll look up by GolfPlayerRecordId
        string? usualPartnerIdentityId = null;
        if (model.UsualPartnerId.HasValue)
        {
            var partnerAccount = await _db.Users
                .FirstOrDefaultAsync(u => u.GolfPlayerRecordId == model.UsualPartnerId.Value);
            usualPartnerIdentityId = partnerAccount?.Id;
            // If partner hasn't registered yet, we store null for now
            // The partner preference is also stored on PlayerRecord for display during registration
        }

        var player = new GolfPlayer
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            GolfPlayerRecordId = model.PlayerId,
            IsActive = true,
            EmailNotifications = true,
            UsualPartnerId = usualPartnerIdentityId,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(player, model.Password);

        if (result.Succeeded)
        {
            // Store the usual partner preference on the record too (by record id)
            // so it persists even if partner registers later
            playerRecord.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _signInManager.SignInAsync(player, isPersistent: false);
            TempData["Success"] = $"Welcome, {player.FirstName}! Your account is ready.";
            return RedirectToAction("Index", "Entry", new { area = "Member" });
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        await LoadRegisterViewBag(model.PlayerId);
        return View(model);
    }

    private async Task LoadRegisterViewBag(int? selectedId)
    {
        // Only show active player records that haven't been claimed yet
        var unclaimed = await _db.GolfPlayerRecords
            .Where(p => p.IsActive && !_db.Users.Any(u => u.GolfPlayerRecordId == p.Id))
            .OrderBy(p => p.FullName)
            .ToListAsync();

        ViewBag.Players = new SelectList(unclaimed, "Id", "FullName", selectedId);
        ViewBag.AllPartners = await _db.GolfPlayerRecords
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .ToListAsync();
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

        // Partners: opposite gender from GolfPlayerRecords
        var myRecord = player.GolfPlayerRecordId.HasValue
            ? await _db.GolfPlayerRecords.FindAsync(player.GolfPlayerRecordId.Value)
            : null;

        var oppositeGender = myRecord?.Gender == "Female" ? "Male" : "Female";

        var partners = await _db.GolfPlayerRecords
            .Where(p => p.IsActive && p.Gender == oppositeGender)
            .OrderBy(p => p.FullName)
            .ToListAsync();

        // Find current usual partner record id
        int? currentUsualRecordId = null;
        if (player.UsualPartnerId != null)
        {
            var usualPartner = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == player.UsualPartnerId);
            currentUsualRecordId = usualPartner?.GolfPlayerRecordId;
        }

        ViewBag.Partners = new SelectList(partners, "Id", "FullName", currentUsualRecordId);

        return View(new ProfileViewModel
        {
            FullName = player.FullName,
            Email = player.Email ?? "",
            MobileNumber = player.MobileNumber,
            WhatsAppOptIn = player.WhatsAppOptIn,
            EmailNotifications = player.EmailNotifications,
            UsualPartnerId = currentUsualRecordId
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

        // Resolve UsualPartnerId from GolfPlayerRecord.Id to GolfPlayer identity Id
        if (model.UsualPartnerId.HasValue)
        {
            var partnerAccount = await _db.Users
                .FirstOrDefaultAsync(u => u.GolfPlayerRecordId == model.UsualPartnerId.Value);
            player.UsualPartnerId = partnerAccount?.Id;
        }
        else
        {
            player.UsualPartnerId = null;
        }

        await _userManager.UpdateAsync(player);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction("Index", "Entry", new { area = "Member" });
    }

    public IActionResult AccessDenied() => View();
}
