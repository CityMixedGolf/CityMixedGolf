namespace CityMixedGolf.Web.Models;

public class DrawPair
{
    public int Id { get; set; }
    public int GroupDrawId { get; set; }
    public string GreenBandPlayerId { get; set; } = string.Empty;
    public string RedBandPlayerId { get; set; } = string.Empty;
    public int PairNumber { get; set; }
    public TeePreference AssignedTee { get; set; } = TeePreference.NoPreference;
    public DrawPairStatus PairStatus { get; set; } = DrawPairStatus.Auto;
    public string? ConflictNote { get; set; }

    // Results
    public int? Score { get; set; }
    public int? Position { get; set; }
    public int? OrderOfMeritPoints { get; set; }

    public virtual GroupDraw GroupDraw { get; set; } = null!;
    public virtual GolfPlayer GreenBandPlayer { get; set; } = null!;
    public virtual GolfPlayer RedBandPlayer { get; set; } = null!;
}

public enum DrawPairStatus { Auto, AutoConflict, ManualOverride }