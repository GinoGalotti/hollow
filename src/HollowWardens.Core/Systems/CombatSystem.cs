namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Encounter;

public class CombatSystem : ICombatSystem
{
    public const string RavageId  = "ravage";
    public const string CorruptId = "corrupt";
    public const string MarchId   = "march";
    public const string SettleId  = "settle";

    /// <summary>
    /// Returns true when the action provokes a native counter-attack.
    /// Only Ravage and Corrupt actions (e.g. "pm_ravage", "pm_corrupt") provoke.
    /// </summary>
    public bool IsProvokedAction(ActionCard action)
    {
        string id = action.Id ?? string.Empty;
        return id.Contains(RavageId,  StringComparison.OrdinalIgnoreCase)
            || id.Contains(CorruptId, StringComparison.OrdinalIgnoreCase);
    }

    // Invaders that were in the heart zone before the current Advance — eligible to march on Heart.
    private readonly HashSet<string> _preAdvanceI1Invaders = new();

    // ── Activate ──────────────────────────────────────────────────────────────

    public void ExecuteActivate(ActionCard action, Territory territory, EncounterState state)
    {
        var alive = territory.Invaders.Where(i => i.IsAlive).ToList();

        string id = action.Id ?? string.Empty;
        if      (id.Contains(RavageId,  StringComparison.OrdinalIgnoreCase)) ExecuteRavage(territory, alive, state);
        else if (id.Contains(MarchId,   StringComparison.OrdinalIgnoreCase)) ExecuteMarch(territory, alive);
        else if (id.Contains(SettleId,  StringComparison.OrdinalIgnoreCase)) ExecuteSettle(territory, alive);

        // Pioneer modifier: build one Infrastructure token per Pioneer after any Activate.
        foreach (var inv in alive.Where(i => i.UnitType == UnitType.Pioneer))
            territory.Tokens.Add(new Infrastructure { TerritoryId = territory.Id });
    }

    private static void ExecuteRavage(Territory territory, List<Invader> invaders, EncounterState state)
    {
        int totalCorruption = state.Balance.BaseRavageCorruption;

        foreach (var inv in invaders)
        {
            GameEvents.InvaderActivated?.Invoke(inv, territory);

            // Outrider pre-hit: 2 damage to lowest-HP alive native before main Ravage.
            if (inv.UnitType == UnitType.Outrider)
            {
                var preTarget = territory.Natives
                    .Where(n => n.IsAlive)
                    .OrderBy(n => n.Hp)
                    .FirstOrDefault();
                if (preTarget != null)
                    ApplyDamageToNative(preTarget, 2, territory);
            }

            // Corruption pool = native damage pool (corruption IS the damage).
            totalCorruption += inv.UnitType switch
            {
                UnitType.Marcher  => 2,
                UnitType.Ironclad => 3,
                UnitType.Outrider => 1,
                UnitType.Pioneer  => 2,
                _                 => 1,
            };
        }

        totalCorruption = (int)(totalCorruption * state.Balance.CorruptionRateMultiplier);
        state.Corruption?.AddCorruption(territory, totalCorruption);
        DistributeDamageToNatives(territory, totalCorruption);
    }

    private static void ExecuteMarch(Territory territory, List<Invader> invaders)
    {
        foreach (var inv in invaders)
        {
            inv.ShieldValue = Math.Max(inv.ShieldValue, 2);
            inv.Hp          = Math.Min(inv.MaxHp, inv.Hp + 1);
            GameEvents.InvaderActivated?.Invoke(inv, territory);
        }
    }

    private static void ExecuteSettle(Territory territory, List<Invader> invaders)
    {
        foreach (var inv in invaders)
        {
            inv.ShieldValue = Math.Max(inv.ShieldValue, 1);
            GameEvents.InvaderActivated?.Invoke(inv, territory);
        }
    }

    // ── Native damage pool & counter-attack ───────────────────────────────────

    public int CalculateNativeDamagePool(Territory territory)
        => territory.Natives.Where(n => n.IsAlive).Sum(n => n.Damage);

    public void ApplyCounterAttack(Territory territory, Dictionary<Invader, int> damageAssignments)
    {
        foreach (var (invader, damage) in damageAssignments)
        {
            if (!invader.IsAlive) continue;
            ApplyDamageToInvader(invader, damage);
            if (!invader.IsAlive)
                GameEvents.InvaderDefeated?.Invoke(invader);
        }
    }

    /// <summary>
    /// Auto-assigns the native damage pool to alive invaders, lowest HP first,
    /// allocating exactly enough to kill before moving to the next.
    /// </summary>
    public void AutoAssignCounterAttack(Territory territory)
    {
        int pool     = CalculateNativeDamagePool(territory);
        var invaders = territory.Invaders
            .Where(i => i.IsAlive)
            .OrderBy(i => i.Hp)
            .ToList();

        foreach (var invader in invaders)
        {
            if (pool <= 0) break;
            int allocate = Math.Min(pool, invader.Hp);
            ApplyDamageToInvader(invader, allocate);
            pool -= allocate;
            if (!invader.IsAlive)
                GameEvents.InvaderDefeated?.Invoke(invader);
        }
    }

    // ── Advance ───────────────────────────────────────────────────────────────

