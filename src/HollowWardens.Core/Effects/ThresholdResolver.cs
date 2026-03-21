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
        [(Element.Root,   1)] = "Root T1: Place Presence adjacent",
        [(Element.Root,   2)] = "Root T2: Reduce Corruption ×3 in one territory",
        [(Element.Root,   3)] = "Root T3: Place 2 Presence + Reduce Corruption ×2 each",
        [(Element.Mist,   1)] = "Mist T1: +1 Weave",
        [(Element.Mist,   2)] = "Mist T2: Return 1 card from discard to hand",
        [(Element.Mist,   3)] = "Mist T3: +3 Weave + return all discard to hand",
        [(Element.Shadow, 1)] = "Shadow T1: +2 Fear",
        [(Element.Shadow, 2)] = "Shadow T2: Next Fear Action draws from Dread+1",
        [(Element.Shadow, 3)] = "Shadow T3: +5 Fear",
        [(Element.Ash,    1)] = "Ash T1: 1 damage to all in most-invaded territory",
        [(Element.Ash,    2)] = "Ash T2: 2 damage to all in one territory + 1 Corruption",
        [(Element.Ash,    3)] = "Ash T3: 3 damage to ALL invaders on board + 1 Corruption each territory",
        [(Element.Gale,   1)] = "Gale T1: Push 1 invader toward spawn",
        [(Element.Gale,   2)] = "Gale T2: Push all invaders in one territory toward spawn",
        [(Element.Gale,   3)] = "Gale T3: Push all invaders on board toward spawn",
        [(Element.Void,   1)] = "Void T1: 1 damage to lowest-HP invader",
        [(Element.Void,   2)] = "Void T2: All invaders take 1 damage",
        [(Element.Void,   3)] = "Void T3: All invaders take 2 damage",
    };

    private static string GetDescription(Element element, int tier)
        => Descriptions.TryGetValue((element, tier), out var d) ? d : $"{element} T{tier}";

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
    /// </summary>
    public void Resolve(Element element, int tier, EncounterState state)
    {
        for (int i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].element == element && _pending[i].tier == tier)
            {
                _pending.RemoveAt(i);
                break;
            }
        }

        ExecuteEffect(element, tier, state);
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

    private static void ExecuteEffect(Element element, int tier, EncounterState state)
    {
        switch (element)
        {
            case Element.Root:   ResolveRoot(tier, state);   break;
            case Element.Mist:   ResolveMist(tier, state);   break;
            case Element.Shadow: ResolveShadow(tier, state); break;
            case Element.Ash:    ResolveAsh(tier, state);    break;
            case Element.Gale:   ResolveGale(tier, state);   break;
            case Element.Void:   ResolveVoid(tier, state);   break;
        }
    }

    // ── Root ─────────────────────────────────────────────────────────────────

    // T1: Place 1 Presence at range 1 from any existing Presence token
    // T2: Reduce Corruption by 3 in one territory with Presence (auto: highest corruption)
    // T3: Place 2 Presence (adjacent to existing) + Reduce Corruption by 2 in each presence territory
    private static void ResolveRoot(int tier, EncounterState state)
    {
        switch (tier)
        {
            case 1:
                PlacePresenceAdjacent(state);
                break;

            case 2:
                var target = state.Territories
                    .Where(t => t.HasPresence && t.CorruptionPoints > 0)
                    .OrderByDescending(t => t.CorruptionPoints)
                    .FirstOrDefault();
                if (target != null)
                    state.Corruption?.ReduceCorruption(target, 3);
                break;

            case 3:
                PlacePresenceAdjacent(state); // first Presence
                PlacePresenceAdjacent(state); // second Presence (from any existing, including just-placed)
                foreach (var t in state.Territories.Where(t => t.HasPresence))
                    state.Corruption?.ReduceCorruption(t, 2);
                break;
        }
    }

    private static void PlacePresenceAdjacent(EncounterState state)
    {
        var presenceTerritories = state.Territories.Where(t => t.HasPresence).ToList();
        foreach (var source in presenceTerritories)
        {
            var neighbor = TerritoryGraph.GetNeighbors(source.Id)
                .Select(id => state.GetTerritory(id))
                .FirstOrDefault(t => t != null && !t.HasPresence);
            if (neighbor != null)
            {
                state.Presence?.PlacePresence(neighbor);
                return;
            }
        }
        // Fallback: stack on an existing presence territory
        var fallback = presenceTerritories.FirstOrDefault();
        if (fallback != null)
            state.Presence?.PlacePresence(fallback);
    }

    // ── Mist ─────────────────────────────────────────────────────────────────

    // T1: Restore 1 Weave
    // T2: Return 1 card from discard to hand
    // T3: Restore 3 Weave + return ALL discard to hand
    private static void ResolveMist(int tier, EncounterState state)
    {
        switch (tier)
        {
            case 1:
                state.Weave?.Restore(1);
                break;

            case 2:
                state.Deck?.ReturnDiscardToHand(1);
                break;

            case 3:
                state.Weave?.Restore(3);
                state.Deck?.ReturnDiscardToHand(int.MaxValue);
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
    // T3: 3 damage to ALL invaders on board + 1 Corruption per affected territory
    private static void ResolveAsh(int tier, EncounterState state)
    {
        switch (tier)
        {
            case 1:
            {
                var target = state.TerritoriesWithInvaders()
                    .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                    .FirstOrDefault();
                if (target == null) return;
                foreach (var invader in target.Invaders.Where(i => i.IsAlive).ToList())
                    ApplyDamage(invader, 1);
                break;
            }

            case 2:
            {
                var target = state.TerritoriesWithInvaders()
                    .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
                    .FirstOrDefault();
                if (target == null) return;
                foreach (var invader in target.Invaders.Where(i => i.IsAlive).ToList())
                    ApplyDamage(invader, 2);
                state.Corruption?.AddCorruption(target, 1);
                break;
            }

            case 3:
            {
                var affected = state.TerritoriesWithInvaders().ToList();
                foreach (var territory in affected)
                {
                    foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
                        ApplyDamage(invader, 3);
                    state.Corruption?.AddCorruption(territory, 1);
                }
                break;
            }
        }
    }

    // ── Gale ─────────────────────────────────────────────────────────────────

    // T1: Push 1 invader one territory toward spawn (away from I1)
    // T2: Push ALL invaders in one territory (auto: closest to I1)
    // T3: Push ALL invaders on board one territory toward spawn
    private static void ResolveGale(int tier, EncounterState state)
    {
        switch (tier)
        {
            case 1:
            {
                var territory = state.TerritoriesWithInvaders()
                    .OrderBy(t => TerritoryGraph.Distance(t.Id, "I1"))
                    .FirstOrDefault();
                if (territory == null) return;
                var invader = territory.Invaders.FirstOrDefault(i => i.IsAlive);
                if (invader != null)
                    PushInvader(invader, territory, state);
                break;
            }

            case 2:
            {
                var territory = state.TerritoriesWithInvaders()
                    .OrderBy(t => TerritoryGraph.Distance(t.Id, "I1"))
                    .FirstOrDefault();
                if (territory == null) return;
                PushAllInTerritory(territory, state);
                break;
            }

            case 3:
            {
                // Process farthest-from-I1 first so moved invaders aren't pushed twice
                var territories = state.TerritoriesWithInvaders()
                    .OrderByDescending(t => TerritoryGraph.Distance(t.Id, "I1"))
                    .ToList();
                foreach (var territory in territories)
                    PushAllInTerritory(territory, state);
                break;
            }
        }
    }

    private static void PushAllInTerritory(Territory territory, EncounterState state)
    {
        foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
            PushInvader(invader, territory, state);
    }

    private static void PushInvader(Invader invader, Territory from, EncounterState state)
    {
        int currentDist = TerritoryGraph.Distance(from.Id, "I1");
        var pushTargetId = TerritoryGraph.GetNeighbors(from.Id)
            .Where(n => TerritoryGraph.Distance(n, "I1") >= currentDist)
            .OrderByDescending(n => TerritoryGraph.Distance(n, "I1"))
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

    // T1: Deal 1 damage to the lowest-HP alive invader on the board
    // T2: All invaders take 1 damage
    // T3: All invaders take 2 damage (design note: "no Corruption on death" deferred — no per-death Corruption in current model)
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
                ApplyDamage(invader, 1);
                break;
            }

            case 2:
                foreach (var territory in state.Territories)
                    foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
                        ApplyDamage(invader, 1);
                break;

            case 3:
                foreach (var territory in state.Territories)
                    foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
                        ApplyDamage(invader, 2);
                break;
        }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    // Shield X blocks damage < X; damage >= X breaks shield and deals full damage.
    private static void ApplyDamage(Invader invader, int damage)
    {
        if (damage < invader.ShieldValue) return;
        invader.ShieldValue = 0;
        invader.Hp = Math.Max(0, invader.Hp - damage);
        if (!invader.IsAlive)
            GameEvents.InvaderDefeated?.Invoke(invader);
    }
}
