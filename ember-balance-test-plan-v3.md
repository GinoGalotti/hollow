# Ember Balance Test Plan v3 — Surgical Per-Element Tuning

## Corrected Understanding

- Each threshold tier fires **once per turn** (confirmed by 400+ unit tests)
- Cascade = T1(1) + T2(2) + T3(3) = **6 free damage/turn**
- T1 hits most-invaded territory, T2 hits one territory, **T3 hits ALL invaders board-wide**
- With decay 1/turn from 11+ Ash, T3 never turns off once active
- 6 damage ≥ all invader HP (Outrider 3, Marcher 4, Ironclad 5), so nothing survives

## Why Per-Element Overrides Change Everything

Previously, nerfing T3 damage or thresholds was global — it would break Root too. Now we can nerf Ash T3 specifically:

- **Ash T3 damage 3→2:** Focused territory takes 1+2+2=5 (still kills everything). But non-focused territories only take T3=2, so Outriders survive (1 HP), Marchers survive (2 HP), Ironclads survive (3 HP). Flanking invaders live.
- **Ash T3 threshold 11→14:** T3 comes online 1-2 turns later, Ember-specific. Root untouched.
- **Both together:** Fewer T3 turns AND each T3 turn does less board-wide damage.

None of these touch Root's balance.

---

## The 5 Scenarios

### A — Baseline (have it)
48.4% Clean, 51.6% Weathered, 0% Breach. Weave 20/20 every tide. Zero heart damage.

### B — Faster Element Decay (global)
**Override:** `element_decay_per_turn: 2`
**Effect:** Pool at 11 decays to 9 (below T3). Rest turns = no T3.
**Tests:** Is maintenance cost alone enough?
**Caveat:** Global — needs Root re-test before shipping.

### C — Ash T3 Damage 2 (surgical)
**Override:** `element_overrides.Ash.t3_damage: 2`
**Effect:** Focused territory: 1+2+2=5 (still lethal). Board-wide T3 hit: 2 (flanking invaders survive).
**Tests:** Does reduced board-wide overkill create enough throughput for heart pressure?
**Best single knob:** Ember-specific, no engine timing change, directly addresses T3 board-wipe.

### D — Ash T3 Threshold 14 (surgical)
**Override:** `element_overrides.Ash.tier3_threshold: 14`
**Effect:** T3 delayed 1-2 turns. With decay 1/turn, pool drops below 14 next turn unless bot plays more Ash.
**Tests:** Does an early-game vulnerability window change outcomes?

### E — Decay 2 + Ash T3 Damage 2
**Overrides:** `element_decay_per_turn: 2` + `element_overrides.Ash.t3_damage: 2`
**Effect:** T3 fires less often AND deals less board-wide damage.
**Tests:** If C alone isn't enough, does adding maintenance cost reach the target?

### F — Ash T3 at 14 + Ash T3 Damage 2 (double surgical)
**Overrides:** `element_overrides.Ash.tier3_threshold: 14` + `element_overrides.Ash.t3_damage: 2`
**Effect:** T3 delayed AND weaker. Entirely Ember-specific.
**Tests:** The cleanest possible nerf. If this hits target, it's a pure config change with zero Root impact.

---

## How to Read Results

```
C alone hits target?
  YES → Ship Ash t3_damage: 2. Simplest, Ember-only, no side effects.
  NO (still 0% breach) →
    F hits target?
      YES → Ship Ash T3@14 + damage 2. Pure Ember config, no Root impact.
      NO →
        E hits target?
          YES → Decay 2 + Ash t3_damage 2. Needs Root re-test (global decay).
          NO → Need code changes or bigger redesign.

B alone hits target?
  YES → Decay 2 works, but test Root before shipping.

D alone hits target?
  YES → Just raise Ash T3 threshold. Likely temporary fix.
```

**Ideal outcome:** C or F hits target → pure Ember config, no global impact, no code.

---

## How to Run

```bash
# Copy sim-profiles/ to project root, then:
bash run-ember-tests.sh
```

---

## What to Send Back

1. All 5 summary.txt files in one message
2. Any verbose log where weave dropped below 15 or breach occurred

---

## Decision Matrix

| Scenario | Knobs | Clean% | Weath% | Breach% | Weave | Heart | Root-safe? |
|----------|-------|--------|--------|---------|-------|-------|------------|
| A baseline | — | 48.4 | 51.6 | 0 | 20.0 | 0 | — |
| B decay 2 | decay=2 | | | | | | NO (global) |
| C Ash T3→2 | Ash.t3_dmg=2 | | | | | | YES |
| D Ash T3@14 | Ash.tier3=14 | | | | | | YES |
| E B+C | decay=2 + Ash.t3_dmg=2 | | | | | | NO (global) |
| F D+C | Ash.tier3=14 + Ash.t3_dmg=2 | | | | | | YES |

**Target:** 50–70% Clean, 20–35% Weathered, 5–15% Breach, weave 14–18, heart dmg 0.5–2.0
