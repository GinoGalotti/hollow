#!/usr/bin/env bash
# sweep-balance.sh — sweep a single BalanceConfig property across a range.
#
# Usage: ./sweep-balance.sh <warden> <property> <min> <max> <runs> <seed_start>
#   warden      — e.g. root
#   property    — snake_case BalanceConfig key, e.g. max_presence_per_territory
#   min         — inclusive range start (integer)
#   max         — inclusive range end   (integer)
#   runs        — encounters per value  (e.g. 200)
#   seed_start  — first seed
#
# Example: ./sweep-balance.sh root max_presence_per_territory 1 5 200 42
#
# Output: sorted table by value + FLAGS for breach>10% or clean<70%.

set -euo pipefail

WARDEN="${1:-root}"
PROPERTY="${2:-max_presence_per_territory}"
RANGE_MIN="${3:-1}"
RANGE_MAX="${4:-5}"
RUNS="${5:-200}"
SEED_START="${6:-42}"
SEED_END=$(( SEED_START + RUNS - 1 ))
SEEDS="${SEED_START}-${SEED_END}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SIM_PROJECT="${REPO_ROOT}/src/HollowWardens.Sim"
TMP_DIR="${REPO_ROOT}/sim-results/.tmp-sweep-${WARDEN}-${PROPERTY}-$$"
RESULTS_DIR="${REPO_ROOT}/sim-results/sweep-${WARDEN}-${PROPERTY}"

STEP_COUNT=$(( RANGE_MAX - RANGE_MIN + 1 ))
echo "=== SWEEP BALANCE — warden=${WARDEN} property=${PROPERTY} range=[${RANGE_MIN}..${RANGE_MAX}] runs=${RUNS} seeds=${SEEDS} ==="
echo "Steps: ${STEP_COUNT}"
echo ""

mkdir -p "$TMP_DIR" "$RESULTS_DIR"

declare -a ROWS
declare -a FLAGS

for (( v=RANGE_MIN; v<=RANGE_MAX; v++ )); do
  OUT_DIR="${RESULTS_DIR}/value_${v}"
  PROFILE_FILE="${TMP_DIR}/profile_${v}.json"

  cat > "$PROFILE_FILE" <<PROFILE_JSON
{
  "name": "sweep-${PROPERTY}=${v}",
  "warden": "${WARDEN}",
  "balance_overrides": {
    "${PROPERTY}": ${v}
  }
}
PROFILE_JSON

  dotnet run --project "$SIM_PROJECT" --no-build -- \
    --seeds "$SEEDS" \
    --warden "$WARDEN" \
    --profile "$PROFILE_FILE" \
    --output "$OUT_DIR" \
    2>/dev/null >/dev/null || true

  SUMMARY="${OUT_DIR}/summary.txt"
  CLEAN_PCT="0"; WTHR_PCT="0"; BREACH_PCT="0"; AVG_WEAVE="0"
  if [[ -f "$SUMMARY" ]]; then
    CLEAN_PCT=$(grep  'Clean:'     "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
    WTHR_PCT=$(grep   'Weathered:' "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
    BREACH_PCT=$(grep 'Breach:'    "$SUMMARY" | sed 's/.*(//;s/%.*//' | head -1)
    AVG_WEAVE=$(grep  'Avg final weave:' "$SUMMARY" | sed 's/.*weave:[[:space:]]*//;s/ .*//' | head -1)
    CLEAN_PCT="${CLEAN_PCT:-0}"; WTHR_PCT="${WTHR_PCT:-0}"
    BREACH_PCT="${BREACH_PCT:-0}"; AVG_WEAVE="${AVG_WEAVE:-0}"
  fi

  ROWS+=("${v}|${CLEAN_PCT}|${WTHR_PCT}|${BREACH_PCT}|${AVG_WEAVE}")

  B_INT=$(printf "%.0f" "$BREACH_PCT")
  C_INT=$(printf "%.0f" "$CLEAN_PCT")
  FLAG=""
  (( B_INT > 10 )) && FLAG+=" [FLAG:BREACH>10%]"
  (( C_INT  < 70 )) && FLAG+=" [FLAG:CLEAN<70%]"
  [[ -n "$FLAG" ]] && FLAGS+=("${PROPERTY}=${v}${FLAG}")

  printf "  %s=%-4s  clean=%5s%%  breach=%4s%%\n" \
    "$PROPERTY" "$v" "$CLEAN_PCT" "$BREACH_PCT"
done

# ── Table (sorted by value ascending) ───────────────────────────────────────
echo ""
echo "──────────────────────────────────────────────────────────────"
printf "  %-6s  %6s  %9s  %7s  %9s\n" "VALUE" "CLEAN%" "WEATHERED%" "BREACH%" "AVG_WEAVE"
echo "──────────────────────────────────────────────────────────────"

for row in "${ROWS[@]}"; do
  IFS='|' read -r val clean wthr breach weave <<< "$row"
  SUFFIX=""
  B_INT=$(printf "%.0f" "$breach")
  C_INT=$(printf "%.0f" "$clean")
  (( B_INT > 10 )) && SUFFIX+=" ⚑BREACH"
  (( C_INT  < 70 )) && SUFFIX+=" ⚑CLEAN"
  printf "  %-6s  %5s%%  %8s%%  %6s%%  %9s%s\n" \
    "$val" "$clean" "$wthr" "$breach" "$weave" "$SUFFIX"
done

echo "──────────────────────────────────────────────────────────────"

if (( ${#FLAGS[@]} > 0 )); then
  echo ""
  echo "FLAGGED VALUES:"
  for f in "${FLAGS[@]}"; do echo "  $f"; done
else
  echo ""
  echo "No values flagged."
fi

echo ""
echo "Results written to: ${RESULTS_DIR}/"
rm -rf "$TMP_DIR"
