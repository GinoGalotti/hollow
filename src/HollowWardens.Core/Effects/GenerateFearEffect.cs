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
        int amount = state.ApplyFearMultiplier(_data.Value);
        state.Dread?.OnFearGenerated(amount);
        GameEvents.FearGenerated?.Invoke(amount);
    }
}
