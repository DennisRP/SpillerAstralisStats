# SpillerAstralisStats

An Azure Functions application for the future SpillerAstralisStats statistics pipeline.

## Layout

- `SpillerAstralisStats.sln` – root solution
- `src/SpillerAstralisStats/` – .NET 8 isolated Azure Function App
- `src/SpillerAstralisStats.Tests/` – xUnit tests

The timer function currently logs `Hello World` daily using `0 37 13 * * *` and also on host startup. It does not call PandaScore yet.

## Local development

Install the .NET 8 SDK, Azure Functions Core Tools v4, and Azurite. Copy `src/SpillerAstralisStats/local.settings.json.example` to `src/SpillerAstralisStats/local.settings.json`, then start Azurite before running the Functions host.

```powershell
dotnet restore SpillerAstralisStats.sln
dotnet build SpillerAstralisStats.sln
dotnet test SpillerAstralisStats.sln
func start --script-root src/SpillerAstralisStats
```

`local.settings.json` is local-only and ignored by Git. Do not commit secrets.
