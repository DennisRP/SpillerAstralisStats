# SpillerAstralisStats

An Azure Functions application for the future SpillerAstralisStats statistics pipeline.

## Layout

- `SpillerAstralisStats.sln` – root solution
- `src/SpillerAstralisStats/` – .NET 8 isolated Azure Function App
- `src/SpillerAstralisStats.Tests/` – xUnit tests

The timer function currently logs `Hello World` daily using `0 37 13 * * *` and also on host startup. It does not call PandaScore yet.

## Local development

Install the .NET 8 SDK, Azure Functions Core Tools v4, and Azurite. Copy `src/SpillerAstralisStats/local.settings.json.example` to `src/SpillerAstralisStats/local.settings.json`, then start Azurite before running the Functions host.

Set `PandaScore__ApiKey` in the local settings file (or as a deployed Function App setting). `PandaScore__BaseUrl` defaults to `https://api.pandascore.co/`, and `PandaScore__HistoryLookbackMonths` defaults to `12` and accepts values from `6` through `12`. The ingestion uses PandaScore team ID `3209` and does not persist data yet.

```powershell
dotnet restore SpillerAstralisStats.sln
dotnet build SpillerAstralisStats.sln
dotnet test SpillerAstralisStats.sln
func start --script-root src/SpillerAstralisStats
```

`local.settings.json` is local-only and ignored by Git. Do not commit secrets.
