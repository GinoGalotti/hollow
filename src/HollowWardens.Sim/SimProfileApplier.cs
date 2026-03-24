namespace HollowWardens.Sim;

using System.Text.Json;
using HollowWardens.Core.Data;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Models;
using HollowWardens.Core.Wardens;

public static class SimProfileApplier
{
    public static void ApplyWardenOverrides(
        WardenData wardenData, WardenOverrides overrides)
    {
        if (overrides.HandLimit.HasValue)
            wardenData.HandLimit = overrides.HandLimit.Value;

        if (overrides.StartingPresence != null)
        {
            wardenData.StartingPresence.Territory = overrides.StartingPresence.Territory;
            wardenData.StartingPresence.Count = overrides.StartingPresence.Count;
        }

        // Add cards from the full pool
        if (overrides.AddCards != null)
        {
            foreach (var cardId in overrides.AddCards)
            {
                var card = wardenData.Cards.FirstOrDefault(c => c.Id == cardId);
                if (card != null)
                    card.IsStarting = true; // promote to starting deck
            }
        }

        // Remove cards from starting deck
        if (overrides.RemoveCards != null)
        {
            foreach (var cardId in overrides.RemoveCards)
            {
                var card = wardenData.Cards.FirstOrDefault(c => c.Id == cardId);
                if (card != null)
                    card.IsStarting = false; // demote from starting deck
            }
        }

        // Upgrade card effect values
        if (overrides.UpgradeCards != null)
        {
            foreach (var (cardId, upgrade) in overrides.UpgradeCards)
            {
                var card = wardenData.Cards.FirstOrDefault(c => c.Id == cardId);
                if (card == null) continue;
                if (upgrade.Top?.Value != null)
                    card.TopEffect.Value = upgrade.Top.Value.Value;
                if (upgrade.Top?.Range != null)
                    card.TopEffect.Range = upgrade.Top.Range.Value;
                if (upgrade.Bottom?.Value != null)
                    card.BottomEffect.Value = upgrade.Bottom.Value.Value;
                if (upgrade.Bottom?.Range != null)
                    card.BottomEffect.Range = upgrade.Bottom.Range.Value;
            }
        }
    }

    public static void ApplyEncounterOverrides(
        EncounterConfig config, EncounterOverrides overrides)
    {
        if (overrides.TideCount.HasValue)
            config.TideCount = overrides.TideCount.Value;

        if (overrides.NativeSpawns != null)
            config.NativeSpawns = overrides.NativeSpawns;

        if (overrides.ExtraInvadersPerWave.HasValue && overrides.ExtraInvadersPerWave.Value > 0)
        {
            int extra = overrides.ExtraInvadersPerWave.Value;
            foreach (var wave in config.Waves)
            {
                foreach (var option in wave.Options)
                {
                    // Add extra Marchers to A1 (primary arrival point)
                    if (!option.Units.ContainsKey("A1"))
                        option.Units["A1"] = new List<HollowWardens.Core.Models.UnitType>();
                    for (int i = 0; i < extra; i++)
                        option.Units["A1"].Add(HollowWardens.Core.Models.UnitType.Marcher);
                }
            }
        }

        if (overrides.EscalationSchedule != null)
        {
            config.EscalationSchedule.Clear();
            foreach (var e in overrides.EscalationSchedule)
            {
                config.EscalationSchedule.Add(new EscalationEntry
                {
                    Tide   = e.Tide,
                    CardId = e.Card,
                    Pool   = Enum.Parse<ActionPool>(e.Pool, ignoreCase: true)
                });
            }
        }

        // Easy tier
        if (overrides.ElementDecayOverride.HasValue)
            config.ElementDecayOverride = overrides.ElementDecayOverride.Value;

        if (overrides.StartingElements != null)
            config.StartingElements = overrides.StartingElements;

        if (overrides.ThresholdDamageBonus.HasValue)
            config.ThresholdDamageBonus = overrides.ThresholdDamageBonus.Value;

        if (overrides.VigilPlayLimitOverride.HasValue)
            config.VigilPlayLimitOverride = overrides.VigilPlayLimitOverride.Value;

        if (overrides.DuskPlayLimitOverride.HasValue)
            config.DuskPlayLimitOverride = overrides.DuskPlayLimitOverride.Value;

        if (overrides.HandLimitOverride.HasValue)
            config.HandLimitOverride = overrides.HandLimitOverride.Value;

        if (overrides.NativeHpOverride.HasValue)
            config.NativeHpOverride = overrides.NativeHpOverride.Value;

        if (overrides.NativeDamageOverride.HasValue)
            config.NativeDamageOverride = overrides.NativeDamageOverride.Value;

        if (overrides.FearMultiplier.HasValue)
            config.FearMultiplier = overrides.FearMultiplier.Value;

        if (overrides.HeartDamageMultiplier.HasValue)
            config.HeartDamageMultiplier = overrides.HeartDamageMultiplier.Value;

        // Medium tier
        if (overrides.InvaderCorruptionScaling.HasValue)
            config.InvaderCorruptionScaling = overrides.InvaderCorruptionScaling.Value;

        if (overrides.InvaderArrivalShield.HasValue)
            config.InvaderArrivalShield = overrides.InvaderArrivalShield.Value;

        if (overrides.InvaderRegenOnRest.HasValue)
            config.InvaderRegenOnRest = overrides.InvaderRegenOnRest.Value;

        if (overrides.InvaderAdvanceBonus.HasValue)
            config.InvaderAdvanceBonus = overrides.InvaderAdvanceBonus.Value;

        if (overrides.SurgeTides != null)
            config.SurgeTides = overrides.SurgeTides;

        if (overrides.StartingInfrastructure != null)
            config.StartingInfrastructure = overrides.StartingInfrastructure;

        if (overrides.PresencePlacementCorruptionCost.HasValue)
            config.PresencePlacementCorruptionCost = overrides.PresencePlacementCorruptionCost.Value;

        // Hard tier
        if (overrides.CorruptionSpread.HasValue)
            config.CorruptionSpread = overrides.CorruptionSpread.Value;

        if (overrides.SacredTerritories != null)
            config.SacredTerritories = overrides.SacredTerritories;

        if (overrides.NativeErosionPerTide.HasValue)
            config.NativeErosionPerTide = overrides.NativeErosionPerTide.Value;

        if (overrides.BlightPulseInterval.HasValue)
            config.BlightPulseInterval = overrides.BlightPulseInterval.Value;

        if (overrides.EclipseTides != null)
            config.EclipseTides = overrides.EclipseTides;

        // Board layout
        if (overrides.BoardLayout != null)
            config.BoardLayout = overrides.BoardLayout;
    }

