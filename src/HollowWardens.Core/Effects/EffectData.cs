namespace HollowWardens.Core.Effects;

public class EffectData
{
    public EffectType Type { get; set; }
    public int Value { get; set; }
    public int Range { get; set; }
}

public enum EffectType
{
    PlacePresence, MovePresence,
    GenerateFear, ReduceCorruption, Purify,
    DamageInvaders, PushInvaders, RoutInvaders, SlowInvaders,
    WeakenInvaders, ExposeInvaders, BrittleInvaders,
    RestoreWeave,
    ShieldNatives, BoostNatives, HealNatives, DamageNatives, SpawnNatives, MoveNatives,
    AwakeDormant,
    PullInvaders, CorruptionDetonate, AddCorruption,
    Conditional, Custom
}
