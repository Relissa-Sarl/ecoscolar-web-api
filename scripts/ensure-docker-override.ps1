<#
.SYNOPSIS
  Garantit que docker-compose.override.yml expose SQL Server sur 1433:1433.

.DESCRIPTION
  Fichier local (gitignore). Copie depuis docker-compose.override.example.yml
  si absent, ou re-synchronise le contenu attendu avec -Force.

  N'ajoute jamais ASPNETCORE_ENVIRONMENT=Development sur l'API Docker :
  cela casse la connexion SQL interne au réseau compose.

.EXAMPLE
  .\scripts\ensure-docker-override.ps1

.EXAMPLE
  .\scripts\ensure-docker-override.ps1 -Force
#>

[CmdletBinding()]
param(
  [string]$ProjectDirectory,
  [switch]$Force
)

$ErrorActionPreference = 'Stop'

if (-not $ProjectDirectory) {
  $ProjectDirectory = Split-Path -Parent $PSScriptRoot
  if (-not $ProjectDirectory) {
    $ProjectDirectory = (Get-Location).Path
  }
}
$ProjectDirectory = (Resolve-Path $ProjectDirectory).Path

$examplePath = Join-Path $ProjectDirectory 'docker-compose.override.example.yml'
$overridePath = Join-Path $ProjectDirectory 'docker-compose.override.yml'

if (-not (Test-Path $examplePath)) {
  throw "Fichier modele introuvable : $examplePath"
}

$expectedContent = (Get-Content -Path $examplePath -Raw).TrimEnd() + "`n"

if (-not (Test-Path $overridePath)) {
  Set-Content -Path $overridePath -Value $expectedContent -Encoding utf8 -NoNewline
  Write-Host "Cree : docker-compose.override.yml (1433:1433)"
  return
}

$currentContent = (Get-Content -Path $overridePath -Raw).TrimEnd() + "`n"

if ($Force -or $currentContent -ne $expectedContent) {
  if (-not $Force -and $currentContent -ne $expectedContent) {
    Write-Host "Override different du modele - re-synchronisation..."
  }
  Set-Content -Path $overridePath -Value $expectedContent -Encoding utf8 -NoNewline
  Write-Host "Mis a jour : docker-compose.override.yml (1433:1433)"
  return
}

Write-Host "OK : docker-compose.override.yml deja a jour (1433:1433)"
