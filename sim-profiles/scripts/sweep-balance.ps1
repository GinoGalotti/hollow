#Requires -Version 5.1
<#
.SYNOPSIS
    Sweep a single BalanceConfig property across an integer range.

.PARAMETER Warden
    Warden ID. Default: root

.PARAMETER Property
    snake_case BalanceConfig key (e.g. max_presence_per_territory). Default: max_presence_per_territory

.PARAMETER Min
    Inclusive range start. Default: 1

.PARAMETER Max
    Inclusive range end. Default: 5

.PARAMETER Runs
    Encounters per value. Default: 200

.PARAMETER SeedStart
    First seed. Default: 42

.EXAMPLE
    .\sweep-balance.ps1 root max_presence_per_territory 1 5 200 42
#>
param(
    [string]$Warden    = "root",
    [string]$Property  = "max_presence_per_territory",
    [int]   $Min       = 1,
    [int]   $Max       = 5,
    [int]   $Runs      = 200,
    [int]   $SeedStart = 42
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$SeedEnd   = $SeedStart + $Runs - 1
$Seeds     = "$SeedStart-$SeedEnd"
$RepoRoot  = (Resolve-Path (Join-Path $PSScriptRoot ".." "..")).Path
$SimProj   = Join-Path $RepoRoot "src" "HollowWardens.Sim"
$TmpDir    = Join-Path ([System.IO.Path]::GetTempPath()) "sweep-$Warden-$Property-$PID"
$ResultDir = Join-Path $RepoRoot "sim-results" "sweep-$Warden-$Property"

$stepCount = $Max - $Min + 1
Write-Host "=== SWEEP BALANCE — warden=$Warden property=$Property range=[$Min..$Max] runs=$Runs seeds=$Seeds ==="
Write-Host "Steps: $stepCount"
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

for ($v = $Min; $v -le $Max; $v++) {
    $outDir      = Join-Path $ResultDir "value_$v"
    $profileFile = Join-Path $TmpDir "profile_${v}.json"

    # Build balance_overrides with the property as a JSON number
    $balOverrides = [ordered]@{ $Property = $v }
    $profile = [ordered]@{
        name              = "sweep-$Property=$v"
        warden            = $Warden
        balance_overrides = $balOverrides
    } | ConvertTo-Json -Depth 5
    Set-Content -Path $profileFile -Value $profile -Encoding UTF8

    dotnet run --project $SimProj --no-build -- `
        --seeds $Seeds --warden $Warden `
        --profile $profileFile --output $outDir `
        2>$null | Out-Null

    $s = Parse-Summary (Join-Path $outDir "summary.txt")
    $rows.Add([PSCustomObject]@{
        Value     = $v
        Clean     = $s.Clean
        Weathered = $s.Weathered
        Breach    = $s.Breach
        AvgWeave  = $s.AvgWeave
    })

    $flag = ""
    if ($s.Breach -gt 10) { $flag += " [FLAG:BREACH>10%]" }
    if ($s.Clean  -lt 70) { $flag += " [FLAG:CLEAN<70%]"  }
    if ($flag) { $flags.Add("$Property=$v$flag") }

    Write-Host ("  {0}={1,-4}  clean={2,5:F1}%  breach={3,4:F1}%" -f $Property, $v, $s.Clean, $s.Breach)
}

# ── Table (value order) ──────────────────────────────────────────────────────
Write-Host ""
Write-Host ("─" * 62)
Write-Host ("  {0,-6}  {1,6}  {2,9}  {3,7}  {4,9}" -f "VALUE","CLEAN%","WEATHERED%","BREACH%","AVG_WEAVE")
Write-Host ("─" * 62)

foreach ($r in $rows) {
    $suffix = ""
    if ($r.Breach -gt 10) { $suffix += " *BREACH" }
    if ($r.Clean  -lt 70) { $suffix += " *CLEAN"  }
    Write-Host ("  {0,-6}  {1,5:F1}%  {2,8:F1}%  {3,6:F1}%  {4,9:F2}{5}" -f `
        $r.Value, $r.Clean, $r.Weathered, $r.Breach, $r.AvgWeave, $suffix)
}

Write-Host ("─" * 62)

if ($flags.Count -gt 0) {
    Write-Host ""
    Write-Host "FLAGGED VALUES:"
    $flags | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host ""
    Write-Host "No values flagged."
}

Write-Host ""
Write-Host "Results written to: $ResultDir"
Remove-Item -Recurse -Force $TmpDir -ErrorAction SilentlyContinue
