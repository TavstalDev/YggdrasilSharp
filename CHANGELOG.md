# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0]
<details>
<summary>Initial release</summary>

### Added

#### Yggdrasil compatibility
- Server root metadata at `/yggdrasil` (skin domains, public key signature, server and implementation
  name/version, feature flags, homepage and register links).
- A profile public-keys endpoint, exposed under both `/yggdrasil/publickeys` and the
  `/yggdrasil/minecraftservices/publickeys` alias. It currently returns an empty `profileKeys` list.
- Profile lookup by username or batch of usernames at `/yggdrasil/api/profiles/minecraft`.
- Texture payload retrieval by hash at `/yggdrasil/textures/{hash}`.
- Session server endpoints, served under both the client prefix
  (`/yggdrasil/session/minecraft`) and the server prefix
  (`/yggdrasil/sessionserver/session/minecraft`): blocked-server list, `join`, `hasJoined`, and
  `profile/{uuid}` with signed session profiles.
- Access-token and remote-IP validation on `join`, plus expiry enforcement for play sessions and
  server-join records.
- `ETag` / `If-None-Match` revalidation (`304 Not Modified`) on the blocked-server list and session
  profile responses.
- Legacy Yggdrasil auth-server endpoints — `authenticate`, `refresh`, `validate`, `invalidate`, and
  `signout` under `/yggdrasil/authserver/…` (and the `/yggdrasil/…` alias) — gated behind the
  `Yggdrasil:EnableLegacyAuth` configuration key, which is **disabled by default**.

#### Accounts and authentication
- Account registration with an emailed confirmation token, login, and logout.
- Password recovery through an emailed reset token.
- TOTP two-factor authentication: secret and QR code generation, enable, disable, and backup-code
  regeneration. Backup codes are stored hashed and consumed on use.
- Two-factor recovery using a backup code, for users who lose their authenticator.
- Dedicated launcher login flows at `/login/launcher` and `/login/launcher/2fa`.
- Configurable account lockout (5 attempts per 15 minutes by default).
- Two interchangeable authentication schemes — **Bearer** and **Basic** — both JWT-based and
  selectable per endpoint, with Bearer as the default challenge scheme.
- Public user profile and avatar lookups by user id.
- First-run admin account seeding from the `ADMIN_USERNAME`, `ADMIN_EMAIL`, and `ADMIN_PASSWORD`
  environment variables.

#### User assets
- Skin upload, replacement, and deletion, with PNG dimension validation accepting 64x32, 64x64,
  512x256, and 512x512 layouts, and a WIDE/SLIM model selection per user.
- Avatar upload, replacement, and deletion.
- Cape upload with validation and deletion, plus per-user equip and unequip.
- Administrative skin, avatar, and cape management for other users by id, gated on elevated
  permission claims.
- Content-addressed file storage keyed by SHA-256 hash, with `ETag`/`304` support on retrieval.
- SkiaSharp-based image decoding and validation for all uploads.
- Antivirus scanning for every file upload through ClamAV (and AMSI on Windows), configured with the
  required `CLAM_AV_HOST` / `CLAM_AV_PORT` environment variables.

#### Session management
- List your own active sessions, revoke a single session, or revoke all of them.
- Administrative session listing and revocation for other users by id.

#### Extras
- News feed: list all posts, fetch the most recent posts (`count`, default 5), retrieve a single post,
  and create, update, or delete posts. Responses are cached with `max-age=3600`.
- Launcher server: version listing, latest version, version details, archive download, full version
  CRUD, and per-version data blobs.
- Configurable skin-serving domains.

#### Persistence
- Entity Framework Core with selectable **MySQL**, **PostgreSQL**, or **SQLite** providers, including
  MySQL connection resiliency with retry-on-failure.
- EF Core schema management: the schema is created on first startup through `DatabaseInitializer`.
- An initial EF Core migration shipped in `YggdrasilSharp/Migrations/`.
- EF Core CLI tooling pinned through `.config/dotnet-tools.json`.
- Automatic seeding of the `Default`, `Moderator`, and `Admin` roles with granular permission claims
  on first startup.
- A background service that cleans up expired logins, play sessions, and server joins hourly, and
  disables itself after repeated failures to avoid thrashing.

#### Security and operations
- Configurable rate limiting supporting four strategies — fixed window, sliding window, concurrency,
  and token bucket — with nine built-in policies: a global default plus register, login, reset,
  upload, download, search, write, and admin categories.
- A default rate limit policy applied to every endpoint, with a configurable rejection status.
- Configuration-driven CORS policy, with a startup warning if origins and headers are wide open.
- Distributed session state with `HttpOnly`, essential cookies and a configurable idle timeout.
- HTTPS through Kestrel using a certificate-store thumbprint on Windows or a `.pfx` file on
  Linux/macOS, with HTTPS redirection.
- A restrictive `Content-Security-Policy` response header.
- Forwarded-headers support for reverse-proxy deployments.
- Configurable request upload size limit (100 MB by default).
- A `/health` endpoint (database-backed), correlation-ID middleware, and memory-cache size/compaction
  configuration.
- Required `FINGERPRINT_SIGNING_KEY` and `TWO_FACTOR_ENCRYPTION_KEY` environment variables for
  fingerprint HMACs and stored TOTP secrets.
- Secrets supplied through environment variables via a `.env` file, keeping credentials out of
  `appsettings.json`.

#### Email
- SMTP email delivery for confirmation and recovery messages.

#### API documentation
- Swagger / OpenAPI documentation served at `/docs` in the Development environment, versioned as `v1`
  and generated from the endpoint metadata and custom response attributes.

#### Engineering
- An xUnit v3 test suite using FluentAssertions and Moq, covering the controllers and both
  authentication handlers.
- Continuous integration and a release workflow that publishes self-contained `linux-x64` and
  `win-x64` builds as zip archives attached to GitHub releases.

### Changed
- Database initialization now applies migrations with `Database.MigrateAsync` instead of
  `Database.EnsureCreatedAsync`, so fresh databases are schema'd through the normal migration
  pipeline.
- The two-factor login flow now identifies the session with a session token and user id instead of
  cookies.
- Controllers cache with sliding expiration; startup validates required configuration (certificate,
  JWT keys, database settings) and fails fast with a clear error.

### Fixed
- Cookie `SameSite` and `SecurePolicy` defaults hardened.
- `MinecraftProfile` now enforces the maximum username length.
- TOTP verification window adjusted to reduce legitimate-code rejections.
- Password-pwned API request timeout increased for reliability.

</details>

[unreleased]: https://github.com/TavstalDev/YggdrasilSharp/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/TavstalDev/YggdrasilSharp/releases/tag/v1.0.0