    public void ExecuteAdvance(ActionCard action, EncounterState state)
    {
        var graph  = state.Graph;
        string heartId = graph.HeartId;

        // Record which invaders were already in the heart zone — only they can march this Tide.
        _preAdvanceI1Invaders.Clear();
        var heart = state.GetTerritory(heartId);
        if (heart != null)
            foreach (var inv in heart.Invaders.Where(i => i.IsAlive))
                _preAdvanceI1Invaders.Add(inv.Id);

        // Collect moves before applying any (prevents double-movement).
        var moves = new List<(Invader invader, string destination)>();

        foreach (var territory in state.Territories.Where(t => t.Row != TerritoryRow.Inner))
        {
            foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
            {
                // D29: Network Slow — ask warden for movement penalty at this territory
                int penalty = state.Warden?.GetMovementPenalty(territory.Id, state.Territories) ?? 0;
                int steps = GetSteps(action, invader, penalty, state.Config.InvaderAdvanceBonus);
                if (steps == 0) continue;

                string currentId = invader.TerritoryId;
                for (int s = 0; s < steps; s++)
                {
                    var next = GetNextTowardHeart(currentId, graph, heartId);
                    if (next == null) break;
                    currentId = next;
                    if (currentId == heartId) break; // grace: stop at heart zone
                }

                if (currentId != invader.TerritoryId)
                    moves.Add((invader, currentId));
            }
        }

        foreach (var (invader, dest) in moves)
            MoveInvader(invader, dest, state, heartId);
    }

    // ── Heart March ───────────────────────────────────────────────────────────

    public void ExecuteHeartMarch(EncounterState state)
    {
        string heartId = state.Graph.HeartId;
        var heart = state.GetTerritory(heartId);
        if (heart == null) return;

        foreach (var invader in heart.Invaders
            .Where(i => i.IsAlive && _preAdvanceI1Invaders.Contains(i.Id))
            .ToList())
        {
            int damage = (int)(Math.Max(1, invader.Hp) * state.Config.HeartDamageMultiplier);
            state.Weave?.DealDamage(damage);
            GameEvents.HeartDamageDealt?.Invoke(heart);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static int GetSteps(ActionCard action, Invader invader, int movementPenalty = 0, int advanceBonus = 0)
    {
        if (invader.UnitType == UnitType.Ironclad)
        {
            bool shouldMove = invader.AlternateMoveTurn;
            invader.AlternateMoveTurn = !invader.AlternateMoveTurn;
            if (!shouldMove) return 0;
        }

        // Apply advance bonus BEFORE penalty so penalty can offset it
        int steps = action.AdvanceModifier + advanceBonus;
        if (invader.UnitType == UnitType.Outrider)
            steps += 1;

        // D29: SlowInvaders halves movement (round down)
        if (invader.IsSlowed)
            steps = steps / 2;

        // D29: Apply Network Slow penalty (minimum 0 total movement)
        steps = Math.Max(0, steps - movementPenalty);

        return steps;
    }

    /// <summary>
    /// Returns the adjacent territory one step closer to the heart (alphabetically first on tie).
    /// Returns null if already at heart.
    /// </summary>
    private static string? GetNextTowardHeart(string currentId, TerritoryGraph graph, string heartId)
    {
        int currentDist = graph.Distance(currentId, heartId);
        if (currentDist == 0) return null;

        return graph.GetNeighbors(currentId)
            .Where(n => graph.Distance(n, heartId) < currentDist)
            .OrderBy(n => n)
            .FirstOrDefault();
    }

    private static void MoveInvader(Invader invader, string toId, EncounterState state, string heartId)
    {
        string fromId = invader.TerritoryId;
        state.GetTerritory(fromId)?.Invaders.Remove(invader);
        invader.TerritoryId = toId;
        var toTerritory = state.GetTerritory(toId);
        toTerritory?.Invaders.Add(invader);
        GameEvents.InvaderAdvanced?.Invoke(invader, fromId, toId);
        if (toId == heartId && toTerritory != null)
            GameEvents.InvaderArrived?.Invoke(invader, toTerritory);
    }

    /// <summary>
    /// Distributes damage to alive natives, targeting lowest HP first to maximize kills.
    /// Allocates exactly enough to kill each target before moving to the next.
    /// </summary>
    private static void DistributeDamageToNatives(Territory territory, int totalDamage)
    {
        var natives   = territory.Natives.Where(n => n.IsAlive).OrderBy(n => n.Hp).ToList();
        int remaining = totalDamage;

        foreach (var native in natives)
        {
            if (remaining <= 0) break;
            int dmg = Math.Min(remaining, native.Hp);
            ApplyDamageToNative(native, dmg, territory);
            remaining -= dmg;
        }
    }

    private static void ApplyDamageToNative(Native native, int damage, Territory territory)
    {
        if (damage < native.ShieldValue) return; // blocked
        native.ShieldValue = 0;
        native.Hp = Math.Max(0, native.Hp - damage);
        GameEvents.NativeDamaged?.Invoke(native, territory);
        if (!native.IsAlive)
            GameEvents.NativeDefeated?.Invoke(native, territory);
    }

    /// <summary>
    /// Shield X: blocks damage below X. Damage >= X breaks shield and deals full damage.
    /// </summary>
    private static void ApplyDamageToInvader(Invader invader, int damage)
    {
        if (damage < invader.ShieldValue) return; // blocked
        invader.ShieldValue = 0;
        invader.Hp = Math.Max(0, invader.Hp - damage);
    }
}
