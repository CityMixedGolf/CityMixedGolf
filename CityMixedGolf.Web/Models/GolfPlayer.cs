using Microsoft.AspNetCore.Identity;

namespace CityMixedGolf.Web.Models;

public class GolfPlayer : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public Gender Gender { get; set; }
    public decimal HandicapIndex { get; set; }
    public BandColour BandColour { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public string? MobileNumber { get; set; }
    public bool WhatsAppOptIn { get; set; } = false;
    public bool EmailNotifications { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<CompetitionEntry> Entries { get; set; } = new HashSet<CompetitionEntry>();
    public virtual ICollection<DrawPair> DrawPairs { get; set; } = new HashSet<DrawPair>();
    public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
}

public enum Gender { Lady, Gent }
public enum BandColour { Green, Red, Unassigned }