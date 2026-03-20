namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class RestoreWeaveEffect : IEffect
{
    private readonly EffectData _data;
    public RestoreWeaveEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
        => state.Weave?.Restore(_data.Value);
}
