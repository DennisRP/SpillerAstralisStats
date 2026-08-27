## Why

The application can retrieve historical matches and calculate deterministic facts, but no stable artifact exists for a static site to consume. Generating static JSON makes the data pipeline observable and deployable without adding a database or an LLM dependency.

## What Changes

- Generate `matches.json` containing only completed, eligible historical Astralis match facts.
- Generate `stats.json` containing recent form and historical head-to-head summaries for every observed opponent.
- Publish the two files together to a configurable static data directory without exposing a mixed or partial generation.
- Invoke data generation after a successful scheduled ingestion and log a safe publication summary.
- Add serialization and file-publication tests using temporary directories.
- Exclude upcoming-match selection, HTML/CSS/JavaScript, LLM calls, persistence, and deployment automation.

## Capabilities

### New Capabilities

- `static-stats-data`: Publishes deterministic historical match and statistics JSON assets for a static consumer.

### Modified Capabilities

None.

## Impact

The Azure Function gains local filesystem output configured outside domain logic. The change uses built-in .NET JSON and filesystem APIs only; PandaScore remains the source of truth and no database or external service is added.
