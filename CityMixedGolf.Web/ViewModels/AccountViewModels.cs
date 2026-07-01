using System.ComponentModel.DataAnnotations;

namespace CityMixedGolf.Web.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    /// <summary>Id from GolfPlayerRecord — the player list entry to claim.</summary>
    [Required(ErrorMessage = "Please select your name from the list.")]
    [Display(Name = "Your name")]
    public int PlayerId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>GolfPlayerRecord.Id of the usual partner (opposite gender).</summary>
    [Display(Name = "Usual playing partner")]
    public int? UsualPartnerId { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public bool WhatsAppOptIn { get; set; }
    public bool EmailNotifications { get; set; }
    public int? UsualPartnerId { get; set; }
}
