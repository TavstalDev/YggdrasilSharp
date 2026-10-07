# Getting Started

This guide walks through installing, configuring, and running YggdrasilSharp for the first time.

## Table of Contents
- [Prerequisites](#prerequisites)
- [1. Clone & Restore](#1-clone--restore)
- [2. Configure Secrets](#2-configure-secrets)
- [3. Configure appsettings.json](#3-configure-appsettingsjson)
- [4. Use Mailhog for testing emails](#4-use-mailhog-for-testing-emails)
- [5. Run](#5-run)
- [6. Next Steps](#6-next-steps)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A database. Any of the following works:
  - MySQL 8+
  - PostgreSQL
  - SQLite (zero setup, uses a local file)
- [ClamAV](https://www.clamav.net/) — every file upload is scanned through it. `CLAM_AV_HOST` and
  `CLAM_AV_PORT` are required configuration; keep the daemon reachable, because scan errors are
  logged and the upload is then treated as clean.

## 1. Clone & Restore

```bash
git clone https://github.com/TavstalDev/YggdrasilSharp.git
cd YggdrasilSharp
dotnet restore YggdrasilSharp.sln
```

## 2. Configure Secrets

Create a `.env` file from the template:

```bash
cp YggdrasilSharp/.env.example YggdrasilSharp/.env
```

Edit `YggdrasilSharp/.env` and replace every placeholder:

| Variable | Purpose |
| --- | --- |
| `JWT_ENCRYPTION_KEY` | Symmetric key used to sign/encrypt JWTs. Use a secure random string (32+ bytes). **Required** — startup throws if missing. |
| `FINGERPRINT_SIGNING_KEY` | HMAC key for machine (device) fingerprints. **Required** — startup throws if missing. |
| `TWO_FACTOR_ENCRYPTION_KEY` | Encrypts stored two-factor (TOTP) secrets. **Required** — startup throws if missing. |
| `DB_USER` / `DB_PASSWORD` | Database credentials; substituted into the connection string at startup. |
| `EMAIL_ADDRESS` / `EMAIL_PASSWORD` | SMTP sender used for confirmation mails, recovery, and 2FA. |
| `CERTIFICATE_FINGERPRINT` | Windows: SHA-1 thumbprint of a store certificate. Linux/macOS: absolute path to a `.pfx` certificate file. **Required** — startup throws if missing. |
| `CERTIFICATE_PASSWORD` | Password for the certificate file (required on Linux/macOS). **Required** — startup throws if missing. |
| `CLAM_AV_HOST` / `CLAM_AV_PORT` | ClamAV endpoint for upload scanning. `CLAM_AV_HOST` is **required** (startup throws if missing); `CLAM_AV_PORT` defaults to `3310`. |
| `ADMIN_PASSWORD` | Password for the admin account seeded on first startup. **Required while seeding** — first startup throws if it is empty. |
| `ADMIN_USERNAME` / `ADMIN_EMAIL` | Identity of the seeded admin account. Optional; default to `admin` and `admin@localhost`. |

## 3. Configure appsettings.json

Open `YggdrasilSharp/appsettings.json` and adjust at minimum:

- `AllowedHosts` – semicolon-separated hosts allowed to reach the API. **The shipped value is a
  placeholder** (`api.example.com;example.com;dashboard.example.com`); replace it with `localhost`
  plus your real domains, otherwise requests to `localhost` are rejected with `400 Bad Request`.
- `Database:Provider` – `MySql`, `PostgreSql`, or `Sqlite` (defaults to MySQL if unknown).
- `Database:ConnectionString` (see [Database](DATABASE.md)).
- `Database:Version` – server version such as `"8.0.31"`. **Required for every provider**; startup
  throws if it is missing or not parseable.
- `Runtime:WebsiteUrl` / `Runtime:ApiUrl` – the public URLs used in links and textures.
- `Yggdrasil:SkinDomains` – allowed domains for serving skins.
- `Yggdrasil:EnableLegacyAuth` – set to `true` to expose the legacy auth-server endpoints used by
  authlib-injector (see [Game](GAME.md) and the [API Reference](API.md#legacy-auth-server));
  disabled by default, and the routes return `403` while it is off.
- `CORS:Default:Sites` – allowed frontend origins.

All settings have XML-documented keys in `Constants.cs` if you need the full list.

## 4. Use Mailhog for testing emails

For local development, you can use **Mailhog** to catch and inspect emails without actually sending them.

1. **Install Mailhog**:
```bash
# Using Docker (recommended)
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog
   
# Or download from https://github.com/mailhog/MailHog
```

2. **Set environment variables**:
```
EMAIL_ADDRESS=test@example.com
EMAIL_PASSWORD=anything
```

3. **Access the web UI**: Open `http://localhost:8025` to view sent emails

## 5. Run

```bash
dotnet run --project YggdrasilSharp
```

Kestrel listens on the port set in `Application:Port` (`5001` by default; the Development environment
overrides it to `36767`). When a certificate is configured the listener uses HTTPS, otherwise HTTP.

- Root endpoint: `http://localhost:5001/`
- API base: the routes under the Controller folders, e.g. `http://localhost:5001/register`
- Swagger UI: served **only in the Development environment**. Plain `dotnet run` uses the
  `Production` profile (no Swagger), so start it with the Development profile instead:

  ```bash
  dotnet run --project YggdrasilSharp --launch-profile Development
  ```

  then open `https://localhost:36767/docs` (or `http://localhost:36767/docs` when no certificate is
  configured).

> The default roles (`Default`, `Moderator`, `Admin`) and their claims are seeded on first startup,
> along with the admin account created from `ADMIN_USERNAME` / `ADMIN_EMAIL` / `ADMIN_PASSWORD`. The
> schema itself comes from the EF Core migrations shipped in `YggdrasilSharp/Migrations/`, which are
> applied automatically — see [Database](DATABASE.md#initialization).

## 6. Next Steps

- See [API Reference](API.md) for available endpoints.
- See [Database](DATABASE.md) for provider-specific connection strings.
