namespace HollowWardens.Core.Effects;

using HollowWardens.Core.Events;
using HollowWardens.Core.Encounter;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;

/// <summary>
/// Manages element threshold effects. Supports:
/// • Immediate auto-resolution (AutoResolve — used by tests and legacy code).
/// • Player-driven pending queue (OnThresholdTriggered → Resolve / ClearUnresolved).
/// </summary>
public class ThresholdResolver
{
    private readonly List<(Element element, int tier)> _pending = new();

    /// <summary>Current unresolved pending entries (read-only view).</summary>
    public IReadOnlyList<(Element element, int tier)> Pending => _pending;

    private static readonly Dictionary<(Element, int), string> Descriptions = new()
    {
        [(Element.Root,   1)] = "Root T1: Reduce Corruption ×3 in one territory with Presence",
        [(Element.Root,   2)] = "Root T2: Place 1 Presence at range 1",
        [(Element.Root,   3)] = "Root T3: Place 2 Presence anywhere + Reduce Corruption ×3 in each",
        [(Element.Mist,   1)] = "Mist T1: Restore 2 Weave",
        [(Element.Mist,   2)] = "Mist T2: Return 2 random cards from discard to draw pile",
        [(Element.Mist,   3)] = "Mist T3: Restore 3 Weave + return 3 random cards to draw pile",
        [(Element.Shadow, 1)] = "Shadow T1: +2 Fear",
        [(Element.Shadow, 2)] = "Shadow T2: Next Fear Action draws from Dread+1",
        [(Element.Shadow, 3)] = "Shadow T3: +5 Fear",
        [(Element.Ash,    1)] = "Ash T1: 1 damage to all invaders in one territory",
        [(Element.Ash,    2)] = "Ash T2: 2 damage to all in one territory + 1 Corruption",
        [(Element.Ash,    3)] = "Ash T3: 2 damage per Presence token in target territory",
        [(Element.Gale,   1)] = "Gale T1: Push 1 invader to any adjacent territory",
        [(Element.Gale,   2)] = "Gale T2: Push all invaders in one territory to any adjacent territory",
        [(Element.Gale,   3)] = "Gale T3: Push all invaders on board toward spawn + skip next Advance",
        [(Element.Void,   1)] = "Void T1: Deal 3 damage to lowest-HP invader; kills generate Fear",
        [(Element.Void,   2)] = "Void T2: All invaders and Natives take 1 damage (not Infrastructure)",
        [(Element.Void,   3)] = "Void T3: All invaders take 1 damage; kills generate Fear",
    };

    public static string GetDescription(Element element, int tier)
        => Descriptions.TryGetValue((element, tier), out var d) ? d : $"{element} T{tier}";

    // ── Target requirements ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when this (element, tier) combination requires the player
    /// to select a territory before the effect can be executed.
    /// T3 effects with board-wide behavior don't require player targeting.
    /// </summary>
    public static bool NeedsTarget(Element element, int tier)
        => (element, tier) switch
        {
            (Element.Root,   1) or (Element.Root,   2) => true,
            (Element.Ash,    1) or (Element.Ash,    2) or (Element.Ash, 3) => true,
            (Element.Gale,   1) or (Element.Gale,   2) => true,
            _                                           => false
        };

    /// <summary>
    /// Returns an EffectData describing the targeting requirement for effects
    /// that need a player-selected territory, or null for auto-resolving effects.
    /// </summary>
    public static EffectData? GetTargetEffect(Element element, int tier)
        => (element, tier) switch
        {
            (Element.Root,   1) => new EffectData { Type = EffectType.ReduceCorruption, Value = 3 },
            (Element.Root,   2) => new EffectData { Type = EffectType.PlacePresence,    Range = 1, Value = 1 },
            (Element.Ash,    1) => new EffectData { Type = EffectType.DamageInvaders,   Value = 1 },
            (Element.Ash,    2) => new EffectData { Type = EffectType.DamageInvaders,   Value = 2 },
            (Element.Ash,    3) => new EffectData { Type = EffectType.DamageInvaders,   Value = 0 }, // presence-scaled
            (Element.Gale,   1) => new EffectData { Type = EffectType.PushInvaders,     Value = 1 },
            (Element.Gale,   2) => new EffectData { Type = EffectType.PushInvaders,     Value = 1 },
            _                   => null
        };

