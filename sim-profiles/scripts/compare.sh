#!/usr/bin/env bash
# compare.sh — side-by-side diff of two sim-results directories.
# Highlights metrics changed by >5% (absolute for percentages, relative for counts).
#
# Usage: ./compare.sh <dir-a> <dir-b>
#
# Example: ./compare.sh sim-results/variant-a/ sim-results/variant-b/

set -euo pipefail

DIR_A="${1:-}"
DIR_B="${2:-}"

if [[ -z "$DIR_A" || -z "$DIR_B" ]]; then
  echo "Usage: $0 <sim-results-dir-a> <sim-results-dir-b>" >&2
  exit 1
fi

SUMMARY_A="${DIR_A%/}/summary.txt"
SUMMARY_B="${DIR_B%/}/summary.txt"

for f in "$SUMMARY_A" "$SUMMARY_B"; do
  if [[ ! -f "$f" ]]; then
    echo "[ERROR] summary.txt not found: $f" >&2
    exit 1
  fi
done

# ── Parse a summary.txt into key=value pairs ────────────────────────────────
parse_summary() {
  local file="$1"
  python3 - "$file" <<'PYEOF'
import re, sys

kv = {}
with open(sys.argv[1]) as f:
    for line in f:
        line = line.strip()
        # e.g.  "  Clean:     242 (48.4%)"
        m = re.match(r'(Clean|Weathered|Breach):\s+\d+\s+\(([\d.]+)%\)', line)
        if m:
            kv[m.group(1).lower() + '_pct'] = float(m.group(2))
            continue
        # e.g.  "  Avg tides completed:  6"
        m = re.match(r'Avg tides completed:\s+([\d.]+)', line)
        if m: kv['avg_tides'] = float(m.group(1)); continue
        # e.g.  "  Avg final weave:      20 / 20"
        m = re.match(r'Avg final weave:\s+([\d.]+)', line)
        if m: kv['avg_weave'] = float(m.group(1)); continue
        # e.g.  "  Game overs (weave 0): 0"
        m = re.match(r'Game overs \(weave 0\):\s+(\d+)', line)
        if m: kv['game_overs'] = float(m.group(1)); continue
        # e.g.  "  Avg invaders killed:     22.75"
        m = re.match(r'Avg invaders killed:\s+([\d.]+)', line)
        if m: kv['avg_invaders_killed'] = float(m.group(1)); continue
        # e.g.  "  Avg peak corruption (single territory): 6.46"
        m = re.match(r'Avg peak corruption \(single territory\):\s+([\d.]+)', line)
        if m: kv['avg_peak_corruption'] = float(m.group(1)); continue
        # e.g.  "  Desecration events (L3 reached):        0 total"
        m = re.match(r'Desecration events \(L3 reached\):\s+(\d+)', line)
        if m: kv['desecration_events'] = float(m.group(1)); continue
        # e.g.  "  Avg total corruption at final tide:     23.63"
        m = re.match(r'Avg total corruption at final tide:\s+([\d.]+)', line)
        if m: kv['avg_total_corruption'] = float(m.group(1)); continue
        # e.g.  "  Avg total presence at final tide: 4.55"
        m = re.match(r'Avg total presence at final tide:\s+([\d.]+)', line)
        if m: kv['avg_presence'] = float(m.group(1)); continue
        # e.g.  "  Avg fear generated per encounter: 28.37"
        m = re.match(r'Avg fear generated per encounter:\s+([\d.]+)', line)
        if m: kv['avg_fear'] = float(m.group(1)); continue

for k, v in kv.items():
    print(f'{k}={v}')
PYEOF
}

# ── Load both summaries ──────────────────────────────────────────────────────
declare -A VA VB
while IFS='=' read -r k v; do VA["$k"]="$v"; done < <(parse_summary "$SUMMARY_A")
while IFS='=' read -r k v; do VB["$k"]="$v"; done < <(parse_summary "$SUMMARY_B")

# ── Name the directories ─────────────────────────────────────────────────────
NAME_A="$(basename "${DIR_A%/}")"
NAME_B="$(basename "${DIR_B%/}")"

echo "=== COMPARE SIM RESULTS ==="
printf "  A: %s\n" "$DIR_A"
printf "  B: %s\n" "$DIR_B"
echo ""

