$ErrorActionPreference = "Stop"
$backupRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = "C:\Users\micha\Desktop\MP_Fenster_Software\MP_Fenster_App"
$src = Join-Path $backupRoot 'CennikService.cs'
$dst = Join-Path $projectRoot 'CennikService.cs'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'DataService.cs'
$dst = Join-Path $projectRoot 'DataService.cs'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'ZlecenieService.cs'
$dst = Join-Path $projectRoot 'ZlecenieService.cs'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'PodgladZlecenWindows.xaml'
$dst = Join-Path $projectRoot 'PodgladZlecenWindows.xaml'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'PodgladZlecenWindows.xaml.cs'
$dst = Join-Path $projectRoot 'PodgladZlecenWindows.xaml.cs'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'TechnologAkceptacja.xaml'
$dst = Join-Path $projectRoot 'TechnologAkceptacja.xaml'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'TechnologAkceptacja.xaml.cs'
$dst = Join-Path $projectRoot 'TechnologAkceptacja.xaml.cs'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'ZlecenieDoAkceptacji.xaml'
$dst = Join-Path $projectRoot 'ZlecenieDoAkceptacji.xaml'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'ZlecenieDoAkceptacji.xaml.cs'
$dst = Join-Path $projectRoot 'ZlecenieDoAkceptacji.xaml.cs'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'SQLQuery1.sql'
$dst = Join-Path $projectRoot 'SQLQuery1.sql'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
$src = Join-Path $backupRoot 'Ikony\backup_ikony_przed_v3_20260531_185945'
$dst = Join-Path $projectRoot 'Ikony\backup_ikony_przed_v3_20260531_185945'
if (Test-Path -LiteralPath $src) { New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null; Move-Item -LiteralPath $src -Destination $dst -Force }
Write-Host "Przywracanie zakonczone." -ForegroundColor Green
