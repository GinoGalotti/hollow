namespace HollowWardens.Core.Effects;

public class EffectResolver
{
    public IEffect Resolve(EffectData data) => data.Type switch
    {
        EffectType.PlacePresence    => new PlacePresenceEffect(data),
        EffectType.ReduceCorruption => new ReduceCorruptionEffect(data),
        EffectType.Purify           => new PurifyEffect(data),
        EffectType.DamageInvaders   => new DamageInvadersEffect(data),
        EffectType.GenerateFear     => new GenerateFearEffect(data),
        EffectType.RestoreWeave     => new RestoreWeaveEffect(data),
        EffectType.PushInvaders     => new PushInvadersEffect(data),
        EffectType.ShieldNatives    => new ShieldNativesEffect(data),
        EffectType.BoostNatives     => new BoostNativesEffect(data),
        EffectType.AwakeDormant     => new AwakeDormantEffect(data),
        _ => throw new NotImplementedException($"Effect type {data.Type} is not implemented.")
    };
}
