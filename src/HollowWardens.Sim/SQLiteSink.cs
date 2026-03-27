using Microsoft.Data.Sqlite;
using HollowWardens.Core.Telemetry;

namespace HollowWardens.Sim;

public class SQLiteSink : ITelemetrySink
{
    private readonly SqliteConnection _conn;

    public SQLiteSink(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        CreateTables();
    }

    private void CreateTables()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA busy_timeout=5000;
            CREATE TABLE IF NOT EXISTS runs (
                run_id TEXT PRIMARY KEY,
                player_id TEXT,
                source TEXT,
                game_version TEXT,
                balance_hash TEXT,
                schema_version INTEGER,
                timestamp TEXT,
                warden TEXT,
                mode TEXT,
                realm TEXT,
                seed INTEGER,
                result TEXT,
                encounters_completed INTEGER,
                final_max_weave INTEGER,
                final_weave INTEGER,
                cards_drafted INTEGER,
                cards_upgraded INTEGER,
                cards_removed INTEGER,
                passives_upgraded INTEGER,
                passives_unlocked INTEGER,
                tokens_earned INTEGER,
                tokens_spent INTEGER,
                duration_seconds REAL,
                path_json TEXT
            );

            CREATE TABLE IF NOT EXISTS encounters (
                encounter_uid TEXT PRIMARY KEY,
                run_id TEXT,
                encounter_index INTEGER,
                encounter_id TEXT,
                board_layout TEXT,
                game_version TEXT,
                balance_hash TEXT,
                schema_version INTEGER,
                source TEXT,
                result TEXT,
                reward_tier TEXT,
                tides_completed INTEGER,
                final_weave INTEGER,
                max_weave_at_start INTEGER,
                invaders_killed INTEGER,
                natives_killed INTEGER,
                heart_damage_events INTEGER,
                peak_corruption INTEGER,
                total_corruption_at_end INTEGER,
                total_presence_at_end INTEGER,
                total_fear_generated INTEGER,
                sacrifices INTEGER,
                passives_unlocked_json TEXT,
                duration_seconds REAL,
                export_string TEXT
            );

            CREATE TABLE IF NOT EXISTS decisions (
                decision_id INTEGER PRIMARY KEY AUTOINCREMENT,
                run_id TEXT,
                encounter_index INTEGER,
                tide INTEGER,
                phase TEXT,
                turn_number INTEGER,
                timestamp_ms INTEGER,
                type TEXT,
                game_version TEXT,
                balance_hash TEXT,
                schema_version INTEGER,
                source TEXT,
                options_json TEXT,
                chosen TEXT,
                chosen_detail TEXT,
                weave INTEGER,
                max_weave INTEGER,
                hand_json TEXT,
                board_json TEXT,
                card_id TEXT,
                card_half TEXT,
                target_territory TEXT,
                elements_before TEXT,
                elements_after TEXT
            );

            CREATE TABLE IF NOT EXISTS tide_snapshots (
                run_id TEXT,
                encounter_index INTEGER,
                tide INTEGER,
                game_version TEXT,
                balance_hash TEXT,
                schema_version INTEGER,
                source TEXT,
                weave INTEGER,
                max_weave INTEGER,
                alive_invaders INTEGER,
                total_presence INTEGER,
                total_corruption INTEGER,
                fear_generated INTEGER,
                invaders_killed INTEGER,
                invaders_arrived INTEGER,
                cards_in_hand INTEGER,
                cards_in_deck INTEGER,
                cards_dissolved INTEGER,
                PRIMARY KEY (run_id, encounter_index, tide)
            );

            CREATE TABLE IF NOT EXISTS events (
                event_uid TEXT PRIMARY KEY,
                run_id TEXT,
                after_encounter_index INTEGER,
                event_id TEXT,
                event_type TEXT,
                game_version TEXT,
                balance_hash TEXT,
                schema_version INTEGER,
                source TEXT,
                option_chosen INTEGER,
                effects_json TEXT,
                weave_before INTEGER,
                weave_after INTEGER,
                tokens_before INTEGER,
                tokens_after INTEGER
            );

