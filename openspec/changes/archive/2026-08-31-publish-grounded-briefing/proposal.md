## Why

The application can now build and validate a grounded match briefing, but that result remains an isolated learning seam. Publishing it as a static asset and displaying it on `/stats` makes the AI feature visible while proving that the site remains useful when no briefing can be produced.

## What Changes

- Extend the successful scheduled data-generation flow to attempt one grounded briefing from its retrieved historical matches and upcoming candidates.
- Publish `briefing.json` alongside the existing static data, carrying either a validated briefing with its grounding provenance or an explicit unavailable state.
- Keep historical `matches.json` and `stats.json` publishing available when no target exists, Foundry is unavailable, or the model returns unusable output.
- Display an AI Match Briefing section on the static stats page, including its loading, available, and unavailable states.
- Keep retries, detailed operational telemetry, caching, briefing history, and evals for later reliability milestones.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `static-stats-data`: Publish a coherent, provenance-bearing static briefing asset with the existing stats data.
- `static-stats-page`: Present the published briefing and clear fallback states in the `/stats` experience.

## Impact

- `DailyStatsFunction` gains a narrow orchestration step after successful PandaScore ingestion.
- Static publication gains one JSON asset and an explicit optional-AI fallback without a database or new infrastructure.
- The existing browser-only page gains briefing rendering but continues to load all content from colocated static assets.
- Tests cover successful publication, no-target/model-failure fallbacks, atomic generation behavior, and browser rendering without live PandaScore or Foundry calls.
