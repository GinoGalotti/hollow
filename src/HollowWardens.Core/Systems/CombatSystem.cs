namespace HollowWardens.Core.Systems;

using HollowWardens.Core.Events;
using HollowWardens.Core.Map;
using HollowWardens.Core.Models;
using HollowWardens.Core.Encounter;

public class CombatSystem : ICombatSystem
{
    public const string RavageId = "ravage";
    public const string MarchId  = "march";
    public const string SettleId = "settle";

    // Invaders that were in I1 before the current Advance — eligible to march on Heart.
    private readonly HashSet<string> _preAdvanceI1Invaders = new();

    // ── Activate ──────────────────────────────────────────────────────────────

    public void ExecuteActivate(ActionCard action, Territory territory, EncounterState state)
    {
        var alive = territory.Invaders.Where(i => i.IsAlive).ToList();

        switch (action.Id)
        {
            case RavageId: ExecuteRavage(territory, alive, state); break;
            case MarchId:  ExecuteMarch(territory, alive);         break;
            case SettleId: ExecuteSettle(territory, alive);        break;
        }

        // Pioneer modifier: build one Infrastructure token per Pioneer after any Activate.
        foreach (var inv in alive.Where(i => i.UnitType == UnitType.Pioneer))
            territory.Tokens.Add(new Infrastructure { TerritoryId = territory.Id });
    }

    private static void ExecuteRavage(Territory territory, List<Invader> invaders, EncounterState state)
    {
        int totalCorruption = 0;
        int totalDamage     = 0;

        foreach (var inv in invaders)
        {
            GameEvents.InvaderActivated?.Invoke(inv, territory);

            // Outrider pre-hit: 1 damage to lowest-HP alive native before main Ravage.
            if (inv.UnitType == UnitType.Outrider)
            {
                var preTarget = territory.Natives
                    .Where(n => n.IsAlive)
                    .OrderBy(n => n.Hp)
                    .FirstOrDefault();
                if (preTarget != null)
                    ApplyDamageToNative(preTarget, 1, territory);
            }

            // Corruption: base 1 per unit; Ironclad +1.
            totalCorruption += inv.UnitType == UnitType.Ironclad ? 2 : 1;

            // Native damage: Pioneer builds instead of fighting.
            if (inv.UnitType != UnitType.Pioneer)
                totalDamage += 1;
        }

        state.Corruption?.AddCorruption(territory, totalCorruption);
        DistributeDamageToNatives(territory, totalDamage);
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
        // Record which invaders were already in I1 — only they can march this Tide.
        _preAdvanceI1Invaders.Clear();
        var i1 = state.GetTerritory("I1");
        if (i1 != null)
            foreach (var inv in i1.Invaders.Where(i => i.IsAlive))
                _preAdvanceI1Invaders.Add(inv.Id);

        // Collect moves before applying any (prevents double-movement).
        var moves = new List<(Invader invader, string destination)>();

        foreach (var territory in state.Territories.Where(t => t.Row != TerritoryRow.Inner))
        {
            foreach (var invader in territory.Invaders.Where(i => i.IsAlive).ToList())
            {
                int steps = GetSteps(action, invader);
                if (steps == 0) continue;

                string currentId = invader.TerritoryId;
                for (int s = 0; s < steps; s++)
                {
                    var next = GetNextTowardHeart(currentId);
                    if (next == null) break;
                    currentId = next;
                    if (currentId == "I1") break; // grace: stop at heart zone
                }

                if (currentId != invader.TerritoryId)
                    moves.Add((invader, currentId));
            }
        }

        foreach (var (invader, dest) in moves)
            MoveInvader(invader, dest, state);
    }

    // ── Heart March ───────────────────────────────────────────────────────────

    public void ExecuteHeartMarch(EncounterState state)
    {
        var i1 = state.GetTerritory("I1");
        if (i1 == null) return;

        foreach (var invader in i1.Invaders
            .Where(i => i.IsAlive && _preAdvanceI1Invaders.Contains(i.Id))
            .ToList())
        {
            int damage = Math.Max(1, invader.Hp);
            state.Weave?.DealDamage(damage);
            GameEvents.HeartDamageDealt?.Invoke(i1);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static int GetSteps(ActionCard action, Invader invader)
    {
        if (invader.UnitType == UnitType.Ironclad)
        {
            bool shouldMove = invader.AlternateMoveTurn;
            invader.AlternateMoveTurn = !invader.AlternateMoveTurn;
            if (!shouldMove) return 0;
        }

        int steps = action.AdvanceModifier;
        if (invader.UnitType == UnitType.Outrider)
            steps += 1;

        return steps;
    }

    /// <summary>
    /// Returns the adjacent territory one step closer to I1 (alphabetically first on tie).
    /// Returns null if already at I1.
    /// </summary>
    private static string? GetNextTowardHeart(string currentId)
    {
        int currentDist = TerritoryGraph.Distance(currentId, "I1");
        if (currentDist == 0) return null;

        return TerritoryGraph.GetNeighbors(currentId)
            .Where(n => TerritoryGraph.Distance(n, "I1") < currentDist)
            .OrderBy(n => n)
            .FirstOrDefault();
    }

    private static void MoveInvader(Invader invader, string toId, EncounterState state)
    {
        string fromId = invader.TerritoryId;
        state.GetTerritory(fromId)?.Invaders.Remove(invader);
        invader.TerritoryId = toId;
        var toTerritory = state.GetTerritory(toId);
        toTerritory?.Invaders.Add(invader);
        GameEvents.InvaderAdvanced?.Invoke(invader, fromId, toId);
        if (toId == "I1" && toTerritory != null)
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
