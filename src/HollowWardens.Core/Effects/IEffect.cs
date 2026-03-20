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
}
