// Export format (produced by ExportFull / imported by ImportFull):
//   "SEED:{seed}|{action1}|{action2}|..."
// Each action token is:
//   "{Timestamp}:{Type}:{CardId}:{TargetTerritoryId}"
// where CardId and TargetTerritoryId are "-" when absent.
// Example: "SEED:42|0:PlayTop:root-grasping-roots:-|1:SelectTarget:-:M1|2:PlayBottom:root-mist-walk:-"

namespace HollowWardens.Core;

using HollowWardens.Core.Models;

public enum GameActionType
{
    PlayTop, PlayBottom, SkipPhase, Rest,
    AssignCounterAttack, SelectTarget, BankThreshold, ResolveThreshold
}

public class GameAction
{
    public int TurnNumber { get; set; }
    public TurnPhase Phase { get; set; }
    public GameActionType Type { get; set; }
    public string? CardId { get; set; }
    public string? TargetTerritoryId { get; set; }
    public Dictionary<string, int>? DamageAssignment { get; set; }
    public int Timestamp { get; set; }

    public override string ToString() =>
        $"{Timestamp}:{Type}:{CardId ?? "-"}:{TargetTerritoryId ?? "-"}";
}

public class ActionLog
{
    private readonly List<GameAction> _actions = new();

    public IReadOnlyList<GameAction> Actions => _actions;
    public int Count => _actions.Count;

    public void Record(GameAction action)
    {
        action.Timestamp = _actions.Count;
        _actions.Add(action);
    }

    /// <summary>Removes all actions after <paramref name="index"/> (inclusive of index is kept).</summary>
    public void TruncateTo(int index)
    {
        if (index + 1 < _actions.Count)
            _actions.RemoveRange(index + 1, _actions.Count - index - 1);
    }

    /// <summary>Returns "SEED:{seed}|{action1}|{action2}|..." for replay.</summary>
    public string Export(int seed) =>
        $"SEED:{seed}|" + string.Join("|", _actions.Select(a => a.ToString()));

    /// <summary>Extracts the seed from a string produced by <see cref="Export"/>.</summary>
    public static int ParseSeed(string exported)
    {
        int pipeIdx = exported.IndexOf('|');
        var seedStr = pipeIdx >= 0 ? exported[5..pipeIdx] : exported[5..];
        return int.Parse(seedStr);
    }

    /// <summary>Alias for <see cref="Export"/>; produces "SEED:{seed}|action|..." for full-state export.</summary>
    public string ExportFull(int seed) => Export(seed);

    /// <summary>
    /// Parses a string produced by <see cref="Export"/> or <see cref="ExportFull"/>.
    /// Returns the seed and the raw action tokens (each "timestamp:type:cardId:targetId").
    /// </summary>
    public static (int seed, string[] rawActions) ImportFull(string data)
    {
        int pipeIdx = data.IndexOf('|');
        if (pipeIdx < 0) return (ParseSeed(data), Array.Empty<string>());
        int seed = ParseSeed(data);
        var raw  = data[(pipeIdx + 1)..].Split('|', StringSplitOptions.RemoveEmptyEntries);
        return (seed, raw);
    }
}
