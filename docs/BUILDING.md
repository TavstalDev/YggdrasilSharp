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
dotnet run --project ./YggdrasilSharp.Tests/YggdrasilSharp.Tests.csproj --configuration Release
```

> `dotnet test` does **not** work on the .NET 10 SDK for this project. The suite uses xUnit v3 on
> Microsoft.Testing.Platform, which no longer supports the VSTest path that `dotnet test` drives, so
> the test project is an executable that must be run directly. This is what CI does as well.

The test project (`YggdrasilSharp.Tests`) uses xUnit v3 with FluentAssertions and Moq. Database
access runs entirely on EF Core's in-memory provider, so no external database is required. Upload
tests do open a connection to ClamAV at `localhost:3310` — start a local ClamAV daemon to exercise
real scanning (CI installs it for this reason); if the connection fails, the scan error is logged
and the upload is treated as clean.

## Run Locally (Development)

```bash
dotnet run --project YggdrasilSharp
```

The environment is chosen by `ASPNETCORE_ENVIRONMENT` or by the launch profile in
`Properties/launchSettings.json`, not by the build configuration — plain `dotnet run --project
YggdrasilSharp` uses the first profile, which sets `Production`. Pick the Development environment to get
the developer exception page:

```bash
dotnet run --project YggdrasilSharp --launch-profile Development
```

Note that the upload directory is resolved from `Runtime:UploadDir` (relative to the web root) in every
environment, not just Development.

## Publish

```bash
dotnet publish YggdrasilSharp/YggdrasilSharp.csproj --configuration Release --output ./out
```

## Continuous Integration

`.github/workflows/ci.yml` runs on every push/PR to `master`:

- Restores the solution
- Builds in `Release`
- Runs the full test suite

`.github/workflows/release.yml` runs on pushes to `stable` (or manually via `workflow_dispatch`). It
performs the same restore, build, and test steps, then publishes self-contained builds for
`linux-x64` and `win-x64` (without PDBs), zips each one as `YggdrasilSharp-<rid>.zip`, reads
`<Version>` from `YggdrasilSharp.csproj`, and creates a [GitHub release](https://github.com/TavstalDev/YggdrasilSharp/releases)
tagged `v<Version>` with the zips attached and a generated commit list as the release body. Bump
`<Version>` before pushing to `stable` so each push produces a distinct release tag.

## Troubleshooting

- **Build fails**: run `dotnet clean` followed by `dotnet restore` to clear stale artifacts.
- **`dotnet test` errors with "Testing with VSTest target is no longer supported"**: run the test
  project directly with `dotnet run` as shown above.
- **"Fluent Assertions is governed by the Xceed License Agreement" warning**: this is a license
  notice printed on every test run, not a failure. Fluent Assertions 8 is free for non-commercial
  use; a subscription is required for commercial use.