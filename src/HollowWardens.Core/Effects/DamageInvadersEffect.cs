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

        foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
        {
            ApplyDamage(invader, _data.Value);
            if (!invader.IsAlive)
                GameEvents.InvaderDefeated?.Invoke(invader);
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