    public static void ApplyBalanceOverrides(
        BalanceConfig balance, Dictionary<string, object> overrides)
    {
        var type = typeof(BalanceConfig);
        foreach (var (key, value) in overrides)
        {
            // Special case: element_overrides is a nested dictionary
            if (key == "element_overrides" && value is JsonElement eoElement
                && eoElement.ValueKind == JsonValueKind.Object)
            {
                ApplyElementOverrides(balance, eoElement);
                continue;
            }

            var propName = SnakeToPascal(key);
            var prop = type.GetProperty(propName);
            if (prop == null) continue;

            if (value is JsonElement je)
            {
                if (prop.PropertyType == typeof(int))
                    prop.SetValue(balance, je.GetInt32());
                else if (prop.PropertyType == typeof(float))
                    prop.SetValue(balance, je.GetSingle());
            }
        }
    }

    private static void ApplyElementOverrides(BalanceConfig balance, JsonElement eoElement)
    {
        var eoCfgType = typeof(BalanceConfig.ElementThresholdConfig);
        foreach (var elementProp in eoElement.EnumerateObject())
        {
            var elementName = elementProp.Name; // e.g., "Ash"
            if (!balance.ElementOverrides.TryGetValue(elementName, out var eoCfg))
            {
                eoCfg = new BalanceConfig.ElementThresholdConfig();
                balance.ElementOverrides[elementName] = eoCfg;
            }

            foreach (var prop in elementProp.Value.EnumerateObject())
            {
                var propName = SnakeToPascal(prop.Name); // e.g., "Tier3Threshold"
                var pi = eoCfgType.GetProperty(propName);
                if (pi == null) continue;
                // All ElementThresholdConfig properties are int?
                pi.SetValue(eoCfg, (int?)prop.Value.GetInt32());
            }
        }
    }

    public static void ApplyPassiveOverrides(
        PassiveGating gating, WardenOverrides overrides)
    {
        if (overrides.ForcePassives != null)
        {
            foreach (var id in overrides.ForcePassives)
                gating.ForceUnlock(id);
        }

        if (overrides.LockPassives != null)
        {
            foreach (var id in overrides.LockPassives)
                gating.ForceLock(id);
        }

        if (overrides.UpgradePassives != null)
        {
            foreach (var upgradeId in overrides.UpgradePassives)
                gating.UpgradePassive(upgradeId);
        }
    }

    public static void ApplyStartingCorruption(
        EncounterState state, Dictionary<string, int> corruption)
    {
        foreach (var (territoryId, points) in corruption)
        {
            var territory = state.GetTerritory(territoryId);
            if (territory != null)
                state.Corruption?.AddCorruption(territory, points);
        }
    }

    public static void ApplyStartingElements(
        EncounterState state, Dictionary<string, int> elements)
    {
        foreach (var (elementName, value) in elements)
        {
            if (Enum.TryParse<Element>(elementName, ignoreCase: true, out var element))
                state.Elements?.AddElements(new[] { element }, value);
        }
    }

    public static void ApplyBoardCarryoverOverride(
        EncounterState state, BoardCarryoverOverride overrides)
    {
        if (overrides.StartingWeave.HasValue)
            state.Weave = new HollowWardens.Core.Systems.WeaveSystem(
                overrides.StartingWeave.Value, state.Balance.MaxWeave);

        if (overrides.StartingCorruption != null)
            ApplyStartingCorruption(state, overrides.StartingCorruption);

        if (overrides.RemovedCards != null)
        {
            foreach (var cardId in overrides.RemovedCards)
                state.Deck?.PermanentlyRemove(cardId);
        }
    }

    private static string SnakeToPascal(string snake)
    {
        return string.Concat(snake.Split('_')
            .Select(s => s.Length > 0 ? char.ToUpper(s[0]) + s[1..] : s));
    }
}
