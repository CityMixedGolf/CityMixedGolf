namespace CityMixedGolf.Web.Models;

public class GroupDraw
{
    public int Id { get; set; }
    public int CompetitionId { get; set; }
    public DrawMethod DrawMethod { get; set; } = DrawMethod.Auto;
    public DateTime DrawnAt { get; set; } = DateTime.UtcNow;
    public string? DrawnByUserId { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    public virtual Competition Competition { get; set; } = null!;
    public virtual ICollection<DrawPair> Pairs { get; set; } = new HashSet<DrawPair>();
}

public enum DrawMethod { Auto, Manual, AutoOverridden }