#Requires -Version 5.1
<#
.SYNOPSIS
    Test all combinations of 1-2 passives force-unlocked at encounter start.

.PARAMETER Warden
    Warden ID (e.g. root, ember). Default: root

.PARAMETER Runs
    Encounters per combo. Default: 100

.PARAMETER SeedStart
    First seed. Default: 42

.EXAMPLE
    .\combo-passives.ps1 root 100 42
#>
param(
    [string]$Warden    = "root",
    [int]   $Runs      = 100,
    [int]   $SeedStart = 42
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SeedEnd   = $SeedStart + $Runs - 1
$Seeds     = "$SeedStart-$SeedEnd"
$RepoRoot  = (Resolve-Path (Join-Path $PSScriptRoot ".." "..")).Path
$SimProj   = Join-Path $RepoRoot "src" "HollowWardens.Sim"
$DataFile  = Join-Path $RepoRoot "data" "wardens" "$Warden.json"
$TmpDir    = Join-Path ([System.IO.Path]::GetTempPath()) "combo-passives-$Warden-$PID"
$ResultDir = Join-Path $RepoRoot "sim-results" "combo-passives-$Warden"

if (-not (Test-Path $DataFile)) {
    Write-Error "Warden data not found: $DataFile"
    exit 1
}

$wardenJson  = Get-Content $DataFile -Raw | ConvertFrom-Json
$allPassives = @($wardenJson.passives | Select-Object -ExpandProperty id)
$pCount      = $allPassives.Count
$pairCount   = $pCount * ($pCount - 1) / 2
$total       = $pCount + $pairCount

Write-Host "=== COMBO PASSIVES — warden=$Warden runs=$Runs seeds=$Seeds ==="
Write-Host "Passive pool: $pCount passives → $pCount singles + $pairCount pairs = $total combos"
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

function Run-Combo([string]$label, [string]$key, [string[]]$passiveList) {
    $outDir      = Join-Path $ResultDir $key
    $profileFile = Join-Path $TmpDir "profile_${key}.json"

    $passiveArr = $passiveList | ConvertTo-Json -Compress
    $profile = [ordered]@{
        name   = "passives-$label"
        warden = $Warden
        warden_overrides = [ordered]@{
            force_passives = $passiveList
        }
    } | ConvertTo-Json -Depth 5
    Set-Content -Path $profileFile -Value $profile -Encoding UTF8

    dotnet run --project $SimProj --no-build -- `
        --seeds $Seeds --warden $Warden `
        --profile $profileFile --output $outDir `
        2>$null | Out-Null

    $s = Parse-Summary (Join-Path $outDir "summary.txt")
    $rows.Add([PSCustomObject]@{
        Label     = $label
        Clean     = $s.Clean
        Weathered = $s.Weathered
        Breach    = $s.Breach
        AvgWeave  = $s.AvgWeave
    })

    $flag = ""
    if ($s.Breach -gt 10) { $flag += " [FLAG:BREACH>10%]" }
    if ($s.Clean  -lt 70) { $flag += " [FLAG:CLEAN<70%]"  }
    if ($flag) { $script:flags.Add("$label$flag") }

    Write-Host ("  {0,-40}  clean={1,5:F1}%  breach={2,4:F1}%" -f $label, $s.Clean, $s.Breach)
}

Write-Host "── Singles ──"
foreach ($p in $allPassives) {
    Run-Combo $p "single_$p" @($p)
}

Write-Host ""
Write-Host "── Pairs ──"
for ($i = 0; $i -lt $pCount - 1; $i++) {
    for ($j = $i + 1; $j -lt $pCount; $j++) {
        $p1 = $allPassives[$i]; $p2 = $allPassives[$j]
        Run-Combo "$p1+$p2" "pair_${p1}__${p2}" @($p1, $p2)
    }
}

# ── Sorted table ─────────────────────────────────────────────────────────────
$sorted = $rows | Sort-Object Clean -Descending

Write-Host ""
Write-Host ("─" * 82)
Write-Host ("{0,-40}  {1,6}  {2,9}  {3,7}  {4,9}" -f "PASSIVE COMBO","CLEAN%","WEATHERED%","BREACH%","AVG_WEAVE")
Write-Host ("─" * 82)

foreach ($r in $sorted) {
    $suffix = ""
    if ($r.Breach -gt 10) { $suffix += " *BREACH" }
    if ($r.Clean  -lt 70) { $suffix += " *CLEAN"  }
    Write-Host ("{0,-40}  {1,5:F1}%  {2,8:F1}%  {3,6:F1}%  {4,9:F2}{5}" -f `
        $r.Label, $r.Clean, $r.Weathered, $r.Breach, $r.AvgWeave, $suffix)
}

Write-Host ("─" * 82)

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
