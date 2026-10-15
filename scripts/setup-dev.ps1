# Generates a random encryption key and stores it in the user secrets of the web project (development only).
param([switch]$Force)

$project = Join-Path $PSScriptRoot "..\src\SharingSystem.Web"
$existing = dotnet user-secrets list --project $project 2>$null | Select-String "Security:EncryptionKey"

if ($existing -and -not $Force) {
    Write-Host "An encryption key is already configured. Use -Force to replace it (stored secrets will become unreadable)."
    exit 0
}

$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "Security:EncryptionKey" $key --project $project | Out-Null
Write-Host "Encryption key stored in user secrets. Start the app with: dotnet run --project src/SharingSystem.Web"
