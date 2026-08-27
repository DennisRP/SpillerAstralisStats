## Why

The scheduled Function App has a validated host but no source data. PandaScore match ingestion is the smallest next step toward deterministic Astralis statistics and later static site generation.

## What Changes

- Add a configured PandaScore client that retrieves Astralis (`3209`) match history from the team matches endpoint.
- Fetch a configurable historical window with a default of 12 months and supported values from 6 through 12 months.
- Map relevant PandaScore response data into provider-independent internal match models.
- Handle paginated API responses, authentication/configuration failures, non-success API responses, malformed payloads, and cancellation predictably.
- Invoke the ingestion operation from the existing daily timer function and log an operational summary without exposing credentials or full provider payloads.
- Add deterministic tests for request construction, pagination, mapping, and failure behavior using mocked HTTP boundaries.
- Do not calculate statistics, persist data, generate static assets, or add LLM functionality in this change.

## Capabilities

### New Capabilities

- `pandscore-match-ingestion`: Retrieves, validates, and maps recent Astralis match history from PandaScore for use by later deterministic statistics.

### Modified Capabilities

None.

## Impact

The Function App gains outbound HTTP access to PandaScore and configuration for a PandaScore API key and history lookback. The timer function retains its established schedule and startup behavior. No database, Azure service, or change to the existing Node.js live-match application is required.
