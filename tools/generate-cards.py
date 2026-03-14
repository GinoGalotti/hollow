#!/usr/bin/env python3
"""
generate-cards.py

Reads data/cards-{warden}.json files and regenerates:
  1. hollow_wardens/resources/cards/{warden}/  (.tres files)
  2. Embedded JSON block in cards-catalog.html (all wardens combined)

Usage:
  python tools/generate-cards.py           # process ALL data/cards-*.json
  python tools/generate-cards.py root      # process only root
  python tools/generate-cards.py root ember veil   # explicit list

Workflow for card design iterations:
  - Edit data/cards-{warden}.json to change values, names, notes.
  - Run this script.
  - All .tres files + catalog HTML update automatically.
  - Commit data/cards-*.json (source of truth) + generated files.

To add a new Warden's cards:
  1. Create data/cards-{warden}.json following the schema below.
  2. Run: python tools/generate-cards.py {warden}

JSON Schema:
  {
    "warden":   "root",           // used as subdirectory name + WardenId in .tres
    "version":  "1.0",
    "cards": [
      {
        "num":     "001",         // zero-padded, used in filename (root_001.tres)
        "id":      "root_001",    // CardData.Id
        "name":    "Card Name",   // CardData.CardName
        "cost":    0,             // CardData.Cost
        "vigil":   { "type": "ReduceCorruption", "value": 1, "range": 1, "desc": "..." },
        "dusk":    { "type": "GenerateFear",     "value": 2, "range": 0, "desc": "..." },
        "dissolve": null,         // null = use default (PlacePresence 1 range 0)
        // OR:
        "dissolve": { "type": "PlacePresence", "value": 1, "range": 3, "desc": "..." },
        "design_note": "Balance notes, open questions, intentions."
      }
    ]
  }

Valid effect type strings (maps to CardEffect.EffectType enum):
  PlacePresence, MovePresence, GenerateFear, ReduceCorruption, Purify,
  DamageInvaders, PushInvaders, RoutInvaders, RestoreWeave, PredictTide,
  Conditional, Custom, AwakeDormant
"""

import glob
import json
import os
import re
import sys

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
ROOT_DIR   = os.path.abspath(os.path.join(SCRIPT_DIR, ".."))

DATA_DIR  = os.path.join(ROOT_DIR, "data")
HTML_PATH = os.path.join(ROOT_DIR, "cards-catalog.html")
TRES_ROOT = os.path.join(ROOT_DIR, "hollow_wardens", "resources", "cards")
CSV_PATH  = os.path.join(ROOT_DIR, "hollow_wardens", "locale", "translations.csv")

EFFECT_TYPE_INT = {
    "PlacePresence":    0,
    "MovePresence":     1,
    "GenerateFear":     2,
    "ReduceCorruption": 3,
    "Purify":           4,
    "DamageInvaders":   5,
    "PushInvaders":     6,
    "RoutInvaders":     7,
    "RestoreWeave":     8,
    "PredictTide":      9,
    "Conditional":      10,
    "Custom":           11,
    "AwakeDormant":     12,
}


# ── Key derivation ─────────────────────────────────────────────────────────────

def card_key_prefix(warden_id: str, num: str) -> str:
    """Derive the translation key prefix for a card, e.g. 'root','001' -> 'CARD_ROOT_001'."""
    return f"CARD_{warden_id.upper()}_{num}"


# ── .tres generation ──────────────────────────────────────────────────────────

def effect_sub_resource(sub_id: str, eff: dict, desc_key: str) -> str:
    type_name = eff["type"]
    if type_name not in EFFECT_TYPE_INT:
        sys.exit(f"ERROR: Unknown effect type '{type_name}' — valid values: {list(EFFECT_TYPE_INT)}")
    t = EFFECT_TYPE_INT[type_name]
    v = eff.get("value", 0)
    r = eff.get("range", 0)
    return (
        f'[sub_resource type="CardEffect" id="{sub_id}"]\n'
        f'script = ExtResource("2")\n'
        f'Type = {t}\n'
        f'Value = {v}\n'
        f'Range = {r}\n'
        f'DescriptionKey = "{desc_key}"\n'
    )


def generate_tres(card: dict, warden_id: str) -> str:
    has_dissolve = card.get("dissolve") is not None
    load_steps   = 5 if has_dissolve else 4
    cost         = card.get("cost", 0)
    prefix       = card_key_prefix(warden_id, card["num"])
    name_key     = f"{prefix}_NAME"

    parts = [
        f'[gd_resource type="CardData" load_steps={load_steps} format=3]',
        "",
        '[ext_resource type="Script" path="res://scripts/data/CardData.cs" id="1"]',
        '[ext_resource type="Script" path="res://scripts/data/CardEffect.cs" id="2"]',
        "",
        effect_sub_resource("CardEffect_v", card["vigil"], f"{prefix}_VIGIL_DESC"),
        effect_sub_resource("CardEffect_d", card["dusk"],  f"{prefix}_DUSK_DESC"),
    ]

    if has_dissolve:
        parts.append(effect_sub_resource("CardEffect_dis", card["dissolve"], f"{prefix}_DISSOLVE_DESC"))

    parts += [
        "[resource]",
        'script = ExtResource("1")',
        f'Id = "{card["id"]}"',
        f'CardNameKey = "{name_key}"',
        f'WardenId = "{warden_id}"',
        f"Cost = {cost}",
        "IsDormant = false",
        'VigilEffect = SubResource("CardEffect_v")',
        'DuskEffect = SubResource("CardEffect_d")',
    ]

    if has_dissolve:
        parts.append('DissolveEffect = SubResource("CardEffect_dis")')

    parts.append("")
    return "\n".join(parts)


