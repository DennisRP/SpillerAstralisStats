## Context

See [proposal.md](proposal.md) for the motivation. The application already has a deterministic historical `MatchFactsSnapshot` and a validated model boundary, but it has no representation of an upcoming target or a record of the exact evidence used for a generated briefing. PandaScore ingestion currently filters future matches out of its only result collection.

## Goals / Non-Goals

**Goals:**

- Make the retrieval step inspectable: given the same records and reference time, it produces the same target and evidence.
- Keep future-match candidates distinct from historical matches so existing static statistics remain historical-only.
- Preserve enough provenance to reproduce and later evaluate a briefing without introducing persistence.

**Non-Goals:**

- This change does not connect grounding or generation to `DailyStatsFunction`, publish `briefing.json`, or alter the static page.
- It does not add external knowledge, web retrieval, embeddings, a database, model retries, or automated semantic evaluation.
- It does not ask the model to select the target, retrieve evidence, calculate facts, or predict an outcome.

## Decisions

### 1. Separate upcoming candidates from historical ingestion output

The ingestion result keeps its existing historical `Matches` collection and adds a separate upcoming-candidates collection. The same PandaScore response is partitioned using the injected run time. Historical records remain governed by the configured lookback window. An upcoming candidate must be scheduled at or after that run time, have provider status `not_started`, identify Astralis team ID `3209`, and identify exactly one other opponent. Future records that do not meet those rules belong to neither collection.

This protects the current static publisher, which continues receiving only historical data. Passing one mixed collection to the publisher was rejected because an upcoming opponent could affect its opponent list even though completed-match calculation filters it later.

### 2. Grounding is a deterministic application service with an explicit reference time

A focused grounding builder accepts historical records, upcoming candidates, a positive recent-match count, and a reference time. It rechecks candidate eligibility against that reference time, selects the target with documented ordering, and uses the existing deterministic facts calculator for that target opponent. A non-positive recent-match count is rejected before a context is produced. The builder returns an explicit no-result outcome when no eligible target exists.

Passing the raw provider payload to the model was rejected because it obscures selection logic and invites the model to compute facts. Selecting by whichever provider record appears first was rejected because it is not reproducible or explainable.

A small application orchestration method composes grounding preparation with the existing briefing generator. It calls the model only when the builder returns a complete context; a no-target result therefore cannot accidentally become an empty or ungrounded model request.

### 3. The grounding contract is the briefing input and provenance record

A serializable grounded context contains: a version identifier; the selected target's identifying, scheduling, opponent, and available competition data; the calculated historical evidence; the reference time; and each unique provider ID represented in that evidence ordered numerically ascending. The briefing generator serializes the complete contract as its factual input and returns the same contract instance or value unchanged in its result.

Keeping provenance only in logs was rejected because it would be difficult for a later publisher or evaluation to associate the facts with one briefing. Splitting audit metadata from the model payload was also rejected for this milestone: using one complete record makes the exact submitted input directly inspectable and avoids two similar contracts drifting apart. Adding a database now was rejected because the static output and later evaluation needs do not yet require persistence.

### 4. Test the grounding seam before live-model behavior

Fixture-based tests cover selection ordering, equal-time tie-breaking, `not_started` and participant eligibility, the inclusive reference-time boundary, invalid recent-match counts, absent targets, no head-to-head history, unique ordered evidence identifiers, and exact serialization sent to a fake model client. Existing tests remain network-free; the optional Foundry smoke check is updated only as necessary to use a fixed grounded fixture.

This sequence teaches a useful AI-engineering practice: first prove retrieval and evidence deterministically, then observe model behavior with the exact evidence already visible.

## Risks / Trade-offs

- [PandaScore can reschedule a match or omit status, opponent, or competition data] → Selection requires explicit `not_started` status and complete participant identity, while preserving optional competition details without inference.
- [A target may have no previous meetings] → Grounding remains valid with explicitly empty history, and the prompt requires the model to omit unsupported comparison claims.
- [Provenance IDs alone cannot recreate changed provider records later] → The full serialized evidence remains in the in-memory result for this milestone; durable replay is deferred until a concrete publishing or evaluation need exists.
- [An upcoming candidate can become stale between ingestion and future publication] → This milestone does not publish output; the later scheduled-generation design must define freshness and regeneration behavior.

## Migration Plan

1. Extend ingestion results while keeping the existing historical collection unchanged for current callers.
2. Add grounding contracts and deterministic builder with unit tests using internal records.
3. Change the briefing input/result flow to use the grounded context and update fake-based tests and the opt-in smoke fixture.
4. Run normal tests without model access, then use the existing opt-in smoke path to inspect one grounded Danish briefing.
5. Roll back by leaving the existing historical ingestion and static publisher path in place and removing the unused grounding flow; no generated static asset or persistent data requires migration.
