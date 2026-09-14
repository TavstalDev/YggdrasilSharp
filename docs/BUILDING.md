# Building

## Table of Contents
- [Requirements](#requirements)
- [Build](#build)
- [Test](#test)
- [Run Locally (Development)](#run-locally-development)
- [Publish](#publish)
- [Continuous Integration](#continuous-integration)
- [Troubleshooting](#troubleshooting)

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build

```bash
dotnet restore YggdrasilSharp.sln
dotnet build YggdrasilSharp.sln --configuration Release
```

## Test

```bash
dotnet test YggdrasilSharp.sln --configuration Release
```

The test project (`YggdrasilSharp.Tests`) uses xUnit and ships in-memory/SQLite test databases, so no external services are required.

## Run Locally (Development)

```bash
dotnet run --project YggdrasilSharp
```

For a debug build the app runs in Development mode, which enables the developer exception page and allows the upload directory to be set dynamically.

## Publish

```bash
dotnet publish YggdrasilSharp/YggdrasilSharp.csproj --configuration Release --output ./out
```

## Continuous Integration

GitHub Actions runs on every push/PR to `master`:

- Restores the solution
- Builds in `Release`
- Runs the full test suite

## Troubleshooting

- **Build fails**: run `dotnet clean` followed by `dotnet restore` to clear stale artifacts.
- **ImageSharp license warning**: the project uses [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT) instead; the warning only appears if you re-add the `SixLabors.ImageSharp` package.