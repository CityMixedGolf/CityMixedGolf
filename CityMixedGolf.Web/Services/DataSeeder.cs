using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;

namespace CityMixedGolf.Web.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<GolfPlayer>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        // ── Roles ──
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        // Don't reseed if we already have competitions
        if (await db.Competitions.AnyAsync()) return;

        // ── Step 1: Create GolfPlayerRecords (source of truth) ──
        var recordSeeds = new (int Id, string FullName, decimal Hcp, string Gender)[]
        {
            (1,  "Sarah Mitchell",  14.2m, "Female"),
            (2,  "Anne Blackwell",  16.1m, "Female"),
            (3,  "Helen Ward",      12.8m, "Female"),
            (4,  "Jane Cooper",     15.5m, "Female"),
            (5,  "Claire Forsyth",  13.9m, "Female"),
            (6,  "Margaret Holt",   19.4m, "Female"),
            (7,  "Diane Pearce",    21.0m, "Female"),
            (8,  "James Thornton",  18.5m, "Male"),
            (9,  "David Park",      19.0m, "Male"),
            (10, "Tom Bradley",     20.4m, "Male"),
            (11, "Robert Hughes",   17.2m, "Male"),
            (12, "Neil Watson",     22.1m, "Male"),
            (13, "Mark Ellison",    11.6m, "Male"),
            (14, "Paul Carrick",    13.0m, "Male"),
        };

        if (!await db.GolfPlayerRecords.AnyAsync())
        {
            foreach (var (id, fullName, hcp, gender) in recordSeeds)
            {
                db.GolfPlayerRecords.Add(new GolfPlayerRecord
                {
                    Id = id,
                    FullName = fullName,
                    HandicapIndex = hcp,
                    Gender = gender,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow.AddMonths(-6)
                });
            }
            await db.SaveChangesAsync();
        }

        var records = await db.GolfPlayerRecords.ToListAsync();
        GolfPlayerRecord Record(string fullName) =>
            records.First(r => r.FullName == fullName);

        // ── Step 2: Create GolfPlayer (Identity) accounts linked to records ──
        var players = new List<GolfPlayer>();

        async Task<GolfPlayer?> CreatePlayer(string fullName, string password = "Password1!")
        {
            var rec = Record(fullName);
            var nameParts = fullName.Trim().Split(' ', 2);
            var email = $"{nameParts[0].ToLower()}.{(nameParts.Length > 1 ? nameParts[1].ToLower().Replace(" ", "") : "player")}@example.com";

            var player = new GolfPlayer
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : "",
                GolfPlayerRecordId = rec.Id,
                IsActive = true,
                EmailNotifications = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            };

            var result = await userManager.CreateAsync(player, password);
            return result.Succeeded ? player : null;
        }

        var playerNames = new[]
        {
            "Sarah Mitchell", "Anne Blackwell", "Helen Ward", "Jane Cooper",
            "Claire Forsyth", "Margaret Holt", "Diane Pearce",
            "James Thornton", "David Park", "Tom Bradley",
            "Robert Hughes", "Neil Watson", "Mark Ellison", "Paul Carrick"
        };

        foreach (var name in playerNames)
        {
            var p = await CreatePlayer(name);
            if (p != null) players.Add(p);
        }

        if (players.Count == 0) return;

        GolfPlayer Find(string fullName) =>
            players.First(p => p.FirstName + " " + p.LastName == fullName);

        // ── Step 3: Past competition with published results ──
        var pastComp = new Competition
        {
            Name = "Summer Mixed",
            CompetitionDate = DateTime.UtcNow.AddDays(-18),
            Format = "Betterball Stableford",
            EntryOpenDate = DateTime.UtcNow.AddDays(-32),
            EntryCloseDate = DateTime.UtcNow.AddDays(-21),
            Status = CompetitionStatus.ResultsEntered,
            CreatedAt = DateTime.UtcNow.AddDays(-35)
        };
        db.Competitions.Add(pastComp);
        await db.SaveChangesAsync();

        var pastPairs = new (string GFirst, string GLast, int Score, int Pos, int Points)[]
        {
            ("Sarah Mitchell", "James Thornton", 38, 1, 20),
            ("Helen Ward",     "Robert Hughes",  36, 2, 16),
            ("Claire Forsyth", "David Park",     35, 3, 12),
            ("Anne Blackwell", "Tom Bradley",    33, 4, 8),
            ("Jane Cooper",    "Neil Watson",    31, 5, 4),
        };

        var pastDraw = new GroupDraw
        {
            CompetitionId = pastComp.Id,
            DrawMethod = DrawMethod.Auto,
            DrawnAt = DateTime.UtcNow.AddDays(-22),
            IsPublished = true,
            PublishedAt = DateTime.UtcNow.AddDays(-22)
        };
        db.GroupDraws.Add(pastDraw);
        await db.SaveChangesAsync();

        int pairNum = 1;
        foreach (var (greenName, redName, score, pos, points) in pastPairs)
        {
            var green = Find(greenName);
            var red = Find(redName);
            db.DrawPairs.Add(new DrawPair
            {
                GroupDrawId = pastDraw.Id,
                GreenBandPlayerId = green.Id,
                RedBandPlayerId = red.Id,
                PairNumber = pairNum++,
                AssignedTee = pairNum % 2 == 0 ? TeePreference.Early : TeePreference.Late,
                PairStatus = DrawPairStatus.Auto,
                Score = score,
                Position = pos,
                OrderOfMeritPoints = points
            });
            db.CompetitionEntries.AddRange(
                new CompetitionEntry { CompetitionId = pastComp.Id, PlayerId = green.Id, Status = EntryStatus.Entered, BandColour = BandColour.Green, TeePreference = TeePreference.NoPreference, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new CompetitionEntry { CompetitionId = pastComp.Id, PlayerId = red.Id,   Status = EntryStatus.Entered, BandColour = BandColour.Red,   TeePreference = TeePreference.NoPreference, CreatedAt = DateTime.UtcNow.AddDays(-30) }
            );
        }
        await db.SaveChangesAsync();

        // ── Step 4: Second past competition ──
        var pastComp2 = new Competition
        {
            Name = "Midsummer Mixed",
            CompetitionDate = DateTime.UtcNow.AddDays(-40),
            Format = "Strokeplay",
            EntryOpenDate = DateTime.UtcNow.AddDays(-54),
            EntryCloseDate = DateTime.UtcNow.AddDays(-43),
            Status = CompetitionStatus.ResultsEntered,
            CreatedAt = DateTime.UtcNow.AddDays(-57)
        };
        db.Competitions.Add(pastComp2);
        await db.SaveChangesAsync();

        var pastPairs2 = new (string GreenName, string RedName, int Score, int Pos, int Points)[]
        {
            ("Claire Forsyth", "David Park",    35, 1, 20),
            ("Sarah Mitchell", "Neil Watson",   34, 2, 16),
            ("Mark Ellison",   "Margaret Holt", 33, 3, 12),
            ("Helen Ward",     "Tom Bradley",   30, 4, 8),
        };

        var pastDraw2 = new GroupDraw
        {
            CompetitionId = pastComp2.Id,
            DrawMethod = DrawMethod.Auto,
            DrawnAt = DateTime.UtcNow.AddDays(-44),
            IsPublished = true,
            PublishedAt = DateTime.UtcNow.AddDays(-44)
        };
        db.GroupDraws.Add(pastDraw2);
        await db.SaveChangesAsync();

        pairNum = 1;
        foreach (var (greenName, redName, score, pos, points) in pastPairs2)
        {
            var green = Find(greenName);
            var red = Find(redName);
            db.DrawPairs.Add(new DrawPair
            {
                GroupDrawId = pastDraw2.Id,
                GreenBandPlayerId = green.Id,
                RedBandPlayerId = red.Id,
                PairNumber = pairNum++,
                AssignedTee = TeePreference.NoPreference,
                PairStatus = DrawPairStatus.Auto,
                Score = score,
                Position = pos,
                OrderOfMeritPoints = points
            });
            // Note: no CompetitionEntries for pastComp2 as they were added with the draw pairs directly
        }
        await db.SaveChangesAsync();

        // ── Step 5: Open competition ──
        var openComp = new Competition
        {
            Name = "Captains Mixed",
            CompetitionDate = DateTime.UtcNow.AddDays(19),
            Format = "Betterball Stableford",
            EntryOpenDate = DateTime.UtcNow.AddDays(-5),
            EntryCloseDate = DateTime.UtcNow.AddDays(16),
            Status = CompetitionStatus.Open,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };
        db.Competitions.Add(openComp);
        await db.SaveChangesAsync();

        var openEntryData = new (string Name, BandColour Band)[]
        {
            ("Sarah Mitchell", BandColour.Green),
            ("James Thornton", BandColour.Red),
            ("Helen Ward",     BandColour.Green),
            ("Robert Hughes",  BandColour.Red),
        };
        foreach (var (name, band) in openEntryData)
        {
            db.CompetitionEntries.Add(new CompetitionEntry
            {
                CompetitionId = openComp.Id,
                PlayerId = Find(name).Id,
                Status = EntryStatus.Entered,
                BandColour = band,
                TeePreference = TeePreference.NoPreference,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
        }
        await db.SaveChangesAsync();

        // ── Step 6: Future competitions ──
        db.Competitions.AddRange(
            new Competition { Name = "Summer Cup", CompetitionDate = DateTime.UtcNow.AddDays(40), Format = "Strokeplay", EntryOpenDate = DateTime.UtcNow.AddDays(26), EntryCloseDate = DateTime.UtcNow.AddDays(37), Status = CompetitionStatus.Draft, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Competition { Name = "Autumn Mixed", CompetitionDate = DateTime.UtcNow.AddDays(75), Format = "Betterball Stableford", EntryOpenDate = DateTime.UtcNow.AddDays(60), EntryCloseDate = DateTime.UtcNow.AddDays(71), Status = CompetitionStatus.Draft, CreatedAt = DateTime.UtcNow.AddDays(-3) }
        );
        await db.SaveChangesAsync();

        // ── Step 7: Admin account ──
        var adminEmail = "citymixedgolf@gmail.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new GolfPlayer
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Club",
                LastName = "Admin",
                GolfPlayerRecordId = null, // admin is not a player record
                IsActive = true,
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(admin, "AdminPass1!");
        }
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
