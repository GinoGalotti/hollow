#!/usr/bin/env bash
# combo-passives.sh — test all combinations of 1 or 2 passives force-unlocked at start.
#
# Usage: ./combo-passives.sh <warden> <runs> <seed_start>
#   warden      — e.g. root or ember
#   runs        — encounters per combo
#   seed_start  — first seed
#
# Example: ./combo-passives.sh root 100 42
#
# Tests all single passives + all pairs, sorted by clean% desc.
# Flags breach>10% or clean<70%.

set -euo pipefail

WARDEN="${1:-root}"
RUNS="${2:-100}"
SEED_START="${3:-42}"
SEED_END=$(( SEED_START + RUNS - 1 ))
SEEDS="${SEED_START}-${SEED_END}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SIM_PROJECT="${REPO_ROOT}/src/HollowWardens.Sim"
DATA_FILE="${REPO_ROOT}/data/wardens/${WARDEN}.json"
TMP_DIR="${REPO_ROOT}/sim-results/.tmp-combo-passives-${WARDEN}-$$"
RESULTS_DIR="${REPO_ROOT}/sim-results/combo-passives-${WARDEN}"

if [[ ! -f "$DATA_FILE" ]]; then
  echo "[ERROR] Warden data not found: $DATA_FILE" >&2
  exit 1
fi

# ── Extract all passive IDs ──────────────────────────────────────────────────
mapfile -t ALL_PASSIVES < <(python3 - "$DATA_FILE" <<'PYEOF' | tr -d '\r'
import json, sys
data = json.load(open(sys.argv[1]))
for p in data.get("passives", []):
    print(p["id"])
PYEOF
)

PASSIVE_COUNT="${#ALL_PASSIVES[@]}"
SINGLE_COUNT=$PASSIVE_COUNT
PAIR_COUNT=$(( PASSIVE_COUNT * (PASSIVE_COUNT - 1) / 2 ))
TOTAL=$(( SINGLE_COUNT + PAIR_COUNT ))

echo "=== COMBO PASSIVES — warden=${WARDEN} runs=${RUNS} seeds=${SEEDS} ==="
echo "Passive pool: ${PASSIVE_COUNT} passives → ${SINGLE_COUNT} singles + ${PAIR_COUNT} pairs = ${TOTAL} combos"
echo ""

mkdir -p "$TMP_DIR" "$RESULTS_DIR"

declare -a ROWS
declare -a FLAGS

run_combo() {
  local label="$1"
  local key="$2"
  local force_json="$3"   # JSON array string, e.g. ["foo","bar"]
  local OUT_DIR="${RESULTS_DIR}/${key}"

  local PROFILE_FILE="${TMP_DIR}/profile_${key}.json"
  cat > "$PROFILE_FILE" <<PROFILE_JSON
{
  "name": "passives-${label}",
  "warden": "${WARDEN}",
  "warden_overrides": {
    "force_passives": ${force_json}
  }
}
PROFILE_JSON

  dotnet run --project "$SIM_PROJECT" --no-build -- \
    --seeds "$SEEDS" \
    --warden "$WARDEN" \
    --profile "$PROFILE_FILE" \
    --output "$OUT_DIR" \
    2>/dev/null >/dev/null || true

  local SUMMARY="${OUT_DIR}/summary.txt"
  local CLEAN_PCT WTHR_PCT BREACH_PCT AVG_WEAVE
  if [[ -f "$SUMMARY" ]]; then
    CLEAN_PCT=$(grep  'Clean:'     "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
    WTHR_PCT=$(grep   'Weathered:' "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
    BREACH_PCT=$(grep 'Breach:'    "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
    AVG_WEAVE=$(grep  'Avg final weave:' "$SUMMARY" | sed 's/.*weave:[[:space:]]*//;s/ .*//' | head -1)
  fi
  CLEAN_PCT="${CLEAN_PCT:-0}"; WTHR_PCT="${WTHR_PCT:-0}"
  BREACH_PCT="${BREACH_PCT:-0}"; AVG_WEAVE="${AVG_WEAVE:-0}"

  ROWS+=("${label}|${CLEAN_PCT}|${WTHR_PCT}|${BREACH_PCT}|${AVG_WEAVE}")

  local B_INT C_INT FLAG=""
  B_INT=$(printf "%.0f" "$BREACH_PCT")
  C_INT=$(printf "%.0f" "$CLEAN_PCT")
  (( B_INT > 10 )) && FLAG+=" [FLAG:BREACH>10%]"
  (( C_INT  < 70 )) && FLAG+=" [FLAG:CLEAN<70%]"
  [[ -n "$FLAG" ]] && FLAGS+=("${label}${FLAG}")

  printf "  %-40s  clean=%5s%%  breach=%4s%%\n" "$label" "$CLEAN_PCT" "$BREACH_PCT"
}

# ── Singles ──────────────────────────────────────────────────────────────────
echo "── Singles ──"
for p in "${ALL_PASSIVES[@]}"; do
  run_combo "$p" "single_${p}" "[\"${p}\"]"
done

# ── Pairs ────────────────────────────────────────────────────────────────────
echo ""
echo "── Pairs ──"
for (( i=0; i<PASSIVE_COUNT-1; i++ )); do
  for (( j=i+1; j<PASSIVE_COUNT; j++ )); do
    P1="${ALL_PASSIVES[$i]}"
    P2="${ALL_PASSIVES[$j]}"
    run_combo "${P1}+${P2}" "pair_${P1}__${P2}" "[\"${P1}\",\"${P2}\"]"
  done
done

# ── Sorted table ─────────────────────────────────────────────────────────────
echo ""
echo "─────────────────────────────────────────────────────────────────────────────────"
printf "%-40s  %6s  %9s  %7s  %9s\n" "PASSIVE COMBO" "CLEAN%" "WEATHERED%" "BREACH%" "AVG_WEAVE"
echo "─────────────────────────────────────────────────────────────────────────────────"

IFS=$'\n' sorted=($(
  for row in "${ROWS[@]}"; do echo "$row"; done \
  | sort -t'|' -k2 -rn
))
unset IFS

for row in "${sorted[@]}"; do
  IFS='|' read -r label clean wthr breach weave <<< "$row"
  SUFFIX=""
  B_INT=$(printf "%.0f" "$breach")
  C_INT=$(printf "%.0f" "$clean")
  (( B_INT > 10 )) && SUFFIX+=" ⚑BREACH"
  (( C_INT  < 70 )) && SUFFIX+=" ⚑CLEAN"
  printf "%-40s  %5s%%  %8s%%  %6s%%  %9s%s\n" \
    "$label" "$clean" "$wthr" "$breach" "$weave" "$SUFFIX"
done

echo "─────────────────────────────────────────────────────────────────────────────────"

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
