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
        int baseAmount = _data.Value;

        // Terrain modifier: Mountain +2, Blighted -1 (applied before fear multiplier)
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory != null)
        {
            baseAmount += TerrainEffects.GetFearModifier(territory.Terrain);
            baseAmount += TerrainEffects.GetEffectValueModifier(territory.Terrain);
        }

        int amount = state.ApplyFearMultiplier(Math.Max(0, baseAmount));
        state.Dread?.OnFearGenerated(amount);
        GameEvents.FearGenerated?.Invoke(amount);
    }
}