def process_warden(json_path: str) -> list:
    """Load a warden JSON, write all .tres files, return list of card dicts (with warden injected)."""
    with open(json_path, encoding="utf-8") as f:
        data = json.load(f)

    warden_id = data.get("warden", os.path.splitext(os.path.basename(json_path))[0].replace("cards-", ""))
    cards     = data["cards"]

    tres_dir = os.path.join(TRES_ROOT, warden_id)
    os.makedirs(tres_dir, exist_ok=True)

    print(f"\n[{warden_id}]  {len(cards)} cards  <- {os.path.relpath(json_path, ROOT_DIR)}")
    for card in cards:
        num     = card["num"]
        path    = os.path.join(tres_dir, f"{warden_id}_{num}.tres")
        content = generate_tres(card, warden_id)
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(content)
        print(f"  OK    {warden_id}_{num}.tres  ({card['name']})")

    # Inject warden field into each card dict for HTML embedding
    for card in cards:
        card["warden"] = warden_id

    return cards


# ── CSV locale update ─────────────────────────────────────────────────────────

def generate_csv_rows(all_cards: list) -> str:
    """Build CSV rows for all cards, grouped by warden."""
    rows = []
    for card in all_cards:
        warden_id = card["warden"]
        num       = card["num"]
        prefix    = card_key_prefix(warden_id, num)
        name_val  = card["name"].replace('"', '""')
        vigil_val = card["vigil"].get("desc", "").replace('"', '""')
        dusk_val  = card["dusk"].get("desc", "").replace('"', '""')
        rows.append(f'{prefix}_NAME,"{name_val}"')
        rows.append(f'{prefix}_VIGIL_DESC,"{vigil_val}"')
        rows.append(f'{prefix}_DUSK_DESC,"{dusk_val}"')
        if card.get("dissolve") is not None:
            dissolve_val = card["dissolve"].get("desc", "").replace('"', '""')
            rows.append(f'{prefix}_DISSOLVE_DESC,"{dissolve_val}"')
    return "\n".join(rows)


def update_csv(all_cards: list) -> None:
    if not os.path.exists(CSV_PATH):
        print(f"\nSKIP  CSV not found at {CSV_PATH}")
        return

    with open(CSV_PATH, encoding="utf-8") as f:
        csv = f.read()

    new_rows = generate_csv_rows(all_cards)
    replacement = (
        "# BEGIN CARDS DATA (auto-generated — edit data/cards-{warden}.json instead)\n"
        + new_rows
        + "\n# END CARDS DATA"
    )

    csv_new, n = re.subn(
        r"# BEGIN CARDS DATA.*?# END CARDS DATA",
        replacement,
        csv,
        flags=re.DOTALL,
    )

    if n == 0:
        print("\nWARN  CSV sentinel block not found — CSV not updated")
        return

    with open(CSV_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write(csv_new)
    print(f"\nOK    Updated translations CSV ({len(all_cards)} cards) -> {os.path.basename(CSV_PATH)}")


# ── HTML catalog update ───────────────────────────────────────────────────────

def update_html(all_cards: list) -> None:
    if not os.path.exists(HTML_PATH):
        print(f"\nSKIP  HTML not found at {HTML_PATH}")
        return

    with open(HTML_PATH, encoding="utf-8") as f:
        html = f.read()

    new_json    = json.dumps(all_cards, indent=2, ensure_ascii=False)
    replacement = (
        "<!-- BEGIN CARDS DATA (auto-generated — edit data/cards-{warden}.json instead) -->\n"
        '<script id="cards-data" type="application/json">\n'
        + new_json
        + "\n</script>\n"
        "<!-- END CARDS DATA -->"
    )

    html_new, n = re.subn(
        r"<!-- BEGIN CARDS DATA.*?<!-- END CARDS DATA -->",
        replacement,
        html,
        flags=re.DOTALL,
    )

    if n == 0:
        print("\nWARN  HTML data block not found — HTML not updated")
        return

    with open(HTML_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write(html_new)
    print(f"\nOK    Updated catalog ({len(all_cards)} total cards) -> {os.path.basename(HTML_PATH)}")


# ── Entry point ───────────────────────────────────────────────────────────────

def main() -> None:
    requested = sys.argv[1:]  # e.g. ["root"] or ["root", "ember"] or []

    if requested:
        json_paths = []
        for warden in requested:
            p = os.path.join(DATA_DIR, f"cards-{warden}.json")
            if not os.path.exists(p):
                sys.exit(f"ERROR: {p} not found")
            json_paths.append(p)
    else:
        json_paths = sorted(glob.glob(os.path.join(DATA_DIR, "cards-*.json")))
        if not json_paths:
            sys.exit(f"ERROR: No data/cards-*.json files found in {DATA_DIR}")

    all_cards: list = []
    for path in json_paths:
        all_cards.extend(process_warden(path))

    update_html(all_cards)
    update_csv(all_cards)
    print(f"\nDone. {len(all_cards)} cards total across {len(json_paths)} warden(s).")


if __name__ == "__main__":
    main()
