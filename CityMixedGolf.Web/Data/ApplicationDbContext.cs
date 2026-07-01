using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Models;

namespace CityMixedGolf.Web.Data;

public class ApplicationDbContext : IdentityDbContext<GolfPlayer>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<CompetitionEntry> CompetitionEntries => Set<CompetitionEntry>();
    public DbSet<GroupDraw> GroupDraws => Set<GroupDraw>();
    public DbSet<DrawPair> DrawPairs => Set<DrawPair>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<GolfPlayerRecord> GolfPlayerRecords => Set<GolfPlayerRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<GolfPlayer>(e =>
        {
            e.Property(p => p.HandicapIndex).HasPrecision(5, 1);
            e.Property(p => p.FirstName).HasMaxLength(100);
            e.Property(p => p.LastName).HasMaxLength(100);
            e.Property(p => p.MobileNumber).HasMaxLength(20);
        });

        builder.Entity<CompetitionEntry>(e =>
        {
            e.HasOne(ce => ce.Competition)
             .WithMany(c => c.Entries)
             .HasForeignKey(ce => ce.CompetitionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ce => ce.Player)
             .WithMany(p => p.Entries)
             .HasForeignKey(ce => ce.PlayerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ce => ce.PreferredPartner)
             .WithMany()
             .HasForeignKey(ce => ce.PreferredPartnerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<DrawPair>(e =>
        {
            e.HasOne(dp => dp.GroupDraw)
             .WithMany(gd => gd.Pairs)
             .HasForeignKey(dp => dp.GroupDrawId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(dp => dp.GreenBandPlayer)
             .WithMany()
             .HasForeignKey(dp => dp.GreenBandPlayerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(dp => dp.RedBandPlayer)
             .WithMany()
             .HasForeignKey(dp => dp.RedBandPlayerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.Player)
             .WithMany(p => p.Notifications)
             .HasForeignKey(n => n.PlayerId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.Competition)
             .WithMany(c => c.Notifications)
             .HasForeignKey(n => n.CompetitionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GroupDraw>(e =>
        {
            e.HasOne(gd => gd.Competition)
             .WithMany(c => c.Draws)
             .HasForeignKey(gd => gd.CompetitionId)
             .OnDelete(DeleteBehavior.Cascade);
        });


        // GolfPlayerRecord — source of truth for player data
        builder.Entity<GolfPlayerRecord>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever(); // preserves HomeAdmin Id
            e.Property(p => p.FullName).IsRequired().HasMaxLength(200);
            e.Property(p => p.Gender).IsRequired().HasMaxLength(10);
            e.Property(p => p.HandicapIndex).HasPrecision(5, 2);
            e.HasOne(p => p.LinkedAccount)
             .WithOne(u => u.PlayerRecord)
             .HasForeignKey<GolfPlayer>(u => u.GolfPlayerRecordId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // UsualPartner self-referencing FK on GolfPlayer
        builder.Entity<GolfPlayer>()
            .HasOne(p => p.UsualPartner)
            .WithMany()
            .HasForeignKey(p => p.UsualPartnerId)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}