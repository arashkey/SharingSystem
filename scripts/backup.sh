#!/usr/bin/env sh
# Creates a full backup of the Docker deployment while it keeps running:
# a consistent database snapshot, the uploaded files and the data protection keys, packed into one .tar file.
# Usage: sh scripts/backup.sh [backup-directory]          (default: ./backups)
# The .env file (encryption key) is NOT part of the backup; keep a copy of it in a separate, safe place.
set -e

CONTAINER="${CONTAINER_NAME:-sharing-system}"
IMAGE="${IMAGE_NAME:-sharing-system:latest}"
BACKUP_DIR="${1:-./backups}"
NAME="sharing-backup-$(date -u +%Y%m%d-%H%M%S).tar"

mkdir -p "$BACKUP_DIR"
BACKUP_DIR="$(cd "$BACKUP_DIR" && pwd)"

echo "Creating a database snapshot..."
SNAPSHOT="$(docker exec "$CONTAINER" dotnet SharingSystem.Web.dll backup-database | tail -n 1)"

echo "Archiving the database snapshot, uploaded files and keys..."
# Unfinished uploads (*.part) and cached zip archives are skipped: they are temporary.
docker run --rm --user 0 --entrypoint sh \
    --volumes-from "$CONTAINER:ro" \
    -v "$BACKUP_DIR:/backup" \
    -e SNAPSHOT="$SNAPSHOT" -e NAME="$NAME" -e HOST_UID="$(id -u)" -e HOST_GID="$(id -g)" \
    "$IMAGE" -c '
        set -e
        mkdir -p /tmp/stage
        cp "$SNAPSHOT" /tmp/stage/sharing.db
        tar -cf "/backup/$NAME" --exclude="*.part" -C /app/data uploads keys -C /tmp/stage sharing.db
        chown "$HOST_UID:$HOST_GID" "/backup/$NAME"'

echo "Backup written to $BACKUP_DIR/$NAME"
