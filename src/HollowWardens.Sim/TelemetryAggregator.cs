using Microsoft.Data.Sqlite;
using HollowWardens.Core.Telemetry;

namespace HollowWardens.Sim;

public static class TelemetryAggregator
{
    public static PlayerProfile Aggregate(string dbPath, string? versionFilter = null)
    {
        if (!File.Exists(dbPath))
            return new PlayerProfile { Name = "empty", Source = "telemetry" };

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        var profile = new PlayerProfile();
        var decisions = QueryDecisions(conn, versionFilter);

        profile.SampleSize = decisions.Count;
        profile.GameVersionFilter = versionFilter;

        if (decisions.Count == 0)
            return profile;

        // Card play distribution: chosen card_id → fraction of card_play decisions
        var cardPlays = decisions.Where(d => d.Type == "card_play").ToList();
        if (cardPlays.Count > 0)
        {
            profile.CardPlayDistribution = cardPlays
                .GroupBy(d => d.Chosen)
                .ToDictionary(g => g.Key, g => g.Count() / (double)cardPlays.Count);
        }

        // Bottom play rate
        var bottomPlays = cardPlays.Count(d => d.CardHalf == "bottom");
        profile.BottomPlayRate = cardPlays.Count > 0
            ? bottomPlays / (double)cardPlays.Count
            : 0.0;

        // Targeting preferences: effect_type → most frequent territory
        var targetDecisions = decisions.Where(d => d.Type == "targeting" && d.ChosenDetail != null).ToList();
        if (targetDecisions.Count > 0)
        {
            profile.TargetingPreference = targetDecisions
                .GroupBy(d => d.ChosenDetail!)
                .ToDictionary(g => g.Key,
                    g => g.GroupBy(d => d.Chosen)
                          .OrderByDescending(x => x.Count())
                          .First().Key);
        }

        // Draft preferences: card_id → fraction of draft picks
        var draftDecisions = decisions.Where(d => d.Type == "draft_pick").ToList();
        if (draftDecisions.Count > 0)
        {
            profile.DraftPreferences = draftDecisions
                .GroupBy(d => d.Chosen)
                .ToDictionary(g => g.Key, g => g.Count() / (double)draftDecisions.Count);
        }

        // Upgrade preferences
        var upgradeDecisions = decisions.Where(d =>
            d.Type == "card_upgrade" || d.Type == "passive_upgrade").ToList();
        if (upgradeDecisions.Count > 0)
        {
            profile.UpgradePreferences = upgradeDecisions
                .GroupBy(d => d.Chosen)
                .ToDictionary(g => g.Key, g => g.Count() / (double)upgradeDecisions.Count);
        }

        // Rest timing
        var restDecisions = decisions.Where(d => d.Type == "rest").ToList();
        int totalTopDecisions = decisions.Count(d => d.Type == "card_play" && d.CardHalf == "top" || d.Type == "rest");
        if (restDecisions.Count > 0 && totalTopDecisions > 0)
        {
            // Count how many rest decisions had 0 options (forced rest)
            int forcedRests = restDecisions.Count(d =>
                string.IsNullOrEmpty(d.OptionsJson) || d.OptionsJson == "[]");
            int voluntaryRests = restDecisions.Count - forcedRests;

            profile.RestTiming = new RestTimingProfile
            {
                ForcedRestPct    = forcedRests / (double)totalTopDecisions,
                VoluntaryRestPct = voluntaryRests / (double)totalTopDecisions,
                AvgCardsInHandAtRest = restDecisions
                    .Where(d => !string.IsNullOrEmpty(d.HandJson))
                    .Select(d => CountJsonArray(d.HandJson!))
                    .DefaultIfEmpty(0)
                    .Average()
            };
        }

        // Event risk tolerance: fraction of event options chosen that are index > 0
        var eventRows = QueryEvents(conn, versionFilter);
        if (eventRows.Count > 0)
        {
            profile.EventRiskTolerance = eventRows.Count(e => e.OptionChosen > 0) / (double)eventRows.Count;
        }

        return profile;
    }

    // ── Query helpers ──────────────────────────────────────────────────────

    private record DecisionRow(string Type, string Chosen, string? ChosenDetail,
        string? OptionsJson, string? CardHalf, string? HandJson);
    private record EventRow(int OptionChosen);

    private static List<DecisionRow> QueryDecisions(SqliteConnection conn, string? versionFilter)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT type, chosen, chosen_detail, options_json, card_half, hand_json " +
                          "FROM decisions" +
                          (versionFilter != null ? " WHERE game_version LIKE @vf" : "");
        if (versionFilter != null)
            cmd.Parameters.AddWithValue("@vf", $"{versionFilter}%");

        var rows = new List<DecisionRow>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new DecisionRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        }
        return rows;
    }

    private static List<EventRow> QueryEvents(SqliteConnection conn, string? versionFilter)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT option_chosen FROM events" +
                          (versionFilter != null ? " WHERE game_version LIKE @vf" : "");
        if (versionFilter != null)
            cmd.Parameters.AddWithValue("@vf", $"{versionFilter}%");

        var rows = new List<EventRow>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            rows.Add(new EventRow(reader.GetInt32(0)));
        return rows;
    }

    private static int CountJsonArray(string json)
    {
        try
        {
            var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(json);
            return arr?.Length ?? 0;
        }
        catch { return 0; }
    }
}
