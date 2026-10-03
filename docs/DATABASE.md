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

1. Applies EF Core migrations (`Database.MigrateAsync`) via `DatabaseInitializer`.
2. Seeds the default roles `Default`, `Moderator`, and `Admin` together with their claims.

No migrations are shipped in this repository, so `MigrateAsync` has nothing to apply and the schema is
**not** created automatically. Generate an initial migration (see below) before pointing the app at an
empty database, or swap the initializer call for `Database.EnsureCreated()`.

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
- `Database:Version` is **required**. Startup throws if it is missing or not parseable, because the
  value is passed to `MySqlServerVersion` for the MySQL provider. Example: `"8.0.31"`.
- Retry-on-failure is enabled for both MySQL and PostgreSQL, but not for SQLite.