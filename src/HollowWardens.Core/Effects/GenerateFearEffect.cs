namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Events;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class GenerateFearEffect : IEffect
{
    private readonly EffectData _data;
    public GenerateFearEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        GameEvents.FearGenerated?.Invoke(_data.Value);
        state.Dread?.OnFearGenerated(_data.Value);
    }
}
