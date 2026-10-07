# Database

YggdrasilSharp uses Entity Framework Core and supports **MySQL**, **PostgreSQL**, and **SQLite**.

## Table of Contents
- [Provider Selection](#provider-selection)
- [Connection Strings](#connection-strings)
  - [MySQL](#mysql)
  - [PostgreSQL](#postgresql)
  - [SQLite](#sqlite)
- [Initialization](#initialization)
- [Common Commands](#common-commands)
  - [Add a Migration](#add-a-migration)
  - [Update the Database](#update-the-database)
  - [List Migrations](#list-migrations)
  - [Remove Last Migration](#remove-last-migration)
- [Notes](#notes)

## Provider Selection

Set `Database:Provider` in `appsettings.json`:

- `MySql`
- `PostgreSql`
- `Sqlite`

Anything else falls back to MySQL.

## Connection Strings

Credentials are stored in `.env` and substituted at runtime via the `$DB_USER` / `$DB_PASSWORD` placeholders.

### MySQL

```json
"ConnectionString": "server=127.0.0.1;port=3306;database=yggdrasil;uid=$DB_USER;pwd=$DB_PASSWORD;"
```

### PostgreSQL

```json
"ConnectionString": "Host=127.0.0.1;Port=5432;Database=yggdrasil;Username=$DB_USER;Password=$DB_PASSWORD;"
```

### SQLite

```json
"Provider": "Sqlite",
"ConnectionString": "Data Source=yggdrasil.db"
```

SQLite needs no server, user, or password — everything lives in a single file. `$DB_USER` / `$DB_PASSWORD` are simply ignored.

## Initialization

On startup the application:

1. Applies the EF Core migrations shipped in `YggdrasilSharp/Migrations/` (`Database.MigrateAsync`)
   via `DatabaseInitializer`, so an empty database is schema'd automatically.
2. Seeds the default roles `Default`, `Moderator`, and `Admin` together with their claims, and on a
   fresh database creates the admin account from the `ADMIN_USERNAME` / `ADMIN_EMAIL` /
   `ADMIN_PASSWORD` environment variables.

> **Provider note:** the shipped migration was generated against MySQL,
> including MySQL identity-column annotations. It has not been validated on PostgreSQL or SQLite —
> if you use either provider, inspect the schema after the first run, or generate a
> provider-specific migration with the command below, before going to production.

## Common Commands

### Add a Migration
```bash
dotnet ef migrations add <MigrationName> --project ./YggdrasilSharp/YggdrasilSharp.csproj --startup-project ./YggdrasilSharp/YggdrasilSharp.csproj
```

### Update the Database
```bash
dotnet ef database update --project ./YggdrasilSharp/YggdrasilSharp.csproj --startup-project ./YggdrasilSharp/YggdrasilSharp.csproj
```

### List Migrations
```bash
dotnet ef migrations list --project ./YggdrasilSharp/YggdrasilSharp.csproj --startup-project ./YggdrasilSharp/YggdrasilSharp.csproj
```

### Remove Last Migration
```bash
dotnet ef migrations remove --project ./YggdrasilSharp/YggdrasilSharp.csproj --startup-project ./YggdrasilSharp/YggdrasilSharp.csproj
```

## Notes

- The placeholder substitution happens in `Program.cs` before the provider is selected.
- `Database:Version` is **required for every provider**. Startup throws if it is missing or not
  parseable; the value is passed to `MySqlServerVersion` when the MySQL provider is selected.
  Example: `"8.0.31"`.
- Retry-on-failure is enabled for both MySQL and PostgreSQL, but not for SQLite.
