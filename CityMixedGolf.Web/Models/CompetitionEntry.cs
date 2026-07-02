namespace CityMixedGolf.Web.Models;

public class CompetitionEntry
{
    public int Id { get; set; }
    public int CompetitionId { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string? PreferredPartnerId { get; set; }
    public bool EnteringAsSingle { get; set; } = false;
    public TeePreference TeePreference { get; set; } = TeePreference.NoPreference;
    public string? SpecialRequests { get; set; }
    public EntryStatus Status { get; set; } = EntryStatus.Entered;

    /// <summary>
    /// Band colour assigned by admin for this specific competition.
    /// Defaults to Unassigned until admin sets it before the draw.
    /// Allows players at the handicap boundary to be green one competition and red another.
    /// </summary>
    public BandColour BandColour { get; set; } = BandColour.Unassigned;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Competition Competition { get; set; } = null!;
    public virtual GolfPlayer Player { get; set; } = null!;
    public virtual GolfPlayer? PreferredPartner { get; set; }
}

public enum TeePreference { Early, Late, NoPreference }
public enum EntryStatus { Entered, Cancelled, Withdrawn }
