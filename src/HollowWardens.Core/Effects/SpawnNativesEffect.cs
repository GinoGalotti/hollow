namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

/// <summary>
/// Spawns {value} natives in the target territory. Target must have Root presence.
/// Native HP and damage come from BalanceConfig defaults.
/// </summary>
public class SpawnNativesEffect : IEffect
{
    private readonly int _count;

    public SpawnNativesEffect(EffectData data) => _count = data.Value;

    public void Resolve(EncounterState state, TargetInfo target)
    {
        var territory = state.GetTerritory(target.TerritoryId);
        if (territory == null || !territory.HasPresence) return;

        int nativeHp     = state.Balance.DefaultNativeHp;
        int nativeDamage = state.Balance.DefaultNativeDamage;

        for (int i = 0; i < _count; i++)
        {
            territory.Natives.Add(new Native
            {
                Hp          = nativeHp,
                MaxHp       = nativeHp,
                Damage      = nativeDamage,
                TerritoryId = territory.Id,
            });
        }
    }
}
