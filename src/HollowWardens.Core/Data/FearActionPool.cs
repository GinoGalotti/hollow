namespace HollowWardens.Core.Data;

using HollowWardens.Core.Effects;
using HollowWardens.Core.Models;

public static class FearActionPool
{
    /// <summary>Returns fear action pools for Dread Levels 1 and 2.</summary>
    public static Dictionary<int, List<FearActionData>> Build() => new()
    {
        [1] = Level1(),
        [2] = Level2()
    };

    private static List<FearActionData> Level1() => new()
    {
        new FearActionData
        {
            Id          = "fa1_strike",
            Description = "Deal 1 damage to 1 invader",
            DreadLevel  = 1,
            Effect      = new EffectData { Type = EffectType.DamageInvaders, Value = 1, Range = 1 }
        },
        new FearActionData
        {
            Id          = "fa1_push",
            Description = "Push 1 invader back toward spawn",
            DreadLevel  = 1,
            Effect      = new EffectData { Type = EffectType.PushInvaders, Value = 1, Range = 1 }
        },
        new FearActionData
        {
            Id          = "fa1_cleanse",
            Description = "Reduce 1 Corruption point from 1 territory",
            DreadLevel  = 1,
            Effect      = new EffectData { Type = EffectType.ReduceCorruption, Value = 1, Range = 0 }
        },
        new FearActionData
        {
            Id          = "fa1_fear",
            Description = "Generate 2 Fear",
            DreadLevel  = 1,
            Effect      = new EffectData { Type = EffectType.GenerateFear, Value = 2, Range = 0 }
        }
    };

    private static List<FearActionData> Level2() => new()
    {
        new FearActionData
        {
            Id          = "fa2_assault",
            Description = "Deal 2 damage to all invaders in 1 territory",
            DreadLevel  = 2,
            Effect      = new EffectData { Type = EffectType.DamageInvaders, Value = 2, Range = 1 }
        },
        new FearActionData
        {
            Id          = "fa2_push_cleanse",
            Description = "Push 1 invader and reduce 1 Corruption",
            DreadLevel  = 2,
            Effect      = new EffectData { Type = EffectType.PushInvaders, Value = 1, Range = 1 }
        },
        new FearActionData
        {
            Id          = "fa2_weave",
            Description = "Restore 1 Weave",
            DreadLevel  = 2,
            Effect      = new EffectData { Type = EffectType.RestoreWeave, Value = 1, Range = 0 }
        },
        new FearActionData
        {
            Id          = "fa2_terror",
            Description = "Generate 3 Fear",
            DreadLevel  = 2,
            Effect      = new EffectData { Type = EffectType.GenerateFear, Value = 3, Range = 0 }
        }
    };
}
