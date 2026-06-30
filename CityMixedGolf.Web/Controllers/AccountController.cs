using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CityMixedGolf.Web.Models;
using CityMixedGolf.Web.ViewModels;

namespace CityMixedGolf.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<GolfPlayer> _userManager;
    private readonly SignInManager<GolfPlayer> _signInManager;

    public AccountController(UserManager<GolfPlayer> userManager, SignInManager<GolfPlayer> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

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

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home", new { area = "Public" });

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var player = new GolfPlayer
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            HandicapIndex = model.HandicapIndex,
            BandColour = model.HandicapIndex <= 18 ? BandColour.Green : BandColour.Red,
            IsActive = true,
            EmailNotifications = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(player, model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(player, isPersistent: false);
            TempData["Success"] = $"Welcome, {player.FirstName}! Your account has been created.";
            return RedirectToAction("Index", "Entry", new { area = "Member" });
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "Public" });
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
