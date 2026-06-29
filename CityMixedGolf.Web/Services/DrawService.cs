using Microsoft.EntityFrameworkCore;
using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;

namespace CityMixedGolf.Web.Services;

public interface IDrawService
{
    Task<GroupDraw> GenerateDrawAsync(int competitionId, string drawnByUserId);
    Task<DrawPair> SwapPlayersAsync(int drawPairId1, int drawPairId2);
    Task<DrawPair> ManualOverrideAsync(int pairId, string newGreenPlayerId, string newRedPlayerId);
    Task PublishDrawAsync(int groupDrawId);
}

public class DrawService : IDrawService
{
    private readonly ApplicationDbContext _db;

    public DrawService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<GroupDraw> GenerateDrawAsync(int competitionId, string drawnByUserId)
    {
        // Load active entries for this competition
        var entries = await _db.CompetitionEntries
            .Include(e => e.Player)
            .Where(e => e.CompetitionId == competitionId && e.Status == EntryStatus.Entered)
            .ToListAsync();

        var greenPlayers = entries
            .Where(e => e.Player.BandColour == BandColour.Green && !e.EnteringAsSingle)
            .Select(e => e.Player).ToList();

        var redPlayers = entries
            .Where(e => e.Player.BandColour == BandColour.Red && !e.EnteringAsSingle)
            .Select(e => e.Player).ToList();

        var singles = entries.Where(e => e.EnteringAsSingle).Select(e => e.Player).ToList();

        // Load full pairing history for conflict detection
        var pairingHistory = await _db.DrawPairs
            .Include(dp => dp.GroupDraw)
            .Where(dp => dp.GroupDraw.IsPublished)
            .ToListAsync();

        var draw = new GroupDraw
        {
            CompetitionId = competitionId,
            DrawMethod = DrawMethod.Auto,
            DrawnAt = DateTime.UtcNow,
            DrawnByUserId = drawnByUserId
        };

        _db.GroupDraws.Add(draw);
        await _db.SaveChangesAsync();

        var pairs = new List<DrawPair>();
        var usedGreen = new HashSet<string>();
        var usedRed = new HashSet<string>();
        int pairNumber = 1;

        // Shuffle both lists for randomness within same priority tier
        var rng = new Random();
        greenPlayers = greenPlayers.OrderBy(_ => rng.Next()).ToList();
        redPlayers = redPlayers.OrderBy(_ => rng.Next()).ToList();

        // Check if two players have a preferred partner match
        var preferenceMap = entries
            .Where(e => e.PreferredPartnerId != null)
            .ToDictionary(e => e.PlayerId, e => e.PreferredPartnerId!);

        foreach (var green in greenPlayers)
        {
            if (usedGreen.Contains(green.Id)) continue;

            GolfPlayer? bestRed = null;
            DrawPairStatus pairStatus = DrawPairStatus.Auto;
            string? conflictNote = null;

            // Priority 1: mutual preferred partner
            if (preferenceMap.TryGetValue(green.Id, out var preferredId))
            {
                bestRed = redPlayers.FirstOrDefault(r => r.Id == preferredId && !usedRed.Contains(r.Id));
            }

            // Priority 2: never played together (cross-colour)
            if (bestRed == null)
            {
                bestRed = redPlayers
                    .Where(r => !usedRed.Contains(r.Id))
                    .OrderBy(r => LastPlayedTogether(pairingHistory, green.Id, r.Id) ?? DateTime.MinValue)
                    .FirstOrDefault();

                if (bestRed != null)
                {
                    var lastPlayed = LastPlayedTogether(pairingHistory, green.Id, bestRed.Id);
                    if (lastPlayed.HasValue)
                    {
                        pairStatus = DrawPairStatus.AutoConflict;
                        conflictNote = $"Last played together {lastPlayed.Value:dd MMM yyyy}";
                    }
                }
            }

            if (bestRed == null) continue;

            // Check preferred tee — try to honour both players preferences
            var greenEntry = entries.First(e => e.PlayerId == green.Id);
            var redEntry = entries.First(e => e.PlayerId == bestRed.Id);
            var assignedTee = ResolveTee(greenEntry.TeePreference, redEntry.TeePreference);

            pairs.Add(new DrawPair
            {
                GroupDrawId = draw.Id,
                GreenBandPlayerId = green.Id,
                RedBandPlayerId = bestRed.Id,
                PairNumber = pairNumber++,
                AssignedTee = assignedTee,
                PairStatus = pairStatus,
                ConflictNote = conflictNote
            });

            usedGreen.Add(green.Id);
            usedRed.Add(bestRed.Id);
        }

        _db.DrawPairs.AddRange(pairs);
        await _db.SaveChangesAsync();

        return draw;
    }

    public async Task<DrawPair> SwapPlayersAsync(int pairId1, int pairId2)
    {
        var pair1 = await _db.DrawPairs.FindAsync(pairId1) ?? throw new InvalidOperationException("Pair not found");
        var pair2 = await _db.DrawPairs.FindAsync(pairId2) ?? throw new InvalidOperationException("Pair not found");

        // Swap red band players between two pairs
        (pair1.RedBandPlayerId, pair2.RedBandPlayerId) = (pair2.RedBandPlayerId, pair1.RedBandPlayerId);
        pair1.PairStatus = DrawPairStatus.ManualOverride;
        pair2.PairStatus = DrawPairStatus.ManualOverride;

        await _db.SaveChangesAsync();
        return pair1;
    }

    public async Task<DrawPair> ManualOverrideAsync(int pairId, string newGreenPlayerId, string newRedPlayerId)
    {
        var pair = await _db.DrawPairs.FindAsync(pairId) ?? throw new InvalidOperationException("Pair not found");
        pair.GreenBandPlayerId = newGreenPlayerId;
        pair.RedBandPlayerId = newRedPlayerId;
        pair.PairStatus = DrawPairStatus.ManualOverride;
        pair.ConflictNote = null;
        await _db.SaveChangesAsync();
        return pair;
    }

    public async Task PublishDrawAsync(int groupDrawId)
    {
        var draw = await _db.GroupDraws.FindAsync(groupDrawId) ?? throw new InvalidOperationException("Draw not found");
        draw.IsPublished = true;
        draw.PublishedAt = DateTime.UtcNow;

        var competition = await _db.Competitions.FindAsync(draw.CompetitionId);
        if (competition != null)
            competition.Status = CompetitionStatus.DrawPublished;

        await _db.SaveChangesAsync();
    }

    // Returns the most recent date two players were paired, or null if never
    private static DateTime? LastPlayedTogether(List<DrawPair> history, string playerId1, string playerId2)
    {
        return history
            .Where(dp =>
                (dp.GreenBandPlayerId == playerId1 && dp.RedBandPlayerId == playerId2) ||
                (dp.GreenBandPlayerId == playerId2 && dp.RedBandPlayerId == playerId1))
            .OrderByDescending(dp => dp.GroupDraw?.DrawnAt)
            .Select(dp => dp.GroupDraw?.DrawnAt)
            .FirstOrDefault();
    }

    private static TeePreference ResolveTee(TeePreference a, TeePreference b)
    {
        if (a == b) return a;
        if (a == TeePreference.NoPreference) return b;
        if (b == TeePreference.NoPreference) return a;
        return TeePreference.NoPreference; // conflict — admin to decide
    }
}