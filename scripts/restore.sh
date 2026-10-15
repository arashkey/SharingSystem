#!/usr/bin/env sh
# Restores a backup created by scripts/backup.sh. ALL current data of the deployment is replaced.
# Usage: sh scripts/restore.sh <backup-file.tar> [--yes]
# The container must exist (run "docker compose up -d" first on a new server) and .env must contain
# the same SECURITY_ENCRYPTION_KEY that was used when the backup was created.
set -e

CONTAINER="${CONTAINER_NAME:-sharing-system}"
IMAGE="${IMAGE_NAME:-sharing-system:latest}"
FILE="$1"

if [ ! -f "$FILE" ]; then
    echo "Usage: sh scripts/restore.sh <backup-file.tar> [--yes]"
    exit 1
fi

if [ "$2" != "--yes" ]; then
    printf "All current data will be replaced by %s. Continue? [y/N] " "$FILE"
    read -r ANSWER
    if [ "$ANSWER" != "y" ] && [ "$ANSWER" != "Y" ]; then
        echo "Cancelled."
        exit 1
    fi
fi

BACKUP_DIR="$(cd "$(dirname "$FILE")" && pwd)"
NAME="$(basename "$FILE")"

echo "Stopping the application..."
docker stop "$CONTAINER" > /dev/null

echo "Restoring data..."
STATUS=0
docker run --rm --user 0 --entrypoint sh \
    --volumes-from "$CONTAINER" \
    -v "$BACKUP_DIR:/backup:ro" \
    -e NAME="$NAME" \
    "$IMAGE" -c '
        set -e
        cd /app/data
        rm -rf uploads keys archives sharing.db sharing.db-wal sharing.db-shm
        tar -xf "/backup/$NAME" -C /app/data
        chown -R "$APP_UID:$APP_UID" uploads keys sharing.db' || STATUS=$?

echo "Starting the application..."
docker start "$CONTAINER" > /dev/null

if [ "$STATUS" -ne 0 ]; then
    echo "Restoring the data failed."
    exit "$STATUS"
fi
echo "Restore completed."
