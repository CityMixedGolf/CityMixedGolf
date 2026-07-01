using Microsoft.AspNetCore.Identity;

namespace CityMixedGolf.Web.Models;

public class GolfPlayer : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";

    // Link to the GolfPlayerRecord (source of truth for handicap/gender/band)
    public int? GolfPlayerRecordId { get; set; }
    public virtual GolfPlayerRecord? PlayerRecord { get; set; }

    // Convenience properties that read from PlayerRecord if linked
    public decimal HandicapIndex => PlayerRecord?.HandicapIndex ?? 0;
    public Gender Gender => PlayerRecord?.GenderEnum ?? Models.Gender.Gent;
    public BandColour BandColour => PlayerRecord?.BandColour ?? Models.BandColour.Unassigned;

    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public string? MobileNumber { get; set; }
    public bool WhatsAppOptIn { get; set; } = false;
    public bool EmailNotifications { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? UsualPartnerId { get; set; }
    public virtual GolfPlayer? UsualPartner { get; set; }

    public virtual ICollection<CompetitionEntry> Entries { get; set; } = new HashSet<CompetitionEntry>();
    public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
}

public enum Gender { Lady, Gent }
public enum BandColour { Green, Red, Unassigned }
