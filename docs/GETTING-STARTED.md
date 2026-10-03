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
| `JWT_ENCRYPTION_KEY` | Symmetric key used to sign/encrypt JWTs. Use a secure random string (32+ bytes). |
| `DB_USER` / `DB_PASSWORD` | Database credentials; substituted into the connection string at startup. |
| `EMAIL_ADDRESS` / `EMAIL_PASSWORD` | SMTP sender used for confirmation mails, recovery, and 2FA. |
| `CERTIFICATE_FINGERPRINT` | Windows: SHA-1 thumbprint of a store certificate. Linux/macOS: absolute path to a `.pfx` certificate file. |
| `CERTIFICATE_PASSWORD` | Password for the certificate file (required on Linux/macOS). |

## 3. Configure appsettings.json

Open `YggdrasilSharp/appsettings.json` and adjust at minimum:

- `Database:Provider` – `MySql`, `PostgreSql`, or `Sqlite` (defaults to MySQL if unknown).
- `Database:ConnectionString` (see [Database](DATABASE.md)).
- `Runtime:WebsiteUrl` / `Runtime:ApiUrl` – the public URLs used in links and textures.
- `Yggdrasil:SkinDomains` – allowed domains for serving skins.
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

Kestrel listens on the port set in `Application:Port` (default `5001`).

- Swagger UI: `http://localhost:5001/docs`
- Root endpoint: `http://localhost:5001/`
- API base: the routes under the Controller folders, e.g. `http://localhost:5001/register`

> The default roles (`Default`, `Moderator`, `Admin`) and their claims are seeded on first startup. The
> schema itself comes from EF Core migrations, and no migrations are currently shipped — see
> [Database](DATABASE.md#initialization).

## 6. Next Steps

- See [API Reference](API.md) for available endpoints.
- See [Database](DATABASE.md) for provider-specific connection strings.