# ── Diff table ───────────────────────────────────────────────────────────────
METRIC_ORDER=(
  clean_pct weathered_pct breach_pct
  avg_tides avg_weave game_overs
  avg_invaders_killed
  avg_peak_corruption avg_total_corruption desecration_events
  avg_presence avg_fear
)

METRIC_LABELS=(
  "Clean %"           "Weathered %"        "Breach %"
  "Avg Tides"         "Avg Weave"          "Game Overs"
  "Avg Invaders Killed"
  "Avg Peak Corruption" "Avg Total Corruption" "Desecration Events"
  "Avg Presence"      "Avg Fear Generated"
)

# Metrics that are percentages (use absolute diff for threshold)
PCT_METRICS="clean_pct weathered_pct breach_pct"

printf "\n%-28s  %10s  %10s  %10s  %s\n" "METRIC" "$NAME_A" "$NAME_B" "DELTA" "NOTE"
echo "────────────────────────────────────────────────────────────────────────"

CHANGED=0

for (( idx=0; idx<${#METRIC_ORDER[@]}; idx++ )); do
  key="${METRIC_ORDER[$idx]}"
  label="${METRIC_LABELS[$idx]}"
  va="${VA[$key]:-N/A}"
  vb="${VB[$key]:-N/A}"

  if [[ "$va" == "N/A" || "$vb" == "N/A" ]]; then
    printf "%-28s  %10s  %10s  %10s\n" "$label" "$va" "$vb" "—"
    continue
  fi

  # Compute delta and flag in python3 (avoids bash float arithmetic)
  read -r delta note < <(python3 - "$va" "$vb" "$key" "$PCT_METRICS" <<'PYEOF'
import sys
a, b = float(sys.argv[1]), float(sys.argv[2])
key = sys.argv[3]
pct_keys = sys.argv[4].split()
delta = b - a

if key in pct_keys:
    # Absolute difference for percentages; flag if abs(delta) > 5pp
    threshold = 5.0
    flag = "*** CHANGED" if abs(delta) > threshold else ""
    print(f"{delta:+.1f}pp  {flag}")
else:
    # Relative change for counts/averages; flag if abs(delta/a) > 5%
    rel = (delta / a * 100) if a != 0 else 0
    threshold = 5.0
    flag = "*** CHANGED" if abs(rel) > threshold else ""
    print(f"{delta:+.2f} ({rel:+.1f}%)  {flag}")
PYEOF
)

  [[ "$note" == *"CHANGED"* ]] && (( CHANGED++ )) || true

  printf "%-28s  %10s  %10s  %10s  %s\n" \
    "$label" "$va" "$vb" "$delta" "$note"
done

echo "────────────────────────────────────────────────────────────────────────"
echo ""
if (( CHANGED > 0 )); then
  echo "  ${CHANGED} metric(s) changed by >5%  (marked ***)"
else
  echo "  No metrics changed by >5%."
fi
echo ""

# ── Per-tide comparison (if both have per-tide.csv) ─────────────────────────
TIDE_A="${DIR_A%/}/per-tide.csv"
TIDE_B="${DIR_B%/}/per-tide.csv"
if [[ -f "$TIDE_A" && -f "$TIDE_B" ]]; then
  echo "PER-TIDE DELTA (B - A):"
  python3 - "$TIDE_A" "$TIDE_B" <<'PYEOF'
import csv, collections, sys

def load(path):
    rows = collections.defaultdict(list)
    with open(path) as f:
        for row in csv.DictReader(f):
            rows[int(row['tide'])].append(row)
    return rows

a_tides = load(sys.argv[1])
b_tides = load(sys.argv[2])
all_tides = sorted(set(a_tides) | set(b_tides))

def avg(rows, col):
    vals = [float(r[col]) for r in rows if col in r]
    return sum(vals)/len(vals) if vals else 0

cols = ['weave','alive_invaders','total_presence','total_corruption']
header = f"  {'Tide':4}  " + "  ".join(f"{c:>18}" for c in cols)
print(header)
print("  " + "─" * (len(header)-2))
for t in all_tides:
    ar, br = a_tides.get(t,[]), b_tides.get(t,[])
    parts = [f"  {t:<4}"]
    for c in cols:
        av = avg(ar, c)
        bv = avg(br, c)
        d = bv - av
        marker = " *" if abs(d) > (0.05 * av if av else 0) else "  "
        parts.append(f"  {av:>7.1f}→{bv:>7.1f}({d:+.1f}){marker}")
    print("".join(parts))
PYEOF
  echo ""
fi
