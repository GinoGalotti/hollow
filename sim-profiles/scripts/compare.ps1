#Requires -Version 5.1
<#
.SYNOPSIS
    Side-by-side diff of two sim-results directories.
    Highlights metrics changed by >5%.

.PARAMETER DirA
    Path to the first sim-results directory (baseline).

.PARAMETER DirB
    Path to the second sim-results directory (variant).

.EXAMPLE
    .\compare.ps1 sim-results\variant-a\ sim-results\variant-b\
#>
param(
    [Parameter(Mandatory)][string]$DirA,
    [Parameter(Mandatory)][string]$DirB
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SummaryA = Join-Path $DirA "summary.txt"
$SummaryB = Join-Path $DirB "summary.txt"

foreach ($f in @($SummaryA, $SummaryB)) {
    if (-not (Test-Path $f)) {
        Write-Error "summary.txt not found: $f"
        exit 1
    }
}

# ── Parse summary.txt ────────────────────────────────────────────────────────
function Parse-Summary([string]$path) {
    $kv = @{}
    foreach ($line in Get-Content $path) {
        $line = $line.Trim()
        if ($line -match '^Clean:\s+\d+\s+\(([\d.]+)%\)')                         { $kv['clean_pct']            = [double]$Matches[1] }
        elseif ($line -match '^Weathered:\s+\d+\s+\(([\d.]+)%\)')                 { $kv['weathered_pct']        = [double]$Matches[1] }
        elseif ($line -match '^Breach:\s+\d+\s+\(([\d.]+)%\)')                    { $kv['breach_pct']           = [double]$Matches[1] }
        elseif ($line -match 'Avg tides completed:\s+([\d.]+)')                    { $kv['avg_tides']            = [double]$Matches[1] }
        elseif ($line -match 'Avg final weave:\s+([\d.]+)')                        { $kv['avg_weave']            = [double]$Matches[1] }
        elseif ($line -match 'Game overs \(weave 0\):\s+(\d+)')                    { $kv['game_overs']           = [double]$Matches[1] }
        elseif ($line -match 'Avg invaders killed:\s+([\d.]+)')                    { $kv['avg_invaders_killed']  = [double]$Matches[1] }
        elseif ($line -match 'Avg peak corruption \(single territory\):\s+([\d.]+)') { $kv['avg_peak_corruption'] = [double]$Matches[1] }
        elseif ($line -match 'Desecration events \(L3 reached\):\s+(\d+)')         { $kv['desecration_events']  = [double]$Matches[1] }
        elseif ($line -match 'Avg total corruption at final tide:\s+([\d.]+)')     { $kv['avg_total_corruption'] = [double]$Matches[1] }
        elseif ($line -match 'Avg total presence at final tide:\s+([\d.]+)')       { $kv['avg_presence']         = [double]$Matches[1] }
        elseif ($line -match 'Avg fear generated per encounter:\s+([\d.]+)')       { $kv['avg_fear']             = [double]$Matches[1] }
    }
    return $kv
}

$VA = Parse-Summary $SummaryA
$VB = Parse-Summary $SummaryB

$NameA = Split-Path $DirA -Leaf
$NameB = Split-Path $DirB -Leaf

Write-Host "=== COMPARE SIM RESULTS ==="
Write-Host "  A: $DirA"
Write-Host "  B: $DirB"
Write-Host ""

# ── Metrics table ────────────────────────────────────────────────────────────
$metricOrder = @(
    @{ Key = 'clean_pct';            Label = 'Clean %';             IsPct = $true  }
    @{ Key = 'weathered_pct';        Label = 'Weathered %';         IsPct = $true  }
    @{ Key = 'breach_pct';           Label = 'Breach %';            IsPct = $true  }
    @{ Key = 'avg_tides';            Label = 'Avg Tides';           IsPct = $false }
    @{ Key = 'avg_weave';            Label = 'Avg Weave';           IsPct = $false }
    @{ Key = 'game_overs';           Label = 'Game Overs';          IsPct = $false }
    @{ Key = 'avg_invaders_killed';  Label = 'Avg Invaders Killed'; IsPct = $false }
    @{ Key = 'avg_peak_corruption';  Label = 'Avg Peak Corruption'; IsPct = $false }
    @{ Key = 'avg_total_corruption'; Label = 'Avg Total Corruption';IsPct = $false }
    @{ Key = 'desecration_events';   Label = 'Desecration Events';  IsPct = $false }
    @{ Key = 'avg_presence';         Label = 'Avg Presence';        IsPct = $false }
    @{ Key = 'avg_fear';             Label = 'Avg Fear Generated';  IsPct = $false }
)

Write-Host ("{0,-28}  {1,10}  {2,10}  {3,12}  {4}" -f "METRIC", $NameA, $NameB, "DELTA", "NOTE")
Write-Host ("─" * 76)

$changedCount = 0

foreach ($m in $metricOrder) {
    $key   = $m.Key
    $label = $m.Label
    $isPct = $m.IsPct

    $va = if ($VA.ContainsKey($key)) { $VA[$key] } else { $null }
    $vb = if ($VB.ContainsKey($key)) { $VB[$key] } else { $null }

    if ($null -eq $va -or $null -eq $vb) {
        Write-Host ("{0,-28}  {1,10}  {2,10}  {3,12}" -f $label, "N/A", "N/A", "—")
        continue
    }

    $delta = $vb - $va
    $note  = ""

    if ($isPct) {
        $deltaStr = "{0:+0.0;-0.0}pp" -f $delta
        if ([Math]::Abs($delta) -gt 5.0) {
            $note = "*** CHANGED"
            $changedCount++
        }
    } else {
        $deltaStr = "{0:+0.00;-0.00}" -f $delta
        $rel = if ($va -ne 0) { $delta / $va * 100 } else { 0 }
        $deltaStr += " ({0:+0.1f;-0.1f}%)" -f $rel
        if ([Math]::Abs($rel) -gt 5.0) {
            $note = "*** CHANGED"
            $changedCount++
        }
    }

    $color = if ($note) { "Yellow" } else { "White" }
    Write-Host ("{0,-28}  {1,10}  {2,10}  {3,12}  {4}" -f $label, $va, $vb, $deltaStr, $note) -ForegroundColor $color
}

Write-Host ("─" * 76)
Write-Host ""
if ($changedCount -gt 0) {
    Write-Host "  $changedCount metric(s) changed by >5%  (marked ***)" -ForegroundColor Yellow
} else {
    Write-Host "  No metrics changed by >5%."
}
Write-Host ""

# ── Per-tide comparison ───────────────────────────────────────────────────────
$TideA = Join-Path $DirA "per-tide.csv"
$TideB = Join-Path $DirB "per-tide.csv"
if ((Test-Path $TideA) -and (Test-Path $TideB)) {
    Write-Host "PER-TIDE DELTA (B - A):"

    function Load-Tides([string]$path) {
        $tides = @{}
        Import-Csv $path | ForEach-Object {
            $t = [int]$_.tide
            if (-not $tides.ContainsKey($t)) { $tides[$t] = @() }
            $tides[$t] += $_
        }
        return $tides
    }

    function Tide-Avg($rows, [string]$col) {
        if ($rows.Count -eq 0) { return 0.0 }
        ($rows | ForEach-Object { [double]($_.$col) } | Measure-Object -Average).Average
    }

    $tidesA = Load-Tides $TideA
    $tidesB = Load-Tides $TideB
    $allTides = (@($tidesA.Keys) + @($tidesB.Keys) | Sort-Object -Unique)

    $cols = @('weave','alive_invaders','total_presence','total_corruption')
    Write-Host ("  {0,-4}  {1}" -f "Tide", ($cols -join "  "))

    foreach ($t in $allTides) {
        $arA = if ($tidesA.ContainsKey($t)) { $tidesA[$t] } else { @() }
        $arB = if ($tidesB.ContainsKey($t)) { $tidesB[$t] } else { @() }
        $parts = "  {0,-4}" -f $t
        foreach ($c in $cols) {
            $av = Tide-Avg $arA $c
            $bv = Tide-Avg $arB $c
            $d  = $bv - $av
            $marker = if ($av -ne 0 -and [Math]::Abs($d / $av) -gt 0.05) { "*" } else { " " }
            $parts += "  {0,6:F1}→{1,6:F1}({2:+0.1f;-0.1f}){3}" -f $av, $bv, $d, $marker
        }
        Write-Host $parts
    }
    Write-Host ""
}
