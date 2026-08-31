# SpillerAstralisStats

An Azure Functions application for the future SpillerAstralisStats statistics pipeline.

## Layout

- `SpillerAstralisStats.sln` – root solution
- `src/SpillerAstralisStats/` – .NET 8 isolated Azure Function App
- `src/SpillerAstralisStats.Tests/` – xUnit tests

The timer function logs `Hello World`, retrieves historical PandaScore matches, and generates static statistics data using `0 37 13 * * *` and on host startup.

## Local development

Install the .NET 8 SDK, Azure Functions Core Tools v4, and Azurite. Copy `src/SpillerAstralisStats/local.settings-example.json` to `src/SpillerAstralisStats/local.settings.json`, then start Azurite before running the Functions host.

Set `PandaScore__ApiKey` in the local settings file (or as a deployed Function App setting). `PandaScore__BaseUrl` defaults to `https://api.pandascore.co/`, and `PandaScore__HistoryLookbackMonths` defaults to `12` and accepts values from `6` through `12`. The ingestion uses PandaScore team ID `3209` and does not persist data yet.

Static data is generated under `dist/stats/data` by default. When running through Azure Functions Core Tools, the local settings use `../../../../../dist/stats/data` because the worker's current directory is `bin/Debug/net8.0`; this makes the generated files land in the repository's `dist/stats/data` directory used by the Python preview. Override the path with `StaticStats__OutputPath` and the recent-match count with `StaticStats__RecentMatchCount`.

The static stats page lives under `dist/stats` and is the deployment handoff for the future `/stats` path. It has no frontend build step and reads only the generated files in `dist/stats/data`. To preview the complete static output locally, generate the data and serve the `dist` directory, for example:

```powershell
python -m http.server 8080 --directory dist
```

Then open `http://localhost:8080/stats/`. The existing root site remains outside this directory; Azure Static Web App composition and deployment automation will be added separately.

## First LLM integration

The first LLM milestone is deliberately isolated from `DailyStatsFunction`, static data generation, and `/stats`. It proves this flow:

```text
deterministic MatchFactsSnapshot
    -> serializable facts JSON
    -> versioned editorial prompt
    -> Microsoft Foundry Responses API
    -> strict MatchBriefing JSON Schema
    -> C# deserialization and application validation
    -> MatchBriefingResult
```

These stages have different responsibilities:

- The deterministic C# calculator owns numbers, outcomes, scores, dates, recent form, and head-to-head facts.
- The prompt tells the model to write in Danish, use only supplied facts, omit unsupported conclusions, and avoid predictions.
- Strict Structured Outputs constrain the JSON shape to `headline`, `summary`, and `keyPoints` with no additional properties.
- C# validation rejects blank or overlong text and an invalid number of key points even when the JSON shape is correct.

The model therefore chooses wording and emphasis; it does not calculate statistics. Prompt version `match-briefing-prompt-v1` and schema version `match-briefing-schema-v1` are attached by the application after validation.

## Grounded match briefing

The next milestone adds the retrieval and evidence-selection step before the existing model call:

```text
PandaScore match response
    -> historical matches + eligible upcoming candidates
    -> deterministic selection of the next Astralis match
    -> recent form + head-to-head facts from historical matches only
    -> versioned grounding record with target, evidence, reference time, and evidence IDs
    -> Microsoft Foundry
    -> validated Danish MatchBriefingResult with the same grounding record
```

This is a useful AI-engineering boundary. C# selects the target and calculates every statistic. The model receives the complete serialized grounding record and can only choose wording and emphasis from its supplied target and historical evidence. It must not add outside facts, calculate statistics, or predict a winner.

The grounding record is returned unchanged with the briefing. It contains the selected target, the historical evidence, a grounding version, the reference time, and unique evidence match IDs in ascending order. That means a developer can inspect exactly what the model was given before judging whether its wording is grounded.

Only a `not_started` Astralis match with a scheduled time at or after the selection time and exactly one opponent is eligible. Historical static data continues to use the separate historical collection, so future matches do not appear in `matches.json` or influence the published statistics.

