#Requires -Version 5.1
<#
.SYNOPSIS
    Test all pairs of draft cards for a given warden.

.DESCRIPTION
    Iterates through all pairs of draft (non-starting) cards, runs a sim for
    each combo, and prints a sorted table of results. Flags any combo where
    breach% > 10 or clean% < 70.

.PARAMETER Warden
    Warden ID (e.g. root, ember). Default: root

.PARAMETER Runs
    Number of encounters per combo. Default: 50

.PARAMETER SeedStart
    First seed. Seeds = SeedStart .. SeedStart+Runs-1. Default: 42

.EXAMPLE
    .\combo-cards.ps1 root 50 42
#>
param(
    [string]$Warden     = "root",
    [int]   $Runs       = 50,
    [int]   $SeedStart  = 42
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SeedEnd   = $SeedStart + $Runs - 1
$Seeds     = "$SeedStart-$SeedEnd"
$RepoRoot  = (Resolve-Path (Join-Path $PSScriptRoot ".." "..")).Path
$SimProj   = Join-Path $RepoRoot "src" "HollowWardens.Sim"
$DataFile  = Join-Path $RepoRoot "data" "wardens" "$Warden.json"
$TmpDir    = Join-Path ([System.IO.Path]::GetTempPath()) "combo-cards-$Warden-$PID"
$ResultDir = Join-Path $RepoRoot "sim-results" "combo-cards-$Warden"

if (-not (Test-Path $DataFile)) {
    Write-Error "Warden data not found: $DataFile"
    exit 1
}

# ── Extract draft card IDs ───────────────────────────────────────────────────
$wardenJson = Get-Content $DataFile -Raw | ConvertFrom-Json
$draftCards = @($wardenJson.cards | Where-Object { -not $_.starting } | Select-Object -ExpandProperty id)
$cardCount  = $draftCards.Count

if ($cardCount -lt 2) {
    Write-Error "Need at least 2 draft cards; found $cardCount"
    exit 1
}

$pairCount = $cardCount * ($cardCount - 1) / 2

Write-Host "=== COMBO CARDS — warden=$Warden runs=$Runs seeds=$Seeds ==="
Write-Host "Draft pool: $cardCount cards → $pairCount pairs"
Write-Host ""

New-Item -ItemType Directory -Force -Path $TmpDir  | Out-Null
New-Item -ItemType Directory -Force -Path $ResultDir | Out-Null

$rows  = [System.Collections.Generic.List[PSCustomObject]]::new()
$flags = [System.Collections.Generic.List[string]]::new()

function Parse-Summary([string]$path) {
    $result = @{ Clean = 0.0; Weathered = 0.0; Breach = 0.0; AvgWeave = 0.0 }
    if (-not (Test-Path $path)) { return $result }
    foreach ($line in Get-Content $path) {
        if ($line -match 'Clean:\s+\d+\s+\(([\d.]+)%\)')     { $result.Clean     = [double]$Matches[1] }
        if ($line -match 'Weathered:\s+\d+\s+\(([\d.]+)%\)') { $result.Weathered = [double]$Matches[1] }
        if ($line -match 'Breach:\s+\d+\s+\(([\d.]+)%\)')    { $result.Breach    = [double]$Matches[1] }
        if ($line -match 'Avg final weave:\s+([\d.]+)')       { $result.AvgWeave  = [double]$Matches[1] }
    }
    return $result
}

for ($i = 0; $i -lt $cardCount - 1; $i++) {
    for ($j = $i + 1; $j -lt $cardCount; $j++) {
        $c1  = $draftCards[$i]
        $c2  = $draftCards[$j]
        $key = "${c1}__${c2}"
        $outDir      = Join-Path $ResultDir $key
        $profileFile = Join-Path $TmpDir "profile_${key}.json"

        $profile = [ordered]@{
            name   = "combo-$c1+$c2"
            warden = $Warden
            warden_overrides = [ordered]@{
                add_cards = @($c1, $c2)
            }
        } | ConvertTo-Json -Depth 5
        Set-Content -Path $profileFile -Value $profile -Encoding UTF8

        dotnet run --project $SimProj --no-build -- `
            --seeds $Seeds --warden $Warden `
            --profile $profileFile --output $outDir `
            2>$null | Out-Null

        $s = Parse-Summary (Join-Path $outDir "summary.txt")
        $rows.Add([PSCustomObject]@{
            Card1     = $c1
            Card2     = $c2
            Clean     = $s.Clean
            Weathered = $s.Weathered
            Breach    = $s.Breach
            AvgWeave  = $s.AvgWeave
        })

        $flag = ""
        if ($s.Breach -gt 10) { $flag += " [FLAG:BREACH>10%]" }
        if ($s.Clean  -lt 70) { $flag += " [FLAG:CLEAN<70%]"  }
        if ($flag) { $flags.Add("$c1 + $c2$flag") }

        Write-Host ("  {0,-18} + {1,-18}  clean={2,5:F1}%  breach={3,4:F1}%" -f $c1, $c2, $s.Clean, $s.Breach)
    }
}

# ── Sorted table ─────────────────────────────────────────────────────────────
$sorted = $rows | Sort-Object Clean -Descending

Write-Host ""
Write-Host ("─" * 78)
Write-Host ("{0,-18}  {1,-18}  {2,6}  {3,9}  {4,7}  {5,9}" -f "CARD 1","CARD 2","CLEAN%","WEATHERED%","BREACH%","AVG_WEAVE")
Write-Host ("─" * 78)

foreach ($r in $sorted) {
    $suffix = ""
    if ($r.Breach -gt 10) { $suffix += " *BREACH" }
    if ($r.Clean  -lt 70) { $suffix += " *CLEAN"  }
    Write-Host ("{0,-18}  {1,-18}  {2,5:F1}%  {3,8:F1}%  {4,6:F1}%  {5,9:F2}{6}" -f `
        $r.Card1, $r.Card2, $r.Clean, $r.Weathered, $r.Breach, $r.AvgWeave, $suffix)
}

Write-Host ("─" * 78)

if ($flags.Count -gt 0) {
    Write-Host ""
    Write-Host "FLAGGED COMBOS:"
    $flags | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host ""
    Write-Host "No combos flagged."
}

Write-Host ""
Write-Host "Results written to: $ResultDir"
Remove-Item -Recurse -Force $TmpDir -ErrorAction SilentlyContinue
