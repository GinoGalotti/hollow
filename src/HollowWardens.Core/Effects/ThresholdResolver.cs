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
        [(Element.Root,   1)] = "Root T1: Place Presence",
        [(Element.Mist,   1)] = "Mist T1: +1 Weave",
        [(Element.Shadow, 1)] = "Shadow T1: +2 Fear",
        [(Element.Ash,    1)] = "Ash T1: Damage Invaders",
        [(Element.Gale,   1)] = "Gale T1: Push Invader",
        [(Element.Void,   1)] = "Void T1: Damage Weakest",
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

        if (tier == 1)
            ExecuteEffect(element, state); // T2/T3 effects not yet implemented
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
    /// Legacy entry point — tests call this directly.
    /// </summary>
    public void AutoResolve(Element element, int tier, EncounterState state)
    {
        if (tier != 1) return;
        ExecuteEffect(element, state);
        state.Elements?.ResolveBanked(element, tier);
        GameEvents.ThresholdResolved?.Invoke(element, tier, GetDescription(element, tier));
    }

    // ── Effect implementations ────────────────────────────────────────────────

    private static void ExecuteEffect(Element element, EncounterState state)
    {
        switch (element)
        {
            case Element.Root:   ResolveRoot(state);   break;
            case Element.Mist:   ResolveMist(state);   break;
            case Element.Shadow: ResolveShadow(state); break;
            case Element.Ash:    ResolveAsh(state);    break;
            case Element.Gale:   ResolveGale(state);   break;
            case Element.Void:   ResolveVoid(state);   break;
        }
    }

    // Root T1: Place 1 Presence at range 1 from any existing Presence token
    private static void ResolveRoot(EncounterState state)
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
        var fallback = presenceTerritories.FirstOrDefault();
        if (fallback != null)
            state.Presence?.PlacePresence(fallback);
    }

    // Mist T1: Restore 1 Weave
    private static void ResolveMist(EncounterState state) => state.Weave?.Restore(1);

    // Shadow T1: Generate 2 Fear
    private static void ResolveShadow(EncounterState state)
    {
        state.Dread?.OnFearGenerated(2);
        GameEvents.FearGenerated?.Invoke(2);
    }

    // Ash T1: Deal 1 damage to all invaders in the territory with the most invaders
    private static void ResolveAsh(EncounterState state)
    {
        var target = state.TerritoriesWithInvaders()
            .OrderByDescending(t => t.Invaders.Count(i => i.IsAlive))
            .FirstOrDefault();
        if (target == null) return;

        foreach (var invader in target.Invaders.Where(i => i.IsAlive).ToList())
            ApplyDamage(invader, 1);
    }

    // Gale T1: Push 1 invader one territory toward spawn (away from I1)
    private static void ResolveGale(EncounterState state)
    {
        var territory = state.TerritoriesWithInvaders()
            .OrderBy(t => TerritoryGraph.Distance(t.Id, "I1"))
            .FirstOrDefault();
        if (territory == null) return;

        var invader = territory.Invaders.FirstOrDefault(i => i.IsAlive);
        if (invader == null) return;

        int currentDist = TerritoryGraph.Distance(territory.Id, "I1");
        var pushTargetId = TerritoryGraph.GetNeighbors(territory.Id)
            .Where(n => TerritoryGraph.Distance(n, "I1") >= currentDist)
            .OrderByDescending(n => TerritoryGraph.Distance(n, "I1"))
            .FirstOrDefault();

        if (pushTargetId == null) return;
        var dest = state.GetTerritory(pushTargetId);
        if (dest == null) return;

        territory.Invaders.Remove(invader);
        invader.TerritoryId = pushTargetId;
        dest.Invaders.Add(invader);
        GameEvents.InvaderAdvanced?.Invoke(invader, territory.Id, pushTargetId);
    }

    // Void T1: Deal 1 damage to the lowest-HP alive invader on the board
    private static void ResolveVoid(EncounterState state)
    {
        var (invader, _) = state.Territories
            .SelectMany(t => t.Invaders.Where(i => i.IsAlive).Select(i => (invader: i, territory: t)))
            .OrderBy(x => x.invader.Hp)
            .FirstOrDefault();

        if (invader == null) return;
        ApplyDamage(invader, 1);
    }

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