## Published briefing asset

The successful scheduled flow now joins the deterministic and AI steps in one static generation:

```text
successful PandaScore ingestion
    -> historical matches + upcoming candidates
    -> deterministic grounding selection and facts
    -> optional validated model briefing
    -> matches.json + stats.json + briefing.json published together
    -> /stats renders historical statistics and the editorial briefing
```

`briefing.json` is an inspectable developer artifact. It has either an `available` state with the validated briefing, prompt/schema versions, and full grounding record, or an `unavailable` state with the safe reason `noEligibleTarget` or `generationFailed`. The browser shows only the headline, summary, and key points; it intentionally does not display grounding identifiers or failure details.

This split is intentional: you can open `dist/stats/data/briefing.json` to audit exactly which deterministic facts grounded an available briefing, while a model outage still produces a complete historical statistics generation and a clear visitor fallback. The file never contains credentials, provider payloads, raw model responses, or exception text.

### Configure Microsoft Foundry locally

1. Create or select a Microsoft Foundry/Azure OpenAI resource and deploy a model that supports the Responses API and strict Structured Outputs. The deployment name is configuration and is not hardcoded.
2. On the Azure OpenAI resource, grant your developer identity the `Cognitive Services OpenAI User` role. `Cognitive Services OpenAI Contributor` also permits inference but grants broader access.
3. Install the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows) if needed, then sign in locally:

   ```powershell
   az login
   ```

4. Add these non-secret settings to `src/SpillerAstralisStats/local.settings.json`:

   ```json
   {
     "Foundry__Endpoint": "https://<resource-name>.openai.azure.com/openai/v1/",
     "Foundry__DeploymentName": "<deployment-name>"
   }
   ```

The endpoint must be an absolute HTTPS URL ending in `/openai/v1/`. The client uses `DefaultAzureCredential` and the Foundry scope `https://ai.azure.com/.default`; no API key is read or stored. Role assignments can take a few minutes to become effective.

The live smoke test is opt-in and uses a fixed, non-sensitive grounded fixture. It writes the selected target, grounding version, ordered evidence IDs, and the validated Danish briefing to the test output. For a one-off shell run, set process environment variables:

```powershell
$env:Foundry__Endpoint = "https://<resource-name>.openai.azure.com/openai/v1/"
$env:Foundry__DeploymentName = "<deployment-name>"
$env:RUN_FOUNDRY_SMOKE_TEST = "true"
dotnet test src/SpillerAstralisStats.Tests/SpillerAstralisStats.Tests.csproj --filter "Category=Integration"
Remove-Item Env:RUN_FOUNDRY_SMOKE_TEST, Env:Foundry__Endpoint, Env:Foundry__DeploymentName
```

For debugging from an IDE without setting environment variables first, set `RUN_FOUNDRY_SMOKE_TEST` to `true` in the ignored `src/SpillerAstralisStats/local.settings.json` file alongside `Foundry__Endpoint` and `Foundry__DeploymentName`, then debug the smoke test from the test explorer. The test reads the `Values` object from the copy of that file in its output directory. Process environment variables take precedence when both sources contain a value.

Without `RUN_FOUNDRY_SMOKE_TEST=true` in either source, the test returns before resolving the Foundry client and makes no network call. Normal `dotnet test` therefore does not require Azure access or generate model cost. Remember to set the local opt-in back to `false` after debugging; leaving it enabled makes every matching local test run call the model. See the Microsoft documentation for the [Responses API](https://learn.microsoft.com/en-us/azure/foundry/openai/how-to/responses) and [Entra role setup](https://learn.microsoft.com/en-us/azure/foundry-classic/openai/how-to/managed-identity).

```powershell
dotnet restore SpillerAstralisStats.sln
dotnet build SpillerAstralisStats.sln
dotnet test SpillerAstralisStats.sln
func start --script-root src/SpillerAstralisStats
```

`local.settings.json` is local-only and ignored by Git. Do not commit secrets.
