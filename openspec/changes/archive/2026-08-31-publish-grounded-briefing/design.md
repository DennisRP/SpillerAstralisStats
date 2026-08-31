## Context

See [proposal.md](proposal.md). Existing static generation publishes an atomic pair of historical JSON assets, while grounded briefing generation is deliberately isolated and returns either a validated result or no result. The page currently consumes only the historical assets.

## Goals / Non-Goals

**Goals:**

- Join deterministic retrieval, grounded generation, static publication, and visitor rendering in one inspectable path.
- Treat the LLM as an optional enhancement so the historical stats product remains available during AI failure.
- Preserve the complete grounding record in the static asset for later developer inspection without exposing it in the visitor UI.

**Non-Goals:**

- No retry policy, timeout policy changes, detailed token/latency telemetry, cache, database, briefing history, or eval suite.
- No live PandaScore request from the browser and no change to the separate live-match application.

## Decisions

### 1. One versioned briefing asset carries either an available or unavailable result

`briefing.json` uses a small discriminated shape with an explicit availability state. An available value holds the validated `MatchBriefingResult`, including grounding provenance. An unavailable value holds a small safe reason category: no target or generation failed. It never stores exception messages, raw provider/model payloads, credentials, or a partial briefing.

This is preferable to omitting the file because the page can distinguish a normal absence of an upcoming match from a loading failure without guessing.

### 2. Generation failure does not roll back deterministic static data

After successful ingestion, the scheduled flow creates the briefing asset before invoking the existing atomic static publisher. A no-target or handled briefing failure becomes an unavailable asset, then `matches.json`, `stats.json`, and `briefing.json` are staged and swapped together. Filesystem failure retains the existing full generation as today.

Making an LLM outage fail the whole scheduled generation was rejected: historical data is independently valid and useful. Publishing `briefing.json` separately was rejected because it could mismatch the statistics and evidence it describes.

### 3. The page treats briefing loading as independent and optional

The browser continues to require `stats.json` and `matches.json` for its historical view. It loads `briefing.json` as a separate optional asset and renders an available or unavailable briefing state without turning a briefing problem into a complete page failure. Grounding provenance remains in the static JSON for audit/developer inspection, but the visitor UI renders only editorial fields and a safe unavailable message.

## Risks / Trade-offs

- [A real model call can add cost to each successful scheduled run] → Keep the current configured model/deployment and defer caching or rate policy to the reliability milestone.
- [Model availability can be intermittent] → Publish a clear unavailable asset and continue deterministic generation.
- [Grounding provenance in a static file is publicly accessible] → It contains only already-supported match facts and provider identifiers; do not add secrets, raw payloads, prompts, or internal error details.

## Migration Plan

1. Add the briefing asset contract and extend atomic publication to include it.
2. Wire optional grounded generation into the successful ingestion path with fake-based tests.
3. Add the page section and browser tests for available, unavailable, and missing-asset states.
4. Verify the complete static output locally without enabling the model smoke test.
5. Roll back by removing the page section and briefing asset from the atomic generation; historical assets remain independently publishable.
