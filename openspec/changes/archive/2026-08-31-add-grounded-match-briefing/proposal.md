## Why

The first LLM milestone proves that an isolated, supplied facts snapshot can become a validated Danish briefing, but it does not yet explain which facts were selected for a real match or let a developer inspect that evidence. The next milestone should teach grounded generation by making deterministic retrieval and provenance explicit before the briefing is scheduled or published.

## What Changes

- Add deterministic selection of one eligible upcoming Astralis match and its historical evidence bundle: recent form, head-to-head meetings, latest meeting, and available competition details.
- Add a versioned, serializable grounding record that identifies the selected match and every historical match supplied to the model through unique, stably ordered provider IDs.
- Extend briefing generation to accept the complete grounding record as its sole factual input, serialize that exact record for the model, and return it unchanged alongside the validated model briefing for later auditing.
- Add network-free tests that demonstrate evidence selection, exact prompt input, missing-history behavior, and provenance retention.
- Keep the scheduled Function, `briefing.json`, `/stats` rendering, production retry/telemetry, and formal evaluation suite outside this milestone.

## Capabilities

### New Capabilities

- `grounded-match-briefing`: Selects an upcoming Astralis match and an auditable, deterministic evidence bundle for a match briefing.

### Modified Capabilities

- `pandscore-match-ingestion`: Retain valid upcoming Astralis match candidates separately from the existing historical collection.
- `structured-match-briefing`: The existing validated briefing flow accepts and retains the new grounded evidence bundle as its sole factual input.

## Impact

- Application code gains deterministic upcoming-match selection, a small grounding/provenance contract, and orchestration from that contract to the existing generator.
- Existing facts calculation remains deterministic, and the Foundry provider boundary remains unchanged in responsibility; no new package, database, retrieval service, or vector search is required.
- Tests gain fixture-driven grounding cases and briefing-input assertions without live PandaScore or model calls.
- The Azure Function and static output remain unaffected, keeping this milestone a learning-focused integration seam rather than a production publishing change.
