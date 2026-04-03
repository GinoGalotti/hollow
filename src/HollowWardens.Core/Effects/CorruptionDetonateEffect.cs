namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Events;
using HollowWardens.Core.Models;

/// <summary>
/// Deals damage to all invaders equal to the territory's corruption points × value multiplier,
/// then fully cleanses the territory.
/// </summary>
public class CorruptionDetonateEffect : IEffect
{
    private readonly EffectData _data;
    public CorruptionDetonateEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        int damage = territory.CorruptionPoints * (_data.Value > 0 ? _data.Value : 1);

        foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
        {
            invader.Hp = Math.Max(0, invader.Hp - damage);
            if (!invader.IsAlive)
            {
                GameEvents.InvaderDefeated?.Invoke(invader);
                state.Dread?.OnFearGenerated(1);
                GameEvents.FearGenerated?.Invoke(1);
            }
        }

        // Cleanse all corruption
        if (state.Corruption != null)
        {
            state.Corruption.ReduceCorruption(territory, territory.CorruptionPoints);
        }
        else
        {
            territory.CorruptionPoints = 0;
        }
    }
}
