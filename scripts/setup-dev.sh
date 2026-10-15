#!/usr/bin/env sh
# Generates a random encryption key and stores it in the user secrets of the web project (development only).
set -e

PROJECT="$(dirname "$0")/../src/SharingSystem.Web"

if dotnet user-secrets list --project "$PROJECT" 2>/dev/null | grep -q "Security:EncryptionKey" && [ "$1" != "--force" ]; then
    echo "An encryption key is already configured. Use --force to replace it (stored secrets will become unreadable)."
    exit 0
fi

KEY="$(openssl rand -base64 32)"
dotnet user-secrets set "Security:EncryptionKey" "$KEY" --project "$PROJECT" > /dev/null
echo "Encryption key stored in user secrets. Start the app with: dotnet run --project src/SharingSystem.Web"
