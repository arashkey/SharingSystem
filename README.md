# Sharing System

A lightweight, self-hosted file sharing platform built with **ASP.NET Core 10 MVC**, inspired by
[Pingvin Share](https://github.com/stonith404/pingvin-share). A Persian guide is available in [docs/fa/README.md](docs/fa/README.md).

## Features

- Share files via a link: custom link ID, name, description, expiration, password protection, view limit
- Chunked uploads (large files), progress bars, drag & drop
- Share page with previews (image, video, audio, PDF, text) and "download all" as zip
- Send share links to e-mail recipients
- Reverse shares: let others upload files to you (size limit, use count, simplified mode, private access, e-mail notification)
- Accounts: sign up / sign in, TOTP two-factor authentication, password reset by e-mail, profile and password change
- My shares: list, copy link, edit details, add/remove files, delete
- Admin panel: dashboard, user management (create/invite, edit, admin role, reset 2FA, delete), all shares, configuration
  (general, share limits, e-mail templates, SMTP with test e-mail, imprint/privacy pages)
- Automatic cleanup of expired shares, abandoned uploads and expired reverse shares
- Light/dark theme, responsive layout
- Bilingual UI: English and Persian (right-to-left layout, Vazirmatn font, Solar Hijri dates). The language follows the
  browser and can be switched in the header; texts live in `src/SharingSystem.Web/Localization/SharedResource.fa.resx`
  (resource key = English text)

## Quick start (development)

```bash
./scripts/setup-dev.ps1        # or: sh scripts/setup-dev.sh   (creates the encryption key in user secrets)
dotnet run --project src/SharingSystem.Web
```

Open http://localhost:5069. The first registered user becomes the administrator.

## Docker

```bash
cp .env.example .env           # then set SECURITY_ENCRYPTION_KEY (openssl rand -base64 32)
docker compose up -d --build
```

Open http://localhost:3000. Data (SQLite database, files, keys) is stored in the `sharing-data` volume.

## Backup

- The application writes a consistent SQLite snapshot every 6 hours to `data/backups` (settings `Backup:*`); an on-demand
  snapshot is created with `docker exec sharing-system dotnet SharingSystem.Web.dll backup-database`.
- `scripts/backup.sh` / `scripts/backup.ps1` create a full backup (database snapshot, uploaded files, keys) as a `.tar`
  file without stopping the application; `scripts/restore.sh` / `scripts/restore.ps1` restore it.
- The `.env` file (encryption key) is not part of the backup; store it separately.

See the Persian guide for the recommended backup schedule and the safeguards for large uploads.

## Architecture

| Project | Responsibility |
|---|---|
| `SharingSystem.Domain` | Entities (`*Entity`), enums, constants |
| `SharingSystem.Application` | Use cases/services, DTOs, abstractions, Mapster mappings |
| `SharingSystem.Infrastructure` | EF Core (SQLite), migrations, local file storage, AES-GCM secret protection, PBKDF2 hashing, TOTP, SMTP (MailKit), cleanup job |
| `SharingSystem.Web` | MVC controllers and views, admin area, upload API, cookie authentication |

## License

Licensed under the [Apache License, Version 2.0](LICENSE).
