namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;

/// <summary>
/// Computes the Ember Fury damage bonus: +1 per territory at corruption Level 1+ when fury is active.
/// </summary>
public static class EmberFuryHelper
{
    public static int GetFuryBonus(EncounterState state)
    {
        if (state.PassiveGating != null && !state.PassiveGating.IsActive("ember_fury"))
            return 0;
        return state.Territories.Count(t => t.CorruptionLevel >= 2);
    }
}
