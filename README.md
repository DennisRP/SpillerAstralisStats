# SpillerAstralisStats

An Azure Functions application for the future SpillerAstralisStats statistics pipeline.

## Layout

- `SpillerAstralisStats.sln` – root solution
- `src/SpillerAstralisStats/` – .NET 8 isolated Azure Function App
- `src/SpillerAstralisStats.Tests/` – xUnit tests

The timer function logs `Hello World`, retrieves historical PandaScore matches, and generates static statistics data using `0 37 13 * * *` and on host startup.

## Local development

Install the .NET 8 SDK, Azure Functions Core Tools v4, and Azurite. Copy `src/SpillerAstralisStats/local.settings.json.example` to `src/SpillerAstralisStats/local.settings.json`, then start Azurite before running the Functions host.

Set `PandaScore__ApiKey` in the local settings file (or as a deployed Function App setting). `PandaScore__BaseUrl` defaults to `https://api.pandascore.co/`, and `PandaScore__HistoryLookbackMonths` defaults to `12` and accepts values from `6` through `12`. The ingestion uses PandaScore team ID `3209` and does not persist data yet.

Static data is generated under `dist/stats/data` by default. When running through Azure Functions Core Tools, the local settings use `../../../../../dist/stats/data` because the worker's current directory is `bin/Debug/net8.0`; this makes the generated files land in the repository's `dist/stats/data` directory used by the Python preview. Override the path with `StaticStats__OutputPath` and the recent-match count with `StaticStats__RecentMatchCount`.

The static stats page lives under `dist/stats` and is the deployment handoff for the future `/stats` path. It has no frontend build step and reads only the generated files in `dist/stats/data`. To preview the complete static output locally, generate the data and serve the `dist` directory, for example:

```powershell
python -m http.server 8080 --directory dist
```

Then open `http://localhost:8080/stats/`. The existing root site remains outside this directory; Azure Static Web App composition and deployment automation will be added separately.

```powershell
dotnet restore SpillerAstralisStats.sln
dotnet build SpillerAstralisStats.sln
dotnet test SpillerAstralisStats.sln
func start --script-root src/SpillerAstralisStats
```

`local.settings.json` is local-only and ignored by Git. Do not commit secrets.
