namespace CityMixedGolf.Web.Models;

/// <summary>
/// Mirrors [HomeAdmin].[dbo].[GolfPlayers] — the source of truth for player data.
/// Refreshed via CSV upload in the Admin area.
/// BandColour is NOT stored here — it is assigned per competition on CompetitionEntry.
/// </summary>
public class GolfPlayerRecord
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public decimal HandicapIndex { get; set; }

    /// <summary>"Male" or "Female" — matches the HomeAdmin CSV export format.</summary>
    public string Gender { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation — the linked Identity account (null if not yet registered)
    public virtual GolfPlayer? LinkedAccount { get; set; }

    // Helpers
    public string GenderDisplay => Gender == "Female" ? "Lady" : "Gent";
    public Gender GenderEnum => Gender == "Female" ? Models.Gender.Lady : Models.Gender.Gent;
}