            CREATE INDEX IF NOT EXISTS idx_encounters_run ON encounters(run_id);
            CREATE INDEX IF NOT EXISTS idx_decisions_run ON decisions(run_id);
            CREATE INDEX IF NOT EXISTS idx_decisions_type ON decisions(type);
            CREATE INDEX IF NOT EXISTS idx_tide_snapshots_run ON tide_snapshots(run_id);
            CREATE INDEX IF NOT EXISTS idx_events_run ON events(run_id);
            """;
        cmd.ExecuteNonQuery();
    }

    public void WriteRun(RunRecord r)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO runs VALUES (
            @runId, @playerId, @source, @gameVersion, @balanceHash, @schemaVersion,
            @timestamp, @warden, @mode, @realm, @seed, @result,
            @encountersCompleted, @finalMaxWeave, @finalWeave,
            @cardsDrafted, @cardsUpgraded, @cardsRemoved,
            @passivesUpgraded, @passivesUnlocked, @tokensEarned, @tokensSpent,
            @durationSeconds, @pathJson)";
        cmd.Parameters.AddWithValue("@runId", r.RunId);
        cmd.Parameters.AddWithValue("@playerId", r.PlayerId);
        cmd.Parameters.AddWithValue("@source", r.Source);
        cmd.Parameters.AddWithValue("@gameVersion", r.GameVersion);
        cmd.Parameters.AddWithValue("@balanceHash", r.BalanceHash);
        cmd.Parameters.AddWithValue("@schemaVersion", r.SchemaVersion);
        cmd.Parameters.AddWithValue("@timestamp", r.Timestamp);
        cmd.Parameters.AddWithValue("@warden", r.Warden);
        cmd.Parameters.AddWithValue("@mode", r.Mode);
        cmd.Parameters.AddWithValue("@realm", (object?)r.Realm ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@seed", r.Seed);
        cmd.Parameters.AddWithValue("@result", r.Result);
        cmd.Parameters.AddWithValue("@encountersCompleted", r.EncountersCompleted);
        cmd.Parameters.AddWithValue("@finalMaxWeave", r.FinalMaxWeave);
        cmd.Parameters.AddWithValue("@finalWeave", r.FinalWeave);
        cmd.Parameters.AddWithValue("@cardsDrafted", r.CardsDrafted);
        cmd.Parameters.AddWithValue("@cardsUpgraded", r.CardsUpgraded);
        cmd.Parameters.AddWithValue("@cardsRemoved", r.CardsRemoved);
        cmd.Parameters.AddWithValue("@passivesUpgraded", r.PassivesUpgraded);
        cmd.Parameters.AddWithValue("@passivesUnlocked", r.PassivesUnlocked);
        cmd.Parameters.AddWithValue("@tokensEarned", r.TokensEarned);
        cmd.Parameters.AddWithValue("@tokensSpent", r.TokensSpent);
        cmd.Parameters.AddWithValue("@durationSeconds", r.DurationSeconds);
        cmd.Parameters.AddWithValue("@pathJson", (object?)r.PathJson ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void WriteEncounter(EncounterRecord r)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO encounters VALUES (
            @encounterUid, @runId, @encounterIndex, @encounterId, @boardLayout,
            @gameVersion, @balanceHash, @schemaVersion, @source,
            @result, @rewardTier, @tidesCompleted, @finalWeave, @maxWeaveAtStart,
            @invadersKilled, @nativesKilled, @heartDamageEvents,
            @peakCorruption, @totalCorruptionAtEnd, @totalPresenceAtEnd,
            @totalFearGenerated, @sacrifices, @passivesUnlockedJson,
            @durationSeconds, @exportString)";
        cmd.Parameters.AddWithValue("@encounterUid", r.EncounterUid);
        cmd.Parameters.AddWithValue("@runId", r.RunId);
        cmd.Parameters.AddWithValue("@encounterIndex", r.EncounterIndex);
        cmd.Parameters.AddWithValue("@encounterId", r.EncounterId);
        cmd.Parameters.AddWithValue("@boardLayout", r.BoardLayout);
        cmd.Parameters.AddWithValue("@gameVersion", r.GameVersion);
        cmd.Parameters.AddWithValue("@balanceHash", r.BalanceHash);
        cmd.Parameters.AddWithValue("@schemaVersion", r.SchemaVersion);
        cmd.Parameters.AddWithValue("@source", r.Source);
        cmd.Parameters.AddWithValue("@result", r.Result);
        cmd.Parameters.AddWithValue("@rewardTier", (object?)r.RewardTier ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tidesCompleted", r.TidesCompleted);
        cmd.Parameters.AddWithValue("@finalWeave", r.FinalWeave);
        cmd.Parameters.AddWithValue("@maxWeaveAtStart", r.MaxWeaveAtStart);
        cmd.Parameters.AddWithValue("@invadersKilled", r.InvadersKilled);
        cmd.Parameters.AddWithValue("@nativesKilled", r.NativesKilled);
        cmd.Parameters.AddWithValue("@heartDamageEvents", r.HeartDamageEvents);
        cmd.Parameters.AddWithValue("@peakCorruption", r.PeakCorruption);
        cmd.Parameters.AddWithValue("@totalCorruptionAtEnd", r.TotalCorruptionAtEnd);
        cmd.Parameters.AddWithValue("@totalPresenceAtEnd", r.TotalPresenceAtEnd);
        cmd.Parameters.AddWithValue("@totalFearGenerated", r.TotalFearGenerated);
        cmd.Parameters.AddWithValue("@sacrifices", r.Sacrifices);
        cmd.Parameters.AddWithValue("@passivesUnlockedJson", (object?)r.PassivesUnlockedJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@durationSeconds", r.DurationSeconds);
        cmd.Parameters.AddWithValue("@exportString", (object?)r.ExportString ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void WriteDecision(DecisionRecord r)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO decisions (
            run_id, encounter_index, tide, phase, turn_number, timestamp_ms, type,
            game_version, balance_hash, schema_version, source,
            options_json, chosen, chosen_detail,
            weave, max_weave, hand_json, board_json,
            card_id, card_half, target_territory, elements_before, elements_after)
            VALUES (
            @runId, @encounterIndex, @tide, @phase, @turnNumber, @timestampMs, @type,
            @gameVersion, @balanceHash, @schemaVersion, @source,
            @optionsJson, @chosen, @chosenDetail,
            @weave, @maxWeave, @handJson, @boardJson,
            @cardId, @cardHalf, @targetTerritory, @elementsBefore, @elementsAfter)";
        cmd.Parameters.AddWithValue("@runId", r.RunId);
        cmd.Parameters.AddWithValue("@encounterIndex", r.EncounterIndex);
        cmd.Parameters.AddWithValue("@tide", r.Tide);
        cmd.Parameters.AddWithValue("@phase", r.Phase);
        cmd.Parameters.AddWithValue("@turnNumber", r.TurnNumber);
        cmd.Parameters.AddWithValue("@timestampMs", r.TimestampMs);
        cmd.Parameters.AddWithValue("@type", r.Type);
        cmd.Parameters.AddWithValue("@gameVersion", r.GameVersion);
        cmd.Parameters.AddWithValue("@balanceHash", r.BalanceHash);
        cmd.Parameters.AddWithValue("@schemaVersion", r.SchemaVersion);
        cmd.Parameters.AddWithValue("@source", r.Source);
        cmd.Parameters.AddWithValue("@optionsJson", (object?)r.OptionsJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@chosen", r.Chosen);
        cmd.Parameters.AddWithValue("@chosenDetail", (object?)r.ChosenDetail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@weave", r.Weave);
        cmd.Parameters.AddWithValue("@maxWeave", r.MaxWeave);
        cmd.Parameters.AddWithValue("@handJson", (object?)r.HandJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@boardJson", (object?)r.BoardJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cardId", (object?)r.CardId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cardHalf", (object?)r.CardHalf ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@targetTerritory", (object?)r.TargetTerritory ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@elementsBefore", (object?)r.ElementsBefore ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@elementsAfter", (object?)r.ElementsAfter ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void WriteTideSnapshot(TideSnapshot r)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO tide_snapshots VALUES (
            @runId, @encounterIndex, @tide,
            @gameVersion, @balanceHash, @schemaVersion, @source,
            @weave, @maxWeave, @aliveInvaders,
            @totalPresence, @totalCorruption,
            @fearGenerated, @invadersKilled, @invadersArrived,
            @cardsInHand, @cardsInDeck, @cardsDissolved)";
        cmd.Parameters.AddWithValue("@runId", r.RunId);
        cmd.Parameters.AddWithValue("@encounterIndex", r.EncounterIndex);
        cmd.Parameters.AddWithValue("@tide", r.Tide);
        cmd.Parameters.AddWithValue("@gameVersion", r.GameVersion);
        cmd.Parameters.AddWithValue("@balanceHash", r.BalanceHash);
        cmd.Parameters.AddWithValue("@schemaVersion", r.SchemaVersion);
        cmd.Parameters.AddWithValue("@source", r.Source);
        cmd.Parameters.AddWithValue("@weave", r.Weave);
        cmd.Parameters.AddWithValue("@maxWeave", r.MaxWeave);
        cmd.Parameters.AddWithValue("@aliveInvaders", r.AliveInvaders);
        cmd.Parameters.AddWithValue("@totalPresence", r.TotalPresence);
        cmd.Parameters.AddWithValue("@totalCorruption", r.TotalCorruption);
        cmd.Parameters.AddWithValue("@fearGenerated", r.FearGenerated);
        cmd.Parameters.AddWithValue("@invadersKilled", r.InvadersKilled);
        cmd.Parameters.AddWithValue("@invadersArrived", r.InvadersArrived);
        cmd.Parameters.AddWithValue("@cardsInHand", r.CardsInHand);
        cmd.Parameters.AddWithValue("@cardsInDeck", r.CardsInDeck);
        cmd.Parameters.AddWithValue("@cardsDissolved", r.CardsDissolved);
        cmd.ExecuteNonQuery();
    }

    public void WriteEvent(EventRecord r)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO events VALUES (
            @eventUid, @runId, @afterEncounterIndex,
            @eventId, @eventType,
            @gameVersion, @balanceHash, @schemaVersion, @source,
            @optionChosen, @effectsJson,
            @weaveBefore, @weaveAfter,
            @tokensBefore, @tokensAfter)";
        cmd.Parameters.AddWithValue("@eventUid", r.EventUid);
        cmd.Parameters.AddWithValue("@runId", r.RunId);
        cmd.Parameters.AddWithValue("@afterEncounterIndex", r.AfterEncounterIndex);
        cmd.Parameters.AddWithValue("@eventId", r.EventId);
        cmd.Parameters.AddWithValue("@eventType", r.EventType);
        cmd.Parameters.AddWithValue("@gameVersion", r.GameVersion);
        cmd.Parameters.AddWithValue("@balanceHash", r.BalanceHash);
        cmd.Parameters.AddWithValue("@schemaVersion", r.SchemaVersion);
        cmd.Parameters.AddWithValue("@source", r.Source);
        cmd.Parameters.AddWithValue("@optionChosen", r.OptionChosen);
        cmd.Parameters.AddWithValue("@effectsJson", (object?)r.EffectsJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@weaveBefore", r.WeaveBefore);
        cmd.Parameters.AddWithValue("@weaveAfter", r.WeaveAfter);
        cmd.Parameters.AddWithValue("@tokensBefore", r.TokensBefore);
        cmd.Parameters.AddWithValue("@tokensAfter", r.TokensAfter);
        cmd.ExecuteNonQuery();
    }

    public void Flush() { /* SQLite auto-commits per statement */ }

    public void Dispose() => _conn?.Dispose();
}
