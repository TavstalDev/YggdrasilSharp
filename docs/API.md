# API Reference

Authentication uses one of two schemes: **Bearer** or **Basic**, both JWT-based. URLs are relative to the configured API base URL (e.g. `http://localhost:5001`). Most endpoints respond with JSON; the texture, file download, avatar, and skin endpoints respond with raw file content.

Interactive documentation (Swagger UI) is available at `/docs`.

## Table of Contents
- [Authentication](#authentication)
  - [Login](#login)
  - [Registration](#registration)
  - [Password Recovery](#password-recovery)
  - [Two-Factor Authentication](#two-factor-authentication)
- [User](#user)
  - [Public Profile](#public-profile)
  - [Own Profile](#own-profile)
  - [Administrative (by user id)](#administrative-by-user-id)
- [Capes & Files](#capes--files)
- [News](#news)
- [Launcher](#launcher)
- [Yggdrasil (Minecraft Auth)](#yggdrasil-minecraft-auth)
  - [Metadata & Keys](#metadata--keys)
  - [Profiles & Textures](#profiles--textures)
  - [Session Server](#session-server)
- [Versioning](#versioning)

## Authentication

### Login

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| POST | `/login` | Authenticate a user (password + optional TOTP) and issue a JWT. | Public |
| PATCH | `/login/2fa` | Complete a login that has 2FA enabled using a TOTP/backup code. | Public |
| POST | `/login/launcher` | Launcher-specific login flow. | Public |
| PATCH | `/login/launcher/2fa` | Finish a launcher login with 2FA. | Public |
| POST | `/logout` | Invalidate the current session/token. | Bearer/Basic |

### Registration

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| POST | `/register` | Create a new account and send a confirmation email. | Public |
| PATCH | `/register/confirm` | Confirm an account with the emailed token. | Public |

### Password Recovery

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| POST | `/recovery/password/request` | Request a password reset link. | Public |
| POST | `/recovery/password` | Reset the password with the emailed token. | Public |
| POST | `/recovery/2fa/request` | Request a 2FA recovery code. | Public |
| POST | `/recovery/2fa` | Reset 2FA using a backup code. | Public |

### Two-Factor Authentication

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| PATCH | `/2fa/enable` | Enable TOTP 2FA. | Bearer/Basic |
| PATCH | `/2fa/disable` | Disable TOTP 2FA. | Bearer/Basic |
| PATCH | `/2fa/generate` | Generate a TOTP secret/QR code. | Bearer/Basic |
| PATCH | `/2fa/regenerate/recovery` | Regenerate backup codes. | Bearer/Basic |

## User

All endpoints in this section require authentication, except the two public profile lookups.

### Public Profile

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| GET | `/user/{userId}` | Fetch a public user profile (username, UUID). | Public |
| GET | `/user/{userId}/avatar` | Fetch a user's avatar image. | Public |

### Own Profile

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| GET | `/user/skin` | Fetch your current skin. | Bearer/Basic |
| PUT | `/user/skin` | Upload/replace your skin. | Bearer/Basic |
| DELETE | `/user/skin` | Remove your skin. | Bearer/Basic |
| GET | `/user/avatar` | Fetch your avatar. | Bearer/Basic |
| POST | `/user/avatar` | Upload/replace your avatar. | Bearer/Basic |
| DELETE | `/user/avatar` | Remove your avatar. | Bearer/Basic |
| PATCH | `/user/cape/{capeId}` | Equip a cape. | Bearer/Basic |
| DELETE | `/user/cape` | Unequip your cape. | Bearer/Basic |
| GET | `/user/sessions` | List your active sessions. | Bearer/Basic |
| DELETE | `/user/sessions/{sessionId}` | Revoke a specific session. | Bearer/Basic |
| DELETE | `/user/sessions` | Revoke all of your sessions. | Bearer/Basic |

### Administrative (by user id)

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| GET | `/user/{userId}/skin` | Fetch another user's skin. | Bearer/Basic |
| PUT | `/user/{userId}/skin` | Set another user's skin. | Bearer/Basic |
| DELETE | `/user/{userId}/skin` | Remove another user's skin. | Bearer/Basic |
| POST | `/user/{userId}/avatar` | Set another user's avatar. | Bearer/Basic |
| DELETE | `/user/{userId}/avatar` | Remove another user's avatar. | Bearer/Basic |
| PATCH | `/user/{userId}/cape/{capeId}` | Equip a cape for another user. | Bearer/Basic |
| DELETE | `/user/{userId}/cape` | Unequip another user's cape. | Bearer/Basic |
| GET | `/user/{userId}/sessions` | List another user's sessions. | Bearer/Basic |
| DELETE | `/user/{userId}/sessions/{sessionId}` | Revoke another user's session. | Bearer/Basic |
| DELETE | `/user/{userId}/sessions` | Revoke all of another user's sessions. | Bearer/Basic |

## Capes & Files

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| POST | `/capes` | Upload a new cape (PNG, validated dimensions). | Bearer/Basic |
| DELETE | `/capes/{capeId}` | Delete a cape. | Bearer/Basic |
| GET | `/files/{hash}` | Download a stored file by its SHA-256 hash. | Public |

## News

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| GET | `/news` | List all news posts. | Public |
| GET | `/news/latest` | Fetch the latest news posts. | Public |
| GET | `/news/{id}` | Fetch a single news post. | Public |
| POST | `/news` | Create a news post. | Bearer/Basic |
| PUT | `/news/{id}` | Update a news post. | Bearer/Basic |
| DELETE | `/news/{id}` | Delete a news post. | Bearer/Basic |

## Launcher

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| GET | `/launcher/versions` | List all launcher versions. | Public |
| GET | `/launcher/versions/latest` | Fetch the latest launcher version. | Public |
| GET | `/launcher/version/{id}` | Fetch version details. | Public |
| GET | `/launcher/version/{id}/download` | Download a version archive. | Public |
| POST | `/launcher/version` | Create a launcher version. | Bearer/Basic |
| PUT | `/launcher/version/{id}` | Update a launcher version. | Bearer/Basic |
| DELETE | `/launcher/version/{id}` | Delete a launcher version. | Bearer/Basic |
| POST | `/launcher/version/{id}/data` | Add a data/blob to a version. | Bearer/Basic |
| DELETE | `/launcher/version/{versionId}/data/{dataId}` | Remove data from a version. | Bearer/Basic |

## Yggdrasil (Minecraft Auth)

These endpoints are compatible with the official Yggdrasil protocol and the Minecraft session server.

### Metadata & Keys

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| GET | `/yggdrasil` | Root metadata: skin domains, public key signature, server/implementation name and version, feature flags, and homepage/register links. | Public |
| GET | `/yggdrasil/publickeys` | Public profile keys. Currently returns an empty list. | Public |
| GET | `/yggdrasil/minecraftservices/publickeys` | Public profile keys (Minecraft services alias). Currently returns an empty list. | Public |

### Profiles & Textures

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| POST | `/yggdrasil/api/profiles/minecraft` | Look up profiles by username(s). | Public |
| GET | `/yggdrasil/textures/{hash}` | Fetch texture data by hash. | Public |

### Session Server

Routes are available under both client (`yggdrasil/session/minecraft`) and server (`yggdrasil/sessionserver/session/minecraft`) prefixes.

| Method | Route | Description | Auth |
| --- | --- | --- | --- |
| GET | `/yggdrasil/sessionserver/blockedservers` | List of blocked servers (currently empty). | Public |
| POST | …/`join` | Verify the client joined a server. | Access token |
| GET | …/`hasJoined` | Confirm a profile has joined a server. | Public |
| GET | …/`profile/{uuid}` | Fetch a session profile by UUID (with signatures). | Public |

## Versioning

The API is currently versioned as `v1` in the Swagger document. No path-based versioning is applied.