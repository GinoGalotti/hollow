namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public interface IEffect
{
    void Resolve(EncounterState state, TargetInfo target);
}

public class TargetInfo
{
    public string TerritoryId { get; set; } = string.Empty;
    public string? InvaderId { get; set; }
    public Card? SourceCard { get; set; }

    /// <summary>
    /// Optional per-invader push destinations. Each entry maps to one invader (in order).
    /// When null or empty, effects fall back to automatic selection.
    /// </summary>
    public List<string>? PushDestinations { get; set; }
}
