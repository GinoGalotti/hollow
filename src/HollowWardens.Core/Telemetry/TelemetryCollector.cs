using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Run;

namespace HollowWardens.Core.Telemetry;

public class TelemetryCollector : IDisposable
{
    private readonly ITelemetrySink _sink;
    private readonly string _balanceHash;

    // Current tracking state
    private string _runId = "";
    private string _playerId = "local";
    private string _source = "player";
    private int _encounterIndex = 0;
    private DateTime _runStart;
    private DateTime _encounterStart;
    private int _currentTide;
    private int _currentTurn;
    private string _currentPhase = "";

    // Run-level state (set in StartRun)
    private string _warden = "";
    private string _mode = "single";
    private string? _realm;
    private int _seed;

    // Per-encounter stat accumulators
    private int _invadersKilled;
    private int _nativesKilled;
    private int _heartDamageEvents;
    private int _peakCorruption;
    private int _sacrifices;
    private int _fearGenerated;

    // Per-tide stat accumulators (reset each tide)
    private int _tideInvadersKilled;
    private int _tideFearGenerated;

    public TelemetryCollector(ITelemetrySink sink, string balanceHash,
                              string source = "player", string playerId = "local")
    {
        _sink = sink;
        _balanceHash = balanceHash;
        _source = source;
        _playerId = playerId;
    }

    // ── Run Lifecycle ─────────────────────────────────

    public void StartRun(string warden, string mode, string? realm, int seed)
    {
        _runId = Guid.NewGuid().ToString();
        _runStart = DateTime.UtcNow;
        _encounterIndex = 0;
        _warden = warden;
        _mode = mode;
        _realm = realm;
        _seed = seed;
    }

    public void EndRun(RunState? runState, string result)
    {
        var record = new RunRecord
        {
            RunId = _runId,
            PlayerId = _playerId,
            Source = _source,
            BalanceHash = _balanceHash,
            Warden = runState?.WardenId ?? _warden,
            Mode = runState != null ? "full_run" : _mode,
            Realm = runState?.RealmId ?? _realm,
            Seed = runState?.Seed ?? _seed,
            Result = result,
            EncountersCompleted = _encounterIndex,
            FinalMaxWeave = runState?.MaxWeave ?? 20,
            FinalWeave = runState?.CurrentWeave ?? 20,
            CardsDrafted = runState?.DeckCardIds.Count ?? 0,
            CardsUpgraded = runState?.AppliedCardUpgradeIds.Count ?? 0,
            CardsRemoved = runState?.PermanentlyRemovedCardIds.Count ?? 0,
            PassivesUpgraded = runState?.AppliedPassiveUpgradeIds.Count ?? 0,
            PassivesUnlocked = runState?.PermanentlyUnlockedPassives.Count ?? 0,
            TokensEarned = runState?.UpgradeTokens ?? 0,
            DurationSeconds = (DateTime.UtcNow - _runStart).TotalSeconds,
        };
        _sink.WriteRun(record);
        _sink.Flush();
    }

    // ── Encounter Lifecycle ───────────────────────────

    public void StartEncounter(string encounterId, string boardLayout, int maxWeave)
    {
        _encounterStart = DateTime.UtcNow;
        _invadersKilled = 0;
        _nativesKilled = 0;
        _heartDamageEvents = 0;
        _peakCorruption = 0;
        _sacrifices = 0;
        _fearGenerated = 0;
        _currentTide = 0;
        _currentTurn = 0;
        _currentPhase = "";
    }

    public void EndEncounter(EncounterState state, string result, string? rewardTier)
    {
        int totalCorruption = state.Territories.Sum(t => t.CorruptionPoints);
        int totalPresence = state.Territories.Sum(t => t.PresenceCount);

        var record = new EncounterRecord
        {
            RunId = _runId,
            EncounterIndex = _encounterIndex,
            EncounterId = state.Config?.Id ?? "",
            BoardLayout = state.Config?.BoardLayout ?? "standard",
            BalanceHash = _balanceHash,
            Source = _source,
            Result = result,
            RewardTier = rewardTier,
            TidesCompleted = _currentTide,
            FinalWeave = state.Weave?.CurrentWeave ?? 0,
            MaxWeaveAtStart = state.Balance?.MaxWeave ?? 20,
            InvadersKilled = _invadersKilled,
            NativesKilled = _nativesKilled,
            HeartDamageEvents = _heartDamageEvents,
            PeakCorruption = _peakCorruption,
            TotalCorruptionAtEnd = totalCorruption,
            TotalPresenceAtEnd = totalPresence,
            TotalFearGenerated = _fearGenerated,
            Sacrifices = _sacrifices,
            DurationSeconds = (DateTime.UtcNow - _encounterStart).TotalSeconds,
        };
        _sink.WriteEncounter(record);
        _encounterIndex++;
    }

    // ── Decision Recording ────────────────────────────

