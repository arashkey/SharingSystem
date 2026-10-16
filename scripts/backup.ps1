# Creates a full backup of the Docker deployment while it keeps running:
# a consistent database snapshot, the uploaded files and the data protection keys, packed into one .tar file.
# Usage: ./scripts/backup.ps1 [-BackupDir <folder>]          (default: ./backups)
# The .env file (encryption key) is NOT part of the backup; keep a copy of it in a separate, safe place.
param(
    [string]$BackupDir = "./backups",
    [string]$Container = "sharing-system",
    [string]$Image = "sharing-system:latest"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force $BackupDir | Out-Null
$backupPath = (Resolve-Path $BackupDir).Path
$name = "sharing-backup-$((Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')).tar"

Write-Host "Creating a database snapshot..."
$snapshot = (docker exec $Container dotnet SharingSystem.Web.dll backup-database | Select-Object -Last 1)
if ($LASTEXITCODE -ne 0) { throw "The database snapshot failed." }

Write-Host "Archiving the database snapshot, uploaded files and keys..."
# Unfinished uploads (*.part) and cached zip archives are skipped: they are temporary.
# The image's BusyBox tar honours only one -C, so the snapshot copy and symlinks to the data
# folders are staged in one directory and archived with -h (follow symlinks).
$script = 'set -e; mkdir -p /tmp/stage; cp "$SNAPSHOT" /tmp/stage/sharing.db; ' +
          'ln -s /app/data/uploads /tmp/stage/uploads; ln -s /app/data/keys /tmp/stage/keys; ' +
          'tar -chf "/backup/$NAME" --exclude="*.part" -C /tmp/stage uploads keys sharing.db'
docker run --rm --user 0 --entrypoint sh --volumes-from "${Container}:ro" -v "${backupPath}:/backup" `
    -e "SNAPSHOT=$snapshot" -e "NAME=$name" $Image -c $script
if ($LASTEXITCODE -ne 0) { throw "Archiving the data failed." }

Write-Host "Backup written to $(Join-Path $backupPath $name)"
