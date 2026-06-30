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

        // ── Players ──
        var playerSeeds = new (string First, string Last, Gender Gender, decimal Hcp, BandColour Band)[]
        {
            ("Sarah", "Mitchell", Gender.Lady, 14.2m, BandColour.Green),
            ("Anne", "Blackwell", Gender.Lady, 16.1m, BandColour.Green),
            ("Helen", "Ward", Gender.Lady, 12.8m, BandColour.Green),
            ("Jane", "Cooper", Gender.Lady, 15.5m, BandColour.Green),
            ("Claire", "Forsyth", Gender.Lady, 13.9m, BandColour.Green),
            ("Margaret", "Holt", Gender.Lady, 19.4m, BandColour.Red),
            ("Diane", "Pearce", Gender.Lady, 21.0m, BandColour.Red),
            ("James", "Thornton", Gender.Gent, 18.5m, BandColour.Red),
            ("David", "Park", Gender.Gent, 19.0m, BandColour.Red),
            ("Tom", "Bradley", Gender.Gent, 20.4m, BandColour.Red),
            ("Robert", "Hughes", Gender.Gent, 17.2m, BandColour.Red),
            ("Neil", "Watson", Gender.Gent, 22.1m, BandColour.Red),
            ("Mark", "Ellison", Gender.Gent, 11.6m, BandColour.Green),
            ("Paul", "Carrick", Gender.Gent, 13.0m, BandColour.Green),
        };

        var players = new List<GolfPlayer>();
        foreach (var (first, last, gender, hcp, band) in playerSeeds)
        {
            var email = $"{first.ToLower()}.{last.ToLower()}@example.com";
            var player = new GolfPlayer
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = first,
                LastName = last,
                Gender = gender,
                HandicapIndex = hcp,
                BandColour = band,
                IsActive = true,
                EmailNotifications = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            };
            var result = await userManager.CreateAsync(player, "Password1!");
            if (result.Succeeded)
                players.Add(player);
        }

        if (players.Count == 0) return; // creation failed, bail out

        GolfPlayer Find(string first, string last) =>
            players.First(p => p.FirstName == first && p.LastName == last);

        // ── Past competition with published results ──
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

        var pastPairs = new (string GFirst, string GLast, string RFirst, string RLast, int Score, int Pos, int Points)[]
        {
            ("Sarah", "Mitchell", "James", "Thornton", 38, 1, 20),
            ("Helen", "Ward", "Robert", "Hughes", 36, 2, 16),
            ("Claire", "Forsyth", "David", "Park", 35, 3, 12),
            ("Anne", "Blackwell", "Tom", "Bradley", 33, 4, 8),
            ("Jane", "Cooper", "Neil", "Watson", 31, 5, 4),
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
        foreach (var (gf, gl, rf, rl, score, pos, points) in pastPairs)
        {
            var green = Find(gf, gl);
            var red = Find(rf, rl);

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

            db.CompetitionEntries.Add(new CompetitionEntry
            {
                CompetitionId = pastComp.Id,
                PlayerId = green.Id,
                Status = EntryStatus.Entered,
                TeePreference = TeePreference.NoPreference,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            });
            db.CompetitionEntries.Add(new CompetitionEntry
            {
                CompetitionId = pastComp.Id,
                PlayerId = red.Id,
                Status = EntryStatus.Entered,
                TeePreference = TeePreference.NoPreference,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            });
        }
        await db.SaveChangesAsync();

        // ── Second past competition for OOM accumulation ──
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

        var pastPairs2 = new (string GFirst, string GLast, string RFirst, string RLast, int Score, int Pos, int Points)[]
        {
            ("Claire", "Forsyth", "David", "Park", 35, 1, 20),
            ("Sarah", "Mitchell", "Neil", "Watson", 34, 2, 16),
            ("Mark", "Ellison", "Margaret", "Holt", 33, 3, 12),
            ("Helen", "Ward", "Tom", "Bradley", 30, 4, 8),
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
        foreach (var (gf, gl, rf, rl, score, pos, points) in pastPairs2)
        {
            var green = Find(gf, gl);
            var red = Find(rf, rl);

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
        }
        await db.SaveChangesAsync();

        // ── Open competition for sign-ups (Captains Mixed) ──
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

        // A handful of entries so the homepage shows "X entered"
        var enteredNames = new[] { ("Sarah", "Mitchell"), ("James", "Thornton"), ("Helen", "Ward"), ("Robert", "Hughes") };
        foreach (var (f, l) in enteredNames)
        {
            var p = Find(f, l);
            db.CompetitionEntries.Add(new CompetitionEntry
            {
                CompetitionId = openComp.Id,
                PlayerId = p.Id,
                Status = EntryStatus.Entered,
                TeePreference = TeePreference.NoPreference,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
        }
        await db.SaveChangesAsync();

        // ── Future competitions (not yet open) ──
        db.Competitions.Add(new Competition
        {
            Name = "Summer Cup",
            CompetitionDate = DateTime.UtcNow.AddDays(40),
            Format = "Strokeplay",
            EntryOpenDate = DateTime.UtcNow.AddDays(26),
            EntryCloseDate = DateTime.UtcNow.AddDays(37),
            Status = CompetitionStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        });
        db.Competitions.Add(new Competition
        {
            Name = "Autumn Mixed",
            CompetitionDate = DateTime.UtcNow.AddDays(75),
            Format = "Betterball Stableford",
            EntryOpenDate = DateTime.UtcNow.AddDays(60),
            EntryCloseDate = DateTime.UtcNow.AddDays(71),
            Status = CompetitionStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        });
        await db.SaveChangesAsync();

        // ── Promote a demo admin ──
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
                Gender = Gender.Gent,
                HandicapIndex = 0,
                BandColour = BandColour.Unassigned,
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

