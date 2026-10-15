# Restores a backup created by scripts/backup.ps1 or scripts/backup.sh. ALL current data of the deployment is replaced.
# Usage: ./scripts/restore.ps1 -File <backup-file.tar> [-Yes]
# The container must exist (run "docker compose up -d" first on a new server) and .env must contain
# the same SECURITY_ENCRYPTION_KEY that was used when the backup was created.
param(
    [Parameter(Mandatory = $true)][string]$File,
    [switch]$Yes,
    [string]$Container = "sharing-system",
    [string]$Image = "sharing-system:latest"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $File -PathType Leaf)) { throw "The backup file '$File' does not exist." }

if (-not $Yes) {
    $answer = Read-Host "All current data will be replaced by $File. Continue? [y/N]"
    if ($answer -ne "y") { Write-Host "Cancelled."; exit 1 }
}

$fullPath = (Resolve-Path $File).Path
$backupDir = Split-Path $fullPath -Parent
$name = Split-Path $fullPath -Leaf

Write-Host "Stopping the application..."
docker stop $Container | Out-Null

Write-Host "Restoring data..."
$script = 'set -e; cd /app/data; rm -rf uploads keys archives sharing.db sharing.db-wal sharing.db-shm; ' +
          'tar -xf "/backup/$NAME" -C /app/data; chown -R "$APP_UID:$APP_UID" uploads keys sharing.db'
docker run --rm --user 0 --entrypoint sh --volumes-from $Container -v "${backupDir}:/backup:ro" `
    -e "NAME=$name" $Image -c $script
$restoreExitCode = $LASTEXITCODE

Write-Host "Starting the application..."
docker start $Container | Out-Null
if ($restoreExitCode -ne 0) { throw "Restoring the data failed." }
Write-Host "Restore completed."