    // ── Player-driven queue API ───────────────────────────────────────────────

    /// <summary>
    /// Called when a threshold fires. Adds to the pending queue and fires
    /// ThresholdPending so the UI can enable the player's resolve button.
    /// </summary>
    public void OnThresholdTriggered(Element element, int tier, EncounterState state)
    {
        _pending.Add((element, tier));
        GameEvents.ThresholdPending?.Invoke(element, tier, GetDescription(element, tier));
    }

    /// <summary>
    /// Resolves a pending threshold effect (player-initiated button press).
    /// Removes the first matching pending entry, executes the effect, fires ThresholdResolved.
    /// Pass <paramref name="targetTerritoryId"/> for effects that require a player-selected territory.
    /// </summary>
    public void Resolve(Element element, int tier, EncounterState state, string? targetTerritoryId = null)
    {
        for (int i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].element == element && _pending[i].tier == tier)
            {
                _pending.RemoveAt(i);
                break;
            }
        }

        ExecuteEffect(element, tier, state, targetTerritoryId);
        state.Elements?.ResolveBanked(element, tier);
        GameEvents.ThresholdResolved?.Invoke(element, tier, GetDescription(element, tier));
    }

    /// <summary>
    /// Expires all unresolved pending entries (called at end of Dusk).
    /// Fires ThresholdExpired for each so the UI can clear pending buttons.
    /// </summary>
    public void ClearUnresolved()
    {
        foreach (var (element, tier) in _pending)
            GameEvents.ThresholdExpired?.Invoke(element, tier);
        _pending.Clear();
    }

    /// <summary>
    /// Resolves all pending thresholds with auto-targeting (no territory selection).
    /// Used by bot strategies and the default IPlayerStrategy implementation.
    /// </summary>
    public void AutoResolveAll(EncounterState state)
    {
        foreach (var (element, tier) in _pending.ToList())
            Resolve(element, tier, state, null);
    }

    // ── Immediate auto-resolution (backward-compat, used by tests) ────────────

    /// <summary>
    /// Immediately resolves without going through the pending queue.
    /// Used by tests and legacy code.
    /// </summary>
    public void AutoResolve(Element element, int tier, EncounterState state)
    {
        ExecuteEffect(element, tier, state);
        state.Elements?.ResolveBanked(element, tier);
        GameEvents.ThresholdResolved?.Invoke(element, tier, GetDescription(element, tier));
    }

    // ── Effect dispatch ────────────────────────────────────────────────────────

    private static void ExecuteEffect(Element element, int tier, EncounterState state,
        string? targetTerritoryId = null)
    {
        switch (element)
        {
            case Element.Root:   ResolveRoot(tier, state, targetTerritoryId);   break;
            case Element.Mist:   ResolveMist(tier, state);                      break;
            case Element.Shadow: ResolveShadow(tier, state);                    break;
            case Element.Ash:    ResolveAsh(tier, state, targetTerritoryId);    break;
            case Element.Gale:   ResolveGale(tier, state, targetTerritoryId);   break;
            case Element.Void:   ResolveVoid(tier, state);                      break;
        }
    }

    // ── Root ─────────────────────────────────────────────────────────────────

    // T1: Reduce Corruption by 3 in one territory with Presence (auto: highest corruption w/ presence)
    // T2: Place 1 Presence at range 1 from any existing Presence token
    // T3: Place 2 Presence anywhere + Reduce Corruption by 3 in each placed territory
    private static void ResolveRoot(int tier, EncounterState state, string? targetTerritoryId = null)
    {
        switch (tier)
        {
            case 1:
            {
                var target = targetTerritoryId != null
                    ? state.GetTerritory(targetTerritoryId)
                    : state.Territories
                        .Where(t => t.HasPresence && t.CorruptionPoints > 0)
                        .OrderByDescending(t => t.CorruptionPoints)
                        .FirstOrDefault();
                if (target != null)
                    state.Corruption?.ReduceCorruption(target, 3);
                break;
            }

            case 2:
                if (targetTerritoryId != null)
                {
                    var territory = state.GetTerritory(targetTerritoryId);
                    if (territory != null) state.Presence?.PlacePresence(territory);
                }
                else
                    PlacePresenceAdjacent(state);
                break;

            case 3:
                if (targetTerritoryId != null)
                {
                    var target = state.GetTerritory(targetTerritoryId);
                    if (target != null)
                    {
                        state.Presence?.PlacePresence(target);
                        state.Presence?.PlacePresence(target);
                        state.Corruption?.ReduceCorruption(target, 3);
                    }
                }
                else
                {
                    var placed1 = PlacePresenceAdjacentTracked(state);
                    if (placed1 != null) state.Corruption?.ReduceCorruption(placed1, 3);
                    var placed2 = PlacePresenceAdjacentTracked(state);
                    if (placed2 != null) state.Corruption?.ReduceCorruption(placed2, 3);
                }
                break;
        }
    }

    private static void PlacePresenceAdjacent(EncounterState state)
        => PlacePresenceAdjacentTracked(state);

    private static Territory? PlacePresenceAdjacentTracked(EncounterState state)
    {
        var presenceTerritories = state.Territories.Where(t => t.HasPresence).ToList();
        foreach (var source in presenceTerritories)
        {
            var neighbor = state.Graph.GetNeighbors(source.Id)
                .Select(id => state.GetTerritory(id))
                .FirstOrDefault(t => t != null && !t.HasPresence);
            if (neighbor != null)
            {
                state.Presence?.PlacePresence(neighbor);
                return neighbor;
            }
        }
        // Fallback: stack on an existing presence territory
        var fallback = presenceTerritories.FirstOrDefault();
        if (fallback != null)
        {
            state.Presence?.PlacePresence(fallback);
            return fallback;
        }
        return null;
    }

    // ── Mist ─────────────────────────────────────────────────────────────────

    // T1: Restore 2 Weave
    // T2: Return 2 random cards from discard to draw pile
    // T3: Restore 3 Weave + return 3 random cards from discard to draw pile
    //     (cumulative with T1+T2 = 5 Weave + 5 cards when all tiers active)
    private static void ResolveMist(int tier, EncounterState state)
    {
        switch (tier)
        {
            case 1:
                state.Weave?.Restore(2);
                break;

            case 2:
                state.Deck?.ReturnDiscardToDraw(2);
                break;

            case 3:
                state.Weave?.Restore(3);
                state.Deck?.ReturnDiscardToDraw(3);
                break;
        }
    }

    // ── Shadow ────────────────────────────────────────────────────────────────

    // T1: Generate 2 Fear
    // T2: Next Fear Action draws from Dread Level + 1
    // T3: Generate 5 Fear (preview/choose feature deferred)
    private static void ResolveShadow(int tier, EncounterState state)
    {
        switch (tier)
        {
            case 1:
                state.Dread?.OnFearGenerated(2);
                GameEvents.FearGenerated?.Invoke(2);
                break;

            case 2:
                state.FearActions?.ElevateNextDraw();
                break;

            case 3:
                state.Dread?.OnFearGenerated(5);
                GameEvents.FearGenerated?.Invoke(5);
                break;
        }
    }

    // ── Ash ───────────────────────────────────────────────────────────────────

    // T1: 1 damage to all invaders in territory with most invaders
    // T2: 2 damage to all in one territory (auto: most invaders) + 1 Corruption to that territory
    // T3: 2 damage per Presence token in target territory to all invaders there (presence-scaled)
    private static void ResolveAsh(int tier, EncounterState state, string? targetTerritoryId = null)
    {
        switch (tier)
        {
            case 1:
            {
                var target = targetTerritoryId != null
                    ? state.GetTerritory(targetTerritoryId)
                    : state.TerritoriesWithInvaders()
                        .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                        .FirstOrDefault();
                if (target == null) return;
                int dmg1 = state.Balance.GetThresholdDamage(Element.Ash, 1);
                foreach (var invader in target.Invaders.Where(i => i.IsAlive).ToList())
                    ApplyDamage(invader, dmg1);
                break;
            }

            case 2:
            {
                var target = targetTerritoryId != null
                    ? state.GetTerritory(targetTerritoryId)
                    : state.TerritoriesWithInvaders()
                        .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                        .FirstOrDefault();
                if (target == null) return;
                int dmg2 = state.Balance.GetThresholdDamage(Element.Ash, 2);
                foreach (var invader in target.Invaders.Where(i => i.IsAlive).ToList())
                    ApplyDamage(invader, dmg2);
                int corr2 = state.Balance.GetThresholdCorruption(Element.Ash, 2);
                if (corr2 > 0) state.Corruption?.AddCorruption(target, corr2);
                break;
            }

            case 3:
            {
                // 2 damage per Presence token in the chosen territory
                var target = targetTerritoryId != null
                    ? state.GetTerritory(targetTerritoryId)
                    : state.Territories
                        .Where(t => t.HasPresence && t.Invaders.Any(i => i.IsAlive))
                        .OrderByDescending(t => t.PresenceCount)
                        .ThenByDescending(t => t.Invaders.Count(i => i.IsAlive))
                        .FirstOrDefault();
                if (target == null) return;
                int presenceDamage = 2 * target.PresenceCount;
                if (presenceDamage <= 0) return;
                foreach (var invader in target.Invaders.Where(i => i.IsAlive).ToList())
                    ApplyDamage(invader, presenceDamage);
                break;
            }
        }
    }

    // ── Gale ─────────────────────────────────────────────────────────────────

    // T1: Push 1 invader to any adjacent territory (auto: toward most-populated neighbor for stacking)
    // T2: Push ALL invaders in one territory to any adjacent territory (auto: most-populated neighbor)
    // T3: Push ALL invaders on board one territory toward spawn (unchanged — tempo shift)
    private static void ResolveGale(int tier, EncounterState state, string? targetTerritoryId = null)
    {
        switch (tier)
        {
            case 1:
            {
                var territory = targetTerritoryId != null
                    ? state.GetTerritory(targetTerritoryId)
                    : state.TerritoriesWithInvaders()
                        .OrderBy(t => state.Graph.Distance(t.Id, state.Graph.HeartId))
                        .FirstOrDefault();
                if (territory == null) return;
                var invader = territory.Invaders.FirstOrDefault(i => i.IsAlive);
                if (invader != null)
                    PushInvaderToStack(invader, territory, state);
                break;
            }

            case 2:
            {
                var territory = targetTerritoryId != null
                    ? state.GetTerritory(targetTerritoryId)
                    : state.TerritoriesWithInvaders()
                        .OrderBy(t => state.Graph.Distance(t.Id, state.Graph.HeartId))
                        .FirstOrDefault();
                if (territory == null) return;
                PushAllToStack(territory, state);
                break;
            }

            case 3:
            {
                // Process farthest-from-heart first so moved invaders aren't pushed twice
                var territories = state.TerritoriesWithInvaders()
                    .OrderByDescending(t => state.Graph.Distance(t.Id, state.Graph.HeartId))
                    .ToList();
                foreach (var territory in territories)
                    PushAllInTerritory(territory, state);
                break;
            }
        }
    }

    private static void PushAllToStack(Territory territory, EncounterState state)
    {
        foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
            PushInvaderToStack(invader, territory, state);
    }

    private static void PushAllInTerritory(Territory territory, EncounterState state)
    {
        foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
            PushInvader(invader, territory, state);
    }

    /// <summary>
    /// Pushes an invader toward the most-populated adjacent territory (creates a stacking "tall problem").
    /// Falls back to away-from-heart direction if all neighbors are empty.
    /// </summary>
    private static void PushInvaderToStack(Invader invader, Territory from, EncounterState state)
    {
        var neighbors = state.Graph.GetNeighbors(from.Id).ToList();
        if (neighbors.Count == 0) return;

        // Prefer neighbor with the most alive invaders (for stacking)
        var pushTargetId = neighbors
            .OrderByDescending(n => state.GetTerritory(n)?.Invaders.Count(i => i.IsAlive) ?? 0)
            .First();

        // If all neighbors are empty, fall back to away-from-heart (toward spawn)
        if ((state.GetTerritory(pushTargetId)?.Invaders.Count(i => i.IsAlive) ?? 0) == 0)
        {
            int currentDist = state.Graph.Distance(from.Id, state.Graph.HeartId);
            var spawnward = neighbors
                .Where(n => state.Graph.Distance(n, state.Graph.HeartId) >= currentDist)
                .OrderByDescending(n => state.Graph.Distance(n, state.Graph.HeartId))
                .FirstOrDefault();
            if (spawnward != null) pushTargetId = spawnward;
        }

        var dest = state.GetTerritory(pushTargetId);
        if (dest == null) return;

        from.Invaders.Remove(invader);
        invader.TerritoryId = pushTargetId;
        dest.Invaders.Add(invader);
        GameEvents.InvaderAdvanced?.Invoke(invader, from.Id, pushTargetId);
    }

    /// <summary>Pushes an invader toward spawn (away from I1). Used by Gale T3.</summary>
    private static void PushInvader(Invader invader, Territory from, EncounterState state)
    {
        int currentDist = state.Graph.Distance(from.Id, state.Graph.HeartId);
        var pushTargetId = state.Graph.GetNeighbors(from.Id)
            .Where(n => state.Graph.Distance(n, state.Graph.HeartId) >= currentDist)
            .OrderByDescending(n => state.Graph.Distance(n, state.Graph.HeartId))
            .FirstOrDefault();

        if (pushTargetId == null) return;
        var dest = state.GetTerritory(pushTargetId);
        if (dest == null) return;

        from.Invaders.Remove(invader);
        invader.TerritoryId = pushTargetId;
        dest.Invaders.Add(invader);
        GameEvents.InvaderAdvanced?.Invoke(invader, from.Id, pushTargetId);
    }

    // ── Void ─────────────────────────────────────────────────────────────────

    // T1: Deal 3 damage to the lowest-HP alive invader on the board; kills generate Fear
    // T2: All invaders take 1 damage + all Natives take 1 damage; Infrastructure not affected; kills generate Fear
    // T3: All invaders take 1 damage; kills generate Fear
    //     (cumulative: T1+T2+T3 = 2 dmg to all invaders + 3 bonus to lowest-HP + 1 dmg to all Natives)
    private static void ResolveVoid(int tier, EncounterState state)
    {
        switch (tier)
        {
            case 1:
            {
                var (invader, _) = state.Territories
                    .SelectMany(t => t.Invaders.Where(i => i.IsAlive).Select(i => (invader: i, territory: t)))
                    .OrderBy(x => x.invader.Hp)
                    .FirstOrDefault();
                if (invader == null) return;
                ApplyDamageWithFear(invader, 3, state);
                break;
            }

            case 2:
                foreach (var territory in state.Territories)
                {
                    foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
                        ApplyDamageWithFear(invader, 1, state);
                    // Natives take damage; Infrastructure tokens are unaffected by design
                    foreach (var native in territory.Natives.Where(n => n.IsAlive).ToList())
                        ApplyDamageToNative(native, territory, 1);
                }
                break;

            case 3:
                foreach (var territory in state.Territories)
                    foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
                        ApplyDamageWithFear(invader, 1, state);
                break;
        }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>Shield X blocks damage &lt; X; damage &gt;= X breaks shield and deals full damage.</summary>
    private static void ApplyDamage(Invader invader, int damage)
    {
        if (damage < invader.ShieldValue) return;
        invader.ShieldValue = 0;
        invader.Hp = Math.Max(0, invader.Hp - damage);
        if (!invader.IsAlive)
            GameEvents.InvaderDefeated?.Invoke(invader);
    }

    /// <summary>Like ApplyDamage but also fires FearGenerated on kills.</summary>
    private static void ApplyDamageWithFear(Invader invader, int damage, EncounterState state)
    {
        if (damage < invader.ShieldValue) return;
        invader.ShieldValue = 0;
        invader.Hp = Math.Max(0, invader.Hp - damage);
        if (!invader.IsAlive)
        {
            GameEvents.InvaderDefeated?.Invoke(invader);
            int fear = state.ApplyFearMultiplier(1);
            state.Dread?.OnFearGenerated(fear);
            GameEvents.FearGenerated?.Invoke(fear);
        }
    }

    private static void ApplyDamageToNative(Native native, Territory territory, int damage)
    {
        if (damage < native.ShieldValue) return;
        native.ShieldValue = 0;
        native.Hp = Math.Max(0, native.Hp - damage);
        if (!native.IsAlive)
            GameEvents.NativeDefeated?.Invoke(native, territory);
    }
}
