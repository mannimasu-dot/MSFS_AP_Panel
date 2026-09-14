<#
Cleanup-Skript: Verschiebt SimConnect SDK Samples/Documentation und erkannte SimConnect-Binärdateien in ein Backup-Verzeichnis.
Führt keine irreversible Löschung durch — Dateien werden verschoben, damit sie bei Bedarf wiederhergestellt werden können.
#>

param(
	[switch] $Force
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Definition
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backup = Join-Path $root "removed_simconnect_backup_$timestamp"

Write-Host "Arbeitsverzeichnis: $root"
Write-Host "Backup-Ordner: $backup"

if (-not $Force)
{
	$ans = Read-Host "Dieses Skript verschiebt 'Samples/', 'Documentation/' und SimConnect*-DLLs nach $backup. Fortfahren? (J/N)"
	if ($ans -ne 'J' -and $ans -ne 'j') { Write-Host "Abgebrochen."; exit 0 }
}

New-Item -ItemType Directory -Path $backup -Force | Out-Null

$itemsMoved = @()

$pathsToRemove = @("Samples", "Documentation")
foreach ($p in $pathsToRemove)
{
	$full = Join-Path $root $p
	if (Test-Path $full)
	{
		$dest = Join-Path $backup $p
		Write-Host "Verschiebe $full -> $dest"
		Move-Item -Path $full -Destination $dest -Force
		$itemsMoved += $full
	}
}

# SimConnect binaries in repo root or subfolders
$dllPatterns = @('SimConnect*.dll','SimConnect_*.dll')
Get-ChildItem -Path $root -Recurse -Include $dllPatterns -File -ErrorAction SilentlyContinue | ForEach-Object {
	$relative = $_.FullName.Substring($root.Length).TrimStart('\')
	$destDir = Join-Path $backup (Split-Path $relative)
	if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
	Write-Host "Verschiebe $($_.FullName) -> $destDir"
	Move-Item -Path $_.FullName -Destination $destDir -Force
	$itemsMoved += $_.FullName
}

if ($itemsMoved.Count -eq 0) { Write-Host "Keine zu verschiebenden SDK-Dateien gefunden." } else { Write-Host "Verschoben: $($itemsMoved.Count) Elemente." }

Write-Host "Fertig. Backup liegt in: $backup"
Write-Host "Hinweis: Falls Sie die Dateien endgültig löschen möchten, löschen Sie das Backup-Verzeichnis manuell."