namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Events;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class DamageInvadersEffect : IEffect
{
    private readonly EffectData _data;
    public DamageInvadersEffect(EffectData data) => _data = data;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null) return;

        // D28: Presence amplification — +1 damage per Presence in target territory
        var damage = AmplificationHelper.GetAmplifiedValue(_data.Value, state, target.TerritoryId);
        // Ember Fury: +1 per corrupted territory when warden is Ember and fury is active
        if (state.Warden?.WardenId == "ember")
            damage += EmberFuryHelper.GetFuryBonus(state);

        foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
        {
            ApplyDamage(invader, damage);
            if (!invader.IsAlive)
            {
                GameEvents.InvaderDefeated?.Invoke(invader);
                // Card-effect kills generate fear (passive/elemental kills do not)
                state.Dread?.OnFearGenerated(1);
                GameEvents.FearGenerated?.Invoke(1);
            }
        }
    }

    /// Shield X: blocks damage below X. Damage >= X breaks shield and deals full damage.
    private static void ApplyDamage(Invader invader, int damage)
    {
        if (damage < invader.ShieldValue) return;
        invader.ShieldValue = 0;
        invader.Hp = Math.Max(0, invader.Hp - damage);
    }
}
