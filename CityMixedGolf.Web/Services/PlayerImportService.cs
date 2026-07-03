using CityMixedGolf.Web.Data;
using CityMixedGolf.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CityMixedGolf.Web.Services;

public interface IPlayerImportService
{
    Task<PlayerImportResult> ImportFromCsvAsync(Stream csvStream);
}

public class PlayerImportResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Deactivated { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class PlayerImportService : IPlayerImportService
{
    private readonly ApplicationDbContext _db;

    public PlayerImportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PlayerImportResult> ImportFromCsvAsync(Stream csvStream)
    {
        var result = new PlayerImportResult();
        var importedIds = new HashSet<int>();

        using var reader = new StreamReader(csvStream);
        var header = await reader.ReadLineAsync();

        if (header == null)
        {
            result.Errors.Add("CSV file is empty.");
            return result;
        }

        // Detect column positions from header
        var cols = header.Split(',');
        int idIdx = -1, nameIdx = -1, hcpIdx = -1, genderIdx = -1;

        for (int i = 0; i < cols.Length; i++)
        {
            var col = cols[i].Trim().Trim('"').ToLowerInvariant();
            if (col == "id") idIdx = i;
            else if (col is "fullname" or "full name" or "name") nameIdx = i;
            else if (col is "handicapindex" or "handicap" or "handicap index") hcpIdx = i;
            else if (col is "gender" or "sex") genderIdx = i;
        }

        if (nameIdx == -1 || hcpIdx == -1 || genderIdx == -1)
        {
            result.Errors.Add("CSV must have columns: FullName (or Name), HandicapIndex (or Handicap), Gender. Id is optional.");
            return result;
        }

        var existing = await _db.GolfPlayerRecords.ToListAsync();
        int lineNumber = 1;

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = ParseCsvLine(line);
            if (parts.Length <= Math.Max(nameIdx, Math.Max(hcpIdx, genderIdx)))
            {
                result.Errors.Add($"Line {lineNumber}: not enough columns, skipped.");
                continue;
            }

            var fullName = parts[nameIdx].Trim();
            var genderRaw = parts[genderIdx].Trim();
            var gender = genderRaw.Equals("Female", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";

            if (!decimal.TryParse(parts[hcpIdx].Trim(), out var handicap))
            {
                result.Errors.Add($"Line {lineNumber}: invalid handicap '{parts[hcpIdx]}', skipped.");
                continue;
            }

            // Try to match by Id first, then by FullName
            GolfPlayerRecord? record = null;

            if (idIdx >= 0 && idIdx < parts.Length && int.TryParse(parts[idIdx].Trim(), out var csvId))
            {
                record = existing.FirstOrDefault(p => p.Id == csvId);
                importedIds.Add(csvId);
            }

            if (record == null)
                record = existing.FirstOrDefault(p =>
                    p.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

            if (record == null)
            {
                // New player
                record = new GolfPlayerRecord
                {
                    FullName = fullName,
                    HandicapIndex = handicap,
                    Gender = gender,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                };
                _db.GolfPlayerRecords.Add(record);
                result.Added++;
            }
            else
            {
                record.FullName = fullName;
                record.HandicapIndex = handicap;
                record.Gender = gender;
                record.IsActive = true;
                record.LastUpdated = DateTime.UtcNow;
                result.Updated++;
            }
        }

        await _db.SaveChangesAsync();
        return result;
    }

    /// <summary>Handles quoted CSV fields containing commas.</summary>
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