    public void RecordDecision(string type, string chosen, string? chosenDetail,
                               string? optionsJson, EncounterState state,
                               string? cardId = null, string? cardHalf = null,
                               string? targetTerritory = null)
    {
        var record = new DecisionRecord
        {
            RunId = _runId,
            EncounterIndex = _encounterIndex,
            Tide = _currentTide,
            Phase = _currentPhase,
            TurnNumber = _currentTurn,
            TimestampMs = (long)(DateTime.UtcNow - _encounterStart).TotalMilliseconds,
            Type = type,
            BalanceHash = _balanceHash,
            Source = _source,
            OptionsJson = optionsJson,
            Chosen = chosen,
            ChosenDetail = chosenDetail,
            Weave = state.Weave?.CurrentWeave ?? 0,
            MaxWeave = state.Balance?.MaxWeave ?? 20,
            HandJson = SerializeHand(state),
            BoardJson = SerializeBoard(state),
            CardId = cardId,
            CardHalf = cardHalf,
            TargetTerritory = targetTerritory,
            ElementsBefore = SerializeElements(state),
        };
        _sink.WriteDecision(record);
    }

    // ── Tide Tracking ─────────────────────────────────

    public void StartTide(int tide)
    {
        _currentTide = tide;
        _tideInvadersKilled = 0;
        _tideFearGenerated = 0;
    }

    public void RecordTideSnapshot(EncounterState state, int arrived, int killed)
    {
        var record = new TideSnapshot
        {
            RunId = _runId,
            EncounterIndex = _encounterIndex,
            Tide = _currentTide,
            BalanceHash = _balanceHash,
            Source = _source,
            Weave = state.Weave?.CurrentWeave ?? 0,
            MaxWeave = state.Balance?.MaxWeave ?? 20,
            AliveInvaders = state.Territories.Sum(t => t.Invaders.Count(i => i.IsAlive)),
            TotalPresence = state.Territories.Sum(t => t.PresenceCount),
            TotalCorruption = state.Territories.Sum(t => t.CorruptionPoints),
            FearGenerated = _fearGenerated,
            InvadersKilled = killed,
            InvadersArrived = arrived,
            CardsInHand = state.Deck?.Hand.Count ?? 0,
            CardsInDeck = state.Deck?.DrawPileCount ?? 0,
            CardsDissolved = state.Deck?.DissolvedCount ?? 0,
        };
        _sink.WriteTideSnapshot(record);
    }

    // ── Event Recording ───────────────────────────────

    public void RecordEvent(string eventId, string eventType, int optionChosen,
                            string? effectsJson, int weaveBefore, int weaveAfter,
                            int tokensBefore, int tokensAfter)
    {
        var record = new EventRecord
        {
            RunId = _runId,
            AfterEncounterIndex = _encounterIndex - 1,
            EventId = eventId,
            EventType = eventType,
            BalanceHash = _balanceHash,
            Source = _source,
            OptionChosen = optionChosen,
            EffectsJson = effectsJson,
            WeaveBefore = weaveBefore,
            WeaveAfter = weaveAfter,
            TokensBefore = tokensBefore,
            TokensAfter = tokensAfter,
        };
        _sink.WriteEvent(record);
    }

    // ── Stat Setters (called by game loop) ────────────

    public void SetTide(int tide) => _currentTide = tide;
    public void SetPhase(string phase) => _currentPhase = phase;
    public void SetTurn(int turn) => _currentTurn = turn;
    public void OnInvaderKilled() { _invadersKilled++; _tideInvadersKilled++; }
    public void OnNativeKilled() => _nativesKilled++;
    public void OnHeartDamage() => _heartDamageEvents++;
    public void OnSacrifice() => _sacrifices++;

    public void OnFearGenerated(int amount)
    {
        _fearGenerated += amount;
        _tideFearGenerated += amount;
    }

    public void UpdatePeakCorruption(int corruption)
    {
        if (corruption > _peakCorruption) _peakCorruption = corruption;
    }

    // ── Serialization Helpers ─────────────────────────

    private static string SerializeHand(EncounterState state)
    {
        var ids = state.Deck?.Hand.Select(c => c.Id) ?? Enumerable.Empty<string>();
        return System.Text.Json.JsonSerializer.Serialize(ids);
    }

    private static string SerializeBoard(EncounterState state)
    {
        var board = state.Territories.Select(t => new
        {
            id = t.Id,
            corruption = t.CorruptionPoints,
            presence = t.PresenceCount,
            invaders = t.Invaders.Count(i => i.IsAlive)
        });
        return System.Text.Json.JsonSerializer.Serialize(board);
    }

    private static string SerializeElements(EncounterState state)
    {
        var dict = new Dictionary<string, int>();
        foreach (var e in Enum.GetValues<Element>())
        {
            var count = state.Elements?.Get(e) ?? 0;
            if (count > 0) dict[e.ToString()] = count;
        }
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }

    public void Dispose() => _sink?.Dispose();
}
