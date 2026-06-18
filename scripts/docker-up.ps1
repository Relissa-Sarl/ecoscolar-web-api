<#
.SYNOPSIS
  Demarre la stack Docker EcoScolar API avec override 1433:1433 garanti.

.EXAMPLE
  .\scripts\docker-up.ps1

.EXAMPLE
  .\scripts\docker-up.ps1 -Build
#>

[CmdletBinding()]
param(
  [string]$ProjectDirectory,
  [switch]$Build,
  [switch]$ForceOverride
)

$ErrorActionPreference = 'Stop'

if (-not $ProjectDirectory) {
  $ProjectDirectory = Split-Path -Parent $PSScriptRoot
  if (-not $ProjectDirectory) {
    $ProjectDirectory = (Get-Location).Path
  }
}
$ProjectDirectory = (Resolve-Path $ProjectDirectory).Path

& (Join-Path $PSScriptRoot 'ensure-docker-override.ps1') -ProjectDirectory $ProjectDirectory -Force:$ForceOverride

$portInUse = Get-NetTCPConnection -LocalPort 1433 -State Listen -ErrorAction SilentlyContinue
if ($portInUse) {
  $owner = Get-Process -Id $portInUse.OwningProcess -ErrorAction SilentlyContinue
  $ownerName = if ($owner) { $owner.ProcessName } else { "PID $($portInUse.OwningProcess)" }
  Write-Warning "Le port 1433 est deja utilise par : $ownerName"
  Write-Warning "Arrete SQL Server local ou libere le port, sinon Docker ne pourra pas binder 1433:1433."
}

Push-Location $ProjectDirectory
try {
  $args = @('compose', 'up', '-d')
  if ($Build) { $args += '--build' }
  & docker @args
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

  Write-Host ""
  Write-Host "API gateway : http://localhost:8080"
  Write-Host "SQL Server  : localhost,1433 (sa / voir .env)"
}
finally {
  Pop-Location
}
