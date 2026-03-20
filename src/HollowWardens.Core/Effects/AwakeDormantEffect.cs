namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class AwakeDormantEffect : IEffect
{
    public AwakeDormantEffect(EffectData _) { }

    public void Resolve(EncounterState state, TargetInfo target)
        => state.Deck?.AwakenAllDormant();
}
