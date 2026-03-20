namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;

public class AwakeDormantEffect : IEffect
{
    public AwakeDormantEffect(EffectData _) { }

    public void Resolve(EncounterState state, TargetInfo target)
        => throw new NotImplementedException(
            "AwakeDormant effect requires IDeckManager.AwakenDormant, which is not yet exposed on the interface.");
}
