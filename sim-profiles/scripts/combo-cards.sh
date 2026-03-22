#!/usr/bin/env bash
# combo-cards.sh — test all pairs of draft cards for a given warden.
#
# Usage: ./combo-cards.sh <warden> <runs> <seed_start>
#   warden      — e.g. root or ember
#   runs        — encounters per combo (e.g. 50)
#   seed_start  — first seed; seeds = seed_start .. seed_start+runs-1
#
# Example: ./combo-cards.sh root 50 42
#
# Output: sorted table (clean% desc) + FLAGS for breach>10% or clean<70%.
# Profiles written to /tmp/combo-cards-<warden>/ and cleaned up after.

set -euo pipefail

WARDEN="${1:-root}"
RUNS="${2:-50}"
SEED_START="${3:-42}"
SEED_END=$(( SEED_START + RUNS - 1 ))
SEEDS="${SEED_START}-${SEED_END}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SIM_PROJECT="${REPO_ROOT}/src/HollowWardens.Sim"
DATA_FILE="${REPO_ROOT}/data/wardens/${WARDEN}.json"
TMP_DIR="${REPO_ROOT}/sim-results/.tmp-combo-cards-${WARDEN}-$$"
RESULTS_DIR="${REPO_ROOT}/sim-results/combo-cards-${WARDEN}"

if [[ ! -f "$DATA_FILE" ]]; then
  echo "[ERROR] Warden data not found: $DATA_FILE" >&2
  exit 1
fi

# ── Extract draft card IDs (starting: false) ────────────────────────────────
# Use python3 for reliable JSON parsing if available, else fall back to grep
if command -v python3 &>/dev/null; then
  mapfile -t DRAFT_CARDS < <(python3 - "$DATA_FILE" <<'PYEOF' | tr -d '\r'
import json, sys
data = json.load(open(sys.argv[1]))
for c in data.get("cards", []):
    if not c.get("starting", True):
        print(c["id"])
PYEOF
)
else
  # grep fallback: extract id lines after "starting": false blocks
  mapfile -t DRAFT_CARDS < <(
    python3 -c "
import json, sys
data = json.load(open('$DATA_FILE'))
for c in data.get('cards', []):
    if not c.get('starting', True):
        print(c['id'])
"
  )
fi

CARD_COUNT="${#DRAFT_CARDS[@]}"
if (( CARD_COUNT < 2 )); then
  echo "[ERROR] Need at least 2 draft cards; found ${CARD_COUNT}" >&2
  exit 1
fi

PAIR_COUNT=$(( CARD_COUNT * (CARD_COUNT - 1) / 2 ))
echo "=== COMBO CARDS — warden=${WARDEN} runs=${RUNS} seeds=${SEEDS} ==="
echo "Draft pool: ${CARD_COUNT} cards → ${PAIR_COUNT} pairs"
echo ""

mkdir -p "$TMP_DIR" "$RESULTS_DIR"

# ── Run each pair ────────────────────────────────────────────────────────────
declare -a ROWS=()   # "card1|card2|clean_pct|weathered_pct|breach_pct|avg_weave"
declare -a FLAGS=()

for (( i=0; i<CARD_COUNT-1; i++ )); do
  for (( j=i+1; j<CARD_COUNT; j++ )); do
    C1="${DRAFT_CARDS[$i]}"
    C2="${DRAFT_CARDS[$j]}"
    COMBO_KEY="${C1}__${C2}"
    OUT_DIR="${RESULTS_DIR}/${COMBO_KEY}"

    # Write a temporary profile JSON
    PROFILE_FILE="${TMP_DIR}/profile_${COMBO_KEY}.json"
    cat > "$PROFILE_FILE" <<PROFILE_JSON
{
  "name": "combo-${C1}+${C2}",
  "warden": "${WARDEN}",
  "warden_overrides": {
    "add_cards": ["${C1}", "${C2}"]
  }
}
PROFILE_JSON

    # Run the sim (suppress output; summary.txt is written to OUT_DIR)
    dotnet run --project "$SIM_PROJECT" --no-build -- \
      --seeds "$SEEDS" \
      --warden "$WARDEN" \
      --profile "$PROFILE_FILE" \
      --output "$OUT_DIR" \
      >/dev/null 2>&1 || true

    # Parse the summary.txt (more reliable than stdout filtering)
    SUMMARY="${OUT_DIR}/summary.txt"
    if [[ -f "$SUMMARY" ]]; then
      CLEAN_PCT=$(grep   'Clean:'     "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
      WTHR_PCT=$(grep    'Weathered:' "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
      BREACH_PCT=$(grep  'Breach:'    "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
      AVG_WEAVE=$(grep   'Avg final weave:' "$SUMMARY" | sed 's/.*weave:[[:space:]]*//;s/ .*//' | head -1)
    else
      CLEAN_PCT="0"; WTHR_PCT="0"; BREACH_PCT="0"; AVG_WEAVE="0"
    fi

    ROWS+=("${C1}|${C2}|${CLEAN_PCT:-0}|${WTHR_PCT:-0}|${BREACH_PCT:-0}|${AVG_WEAVE:-0}")

    # Flag bad combos
    BREACH_INT=$(printf "%.0f" "${BREACH_PCT:-0}")
    CLEAN_INT=$(printf  "%.0f" "${CLEAN_PCT:-0}")
    FLAG=""
    (( BREACH_INT > 10 )) && FLAG+=" [FLAG:BREACH>10%]"
    (( CLEAN_INT  < 70 )) && FLAG+=" [FLAG:CLEAN<70%]"
    [[ -n "$FLAG" ]] && FLAGS+=("${C1} + ${C2}${FLAG}")

    printf "  %-18s + %-18s  clean=%5s%%  breach=%4s%%\n" \
      "$C1" "$C2" "${CLEAN_PCT:-0}" "${BREACH_PCT:-0}"
  done
done

# ── Sort by clean% descending and print table ────────────────────────────────
echo ""
echo "─────────────────────────────────────────────────────────────────────────"
printf "%-18s  %-18s  %6s  %9s  %7s  %9s\n" \
  "CARD 1" "CARD 2" "CLEAN%" "WEATHERED%" "BREACH%" "AVG_WEAVE"
echo "─────────────────────────────────────────────────────────────────────────"

# Sort rows by clean_pct (field 3) descending using process substitution
IFS=$'\n' sorted=($(
  for row in "${ROWS[@]}"; do echo "$row"; done \
  | sort -t'|' -k3 -rn
))
unset IFS

for row in "${sorted[@]}"; do
  IFS='|' read -r c1 c2 clean wthr breach weave <<< "$row"
  SUFFIX=""
  B_INT=$(printf "%.0f" "$breach")
  C_INT=$(printf "%.0f" "$clean")
  (( B_INT > 10 )) && SUFFIX+=" ⚑BREACH"
  (( C_INT  < 70 )) && SUFFIX+=" ⚑CLEAN"
  printf "%-18s  %-18s  %5s%%  %8s%%  %6s%%  %9s%s\n" \
    "$c1" "$c2" "$clean" "$wthr" "$breach" "$weave" "$SUFFIX"
done

echo "─────────────────────────────────────────────────────────────────────────"

if (( ${#FLAGS[@]} > 0 )); then
  echo ""
  echo "FLAGGED COMBOS:"
  for f in "${FLAGS[@]}"; do echo "  $f"; done
else
  echo ""
  echo "No combos flagged."
fi

echo ""
echo "Results written to: ${RESULTS_DIR}/"
rm -rf "$TMP_DIR"
