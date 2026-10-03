# YggdrasilSharp

![Latest Version](https://img.shields.io/github/v/release/TavstalDev/YggdrasilSharp?style=plastic-square)
![Workflow Status](https://img.shields.io/github/actions/workflow/status/TavstalDev/YggdrasilSharp/release.yml?branch=stable&label=build&style=plastic-square)
![License](https://img.shields.io/github/license/TavstalDev/YggdrasilSharp?style=plastic-square)
![Stars](https://img.shields.io/github/stars/TavstalDev/YggdrasilSharp?style=plastic-square)
![Issues](https://img.shields.io/github/issues/TavstalDev/YggdrasilSharp?style=plastic-square)
![Forks](https://img.shields.io/github/forks/TavstalDev/YggdrasilSharp?style=plastic-square)

A lightweight, self-hostable implementation of the Minecraft [Yggdrasil](https://minecraft.wiki/w/Minecraft_Wiki:Projects/wiki.vg/Yggdrasil_protocol) authentication API built with ASP.NET Core. It is designed for offline testing, launcher development, and custom skin/session handling.

## Features

- **Yggdrasil compatibility** – authentication sessions, profiles, textures, and server join (`hasJoined` / `join`) matching the official Yggdrasil API and session server.
- **Full account system** – registration with email confirmation, login, logout, password recovery, and TOTP-based **2FA** (with backup codes).
- **User assets** – skins (64x32/64x64/512x256/512x512), avatars, and capes with upload validation.
- **Launcher server** – version management and file hosting for custom launchers.
- **Extras** – news feed, generic file hosting, per-user session management.
- **Flexible database** – MySQL, PostgreSQL, or SQLite via EF Core.
- **Multiple auth schemes** – JWT Bearer and Basic, selectable per endpoint.
- **Security defaults** – per-endpoint rate limiting, CORS policies, HTTPS with certificate support, security headers.
- **API documentation** – Swagger/OpenAPI at `/docs`.

## Documentation

| Topic | File |
| --- | --- |
| Setup & configuration | [Getting Started](docs/GETTING-STARTED.md) |
| Build & test | [Building](docs/BUILDING.md) |
| Database options | [Database](docs/DATABASE.md) |
| Game client/server integration | [Game](docs/GAME.md) |
| All endpoints | [API Reference](docs/API.md) |

## Quick Start

```bash
# Requirements: .NET 10 SDK, a database (MySQL / PostgreSQL / SQLite)
dotnet restore YggdrasilSharp.sln
dotnet run --project YggdrasilSharp
```

Copy `YggdrasilSharp/.env.example` to `YggdrasilSharp/.env` first and fill in your secrets.
Swagger UI is available at `http://localhost:5001/docs`.

Full instructions: [Getting Started](docs/GETTING-STARTED.md)

## Project Structure

```
YggdrasilSharp/            ASP.NET Core API
├── Controllers/           REST endpoints (Auth, User, Misc, Yggdrasil, Launcher)
├── Models/                DTOs, configuration, EF Core entities
├── Services/              Authentication, database, email, caching
└── Utils/                 Helpers and extensions
YggdrasilSharp.Tests/      xUnit test suite
```

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for more details.

## Contact

For issues or feature requests, use the
[GitHub issue tracker](https://github.com/TavstalDev/YggdrasilSharp/issues).