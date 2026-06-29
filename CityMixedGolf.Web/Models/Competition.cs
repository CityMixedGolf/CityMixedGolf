namespace CityMixedGolf.Web.Models;

public class Competition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CompetitionDate { get; set; }
    public string Format { get; set; } = string.Empty;
    public DateTime EntryOpenDate { get; set; }
    public DateTime EntryCloseDate { get; set; }
    public CompetitionStatus Status { get; set; } = CompetitionStatus.Draft;
    public int? MaxEntries { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<CompetitionEntry> Entries { get; set; } = new HashSet<CompetitionEntry>();
    public virtual ICollection<GroupDraw> Draws { get; set; } = new HashSet<GroupDraw>();
    public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
}

public enum CompetitionStatus
{
    Draft,
    Open,
    Closed,
    DrawPending,
    DrawPublished,
    ResultsEntered,
    Archived
}