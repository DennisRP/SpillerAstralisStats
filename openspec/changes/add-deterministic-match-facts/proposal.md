## Why

PandaScore ingestion now provides provider-independent historical match records, but the application cannot yet turn those records into reliable facts for the planned stats page and grounded AI briefings. Deterministic C# calculations are the next milestone because facts such as recent form, win rates, and head-to-head records must be established before static output or LLM synthesis is introduced.

## What Changes

- Add strongly typed match-fact models derived solely from internal historical match records.
- Add deterministic in-memory calculations for latest matches, recent form, wins and losses, win percentage, head-to-head history, latest meeting, series scores, sweep results, and relevant competition details when available.
- Define predictable handling for unfinished, canceled, incomplete, duplicated, and partially populated records without inventing missing facts.
- Add focused unit tests for ordering, filtering, outcome classification, aggregation, and incomplete data.
- Keep upcoming-match selection, static asset generation, persistence, and LLM integration outside this change.

## Capabilities

### New Capabilities

- `deterministic-match-facts`: Produces validated, provider-independent historical Astralis match facts through deterministic in-memory calculations.

### Modified Capabilities

None.

## Impact

The change adds domain/application models and calculation services that consume the existing `MatchRecord` collection. It adds unit tests but no external API calls, storage, deployment resources, third-party dependencies, or changes to the separate Node.js live-match application.
