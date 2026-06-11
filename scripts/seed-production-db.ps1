<#
.SYNOPSIS
  Peuple la base de donnees de production EcoScolar de maniere idempotente.

.DESCRIPTION
  Le script lance le mode CLI --seed-production de l'API via Docker Compose.
  Il applique les migrations, garantit les roles/localites, cree les admins configures,
  et peut ajouter des donnees de demonstration avec -IncludeDemoData.

  Il ne vide jamais la base et ne remplace pas les mots de passe d'utilisateurs existants.

.EXAMPLE
  $env:ECOSCOLAR_SEED_ADMIN_EMAIL = "admin@example.ch"
  $env:ECOSCOLAR_SEED_ADMIN_PASSWORD = "ChangeMe-Strong-Password-123!"
  $env:ECOSCOLAR_SEED_ADMIN2_EMAIL = "admin2@example.ch"
  $env:ECOSCOLAR_SEED_ADMIN2_PASSWORD = "ChangeMe-Strong-Password-456!"
  .\scripts\seed-production-db.ps1 -Yes

.EXAMPLE
  $env:ECOSCOLAR_SEED_ADMIN_EMAIL = "admin@example.ch"
  $env:ECOSCOLAR_SEED_ADMIN_PASSWORD = "ChangeMe-Strong-Password-123!"
  $env:ECOSCOLAR_SEED_DEMO_PASSWORD = "Demo-Strong-Password-123!"
  .\scripts\seed-production-db.ps1 -IncludeDemoData -Yes
#>

[CmdletBinding()]
param(
  [string]$ProjectDirectory = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
  [string]$ComposeFile = 'docker-compose.yaml',
  [string]$AdminEmail = $env:ECOSCOLAR_SEED_ADMIN_EMAIL,
  [string]$AdminPassword = $env:ECOSCOLAR_SEED_ADMIN_PASSWORD,
  [string]$AdminFirstName = $env:ECOSCOLAR_SEED_ADMIN_FIRST_NAME,
  [string]$AdminLastName = $env:ECOSCOLAR_SEED_ADMIN_LAST_NAME,
  [string]$AdminNickname = $env:ECOSCOLAR_SEED_ADMIN_NICKNAME,
  [string]$Admin2Email = $env:ECOSCOLAR_SEED_ADMIN2_EMAIL,
  [string]$Admin2Password = $env:ECOSCOLAR_SEED_ADMIN2_PASSWORD,
  [string]$Admin2FirstName = $env:ECOSCOLAR_SEED_ADMIN2_FIRST_NAME,
  [string]$Admin2LastName = $env:ECOSCOLAR_SEED_ADMIN2_LAST_NAME,
  [string]$Admin2Nickname = $env:ECOSCOLAR_SEED_ADMIN2_NICKNAME,
  [switch]$IncludeDemoData,
  [string]$DemoPassword = $env:ECOSCOLAR_SEED_DEMO_PASSWORD,
  [switch]$SkipBuild,
  [switch]$Yes
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Set-EnvForDocker {
  param(
    [Parameter(Mandatory = $true)][string]$Name,
    [AllowNull()][string]$Value
  )

  if ([string]::IsNullOrWhiteSpace($Value)) {
    return $false
  }

  [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
  return $true
}

if (-not (Test-Path -LiteralPath $ProjectDirectory -PathType Container)) {
  throw "Dossier projet introuvable: $ProjectDirectory"
}

$composePath = Join-Path $ProjectDirectory $ComposeFile
if (-not (Test-Path -LiteralPath $composePath -PathType Leaf)) {
  throw "Fichier Compose introuvable: $composePath"
}

if ($IncludeDemoData -and [string]::IsNullOrWhiteSpace($DemoPassword)) {
  throw "ECOSCOLAR_SEED_DEMO_PASSWORD ou -DemoPassword est requis avec -IncludeDemoData."
}

if (-not [string]::IsNullOrWhiteSpace($AdminEmail) -and [string]::IsNullOrWhiteSpace($AdminPassword)) {
  throw "ECOSCOLAR_SEED_ADMIN_PASSWORD ou -AdminPassword est requis pour creer l'admin prod."
}

if (-not [string]::IsNullOrWhiteSpace($Admin2Email) -and [string]::IsNullOrWhiteSpace($Admin2Password)) {
  throw "ECOSCOLAR_SEED_ADMIN2_PASSWORD ou -Admin2Password est requis pour creer le deuxieme admin prod."
}

if (-not $Yes) {
  Write-Host "Ce script va ecrire dans la base de production configuree par $composePath." -ForegroundColor Yellow
  Write-Host "Il est idempotent et ne supprime aucune donnee existante." -ForegroundColor Yellow
  $answer = Read-Host "Tape SEED pour continuer"
  if ($answer -ne 'SEED') {
    Write-Host "Operation annulee."
    exit 1
  }
}

$envNames = New-Object System.Collections.Generic.List[string]
foreach ($pair in @(
  @{ Name = 'ECOSCOLAR_SEED_ADMIN_EMAIL'; Value = $AdminEmail },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN_PASSWORD'; Value = $AdminPassword },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN_FIRST_NAME'; Value = $AdminFirstName },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN_LAST_NAME'; Value = $AdminLastName },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN_NICKNAME'; Value = $AdminNickname },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN2_EMAIL'; Value = $Admin2Email },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN2_PASSWORD'; Value = $Admin2Password },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN2_FIRST_NAME'; Value = $Admin2FirstName },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN2_LAST_NAME'; Value = $Admin2LastName },
  @{ Name = 'ECOSCOLAR_SEED_ADMIN2_NICKNAME'; Value = $Admin2Nickname },
  @{ Name = 'ECOSCOLAR_SEED_DEMO_PASSWORD'; Value = $DemoPassword }
)) {
  if (Set-EnvForDocker -Name $pair.Name -Value $pair.Value) {
    $envNames.Add($pair.Name)
  }
}

Push-Location $ProjectDirectory
try {
  $compose = @('compose', '-f', $ComposeFile)

  Write-Host "Demarrage de la base SQL Server..." -ForegroundColor Cyan
  & docker @compose up -d ecoscolar-api-database
  if ($LASTEXITCODE -ne 0) {
    throw "docker compose up ecoscolar-api-database a echoue."
  }

  if (-not $SkipBuild) {
    Write-Host "Build de l'image API avec le mode seed production..." -ForegroundColor Cyan
    & docker @compose build ecoscolar-web-api
    if ($LASTEXITCODE -ne 0) {
      throw "docker compose build ecoscolar-web-api a echoue."
    }
  }

  $runArgs = @(
    'compose', '-f', $ComposeFile,
    'run', '--rm',
    '-e', 'ASPNETCORE_ENVIRONMENT=Production',
    '-e', 'ApplyDatabaseMigrations=true'
  )

  foreach ($name in $envNames) {
    $runArgs += @('-e', $name)
  }

  $runArgs += @('ecoscolar-web-api', '--seed-production')

  if ($IncludeDemoData) {
    $runArgs += '--include-demo-data'
  }

  Write-Host "Execution du seed production..." -ForegroundColor Cyan
  & docker @runArgs
  if ($LASTEXITCODE -ne 0) {
    throw "Le seed production a echoue."
  }

  Write-Host "Seed production termine avec succes." -ForegroundColor Green
}
finally {
  Pop-Location
}
