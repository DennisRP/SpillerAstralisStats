## Context

The application already retrieves historical PandaScore records and calculates deterministic facts, but the current scheduled function only logs the ingestion outcome. Static consumers need stable JSON artifacts; see `proposal.md` and `specs/static-stats-data/spec.md` for behavior.

## Goals / Non-Goals

**Goals:**

- Generate `dist/stats/data/matches.json` and `dist/stats/data/stats.json` by default, with the output root configurable.
- Keep serialization, filesystem publication, and function orchestration outside deterministic calculation logic.
- Publish a complete replacement generation or retain the previous one on failure.

**Non-Goals:**

- Add a browser page, future-match selection, deployment, database, or LLM call.
- Change the PandaScore retrieval contract or infer unavailable match facts.

## Decisions

### Build an explicit static-data snapshot

Add a small application service that receives successful historical `MatchRecord` values, creates the recent form plus a head-to-head summary for each distinct historical opponent, and maps the result to serialization-specific immutable DTOs. This keeps `stats.json` independent of a single future opponent while making later selection a simple lookup.

### Publish through a staging directory

Write both assets to a unique sibling staging directory, then replace the data directory only after serialization completes. Retain the previous directory until the replacement succeeds and restore it if the swap fails. This is preferred over replacing files individually because consumers must not observe mismatched generations.

### Configure the output location at the host boundary

Bind a small options model with a default output path of `dist/stats/data`; it is injected into the publisher while calculation models remain configuration-free. The function invokes publication only after ingestion reports success.

### Use System.Text.Json with explicit options

Use camel-case JSON and omit null properties so unavailable data stays absent rather than being replaced by invented defaults. No new package is required.

## Risks / Trade-offs

- [Directory replacement can be interrupted] → Stage fully, keep the previous generation until replacement succeeds, and clean up only after success.
- [Multiple function invocations can overlap] → Keep publication scoped to one output root and defer concurrency coordination until deployment/runtime behavior requires it.
- [The output contract will evolve] → Keep static DTOs separate from domain records and add compatibility-focused serialization tests.
