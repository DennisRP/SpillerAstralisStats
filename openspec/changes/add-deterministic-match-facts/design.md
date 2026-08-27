## Context

The ingestion boundary currently returns immutable, provider-independent `MatchRecord` values containing match times, participants, scores, games, and competition details. The next pipeline stages need stable facts rather than provider DTOs, but no static-output or LLM boundary exists yet. See `proposal.md` for motivation and `specs/deterministic-match-facts/spec.md` for required behavior.

## Goals / Non-Goals

**Goals:**

- Keep all fact selection and calculation deterministic, in memory, and independently testable.
- Produce strongly typed immutable facts that can later be serialized or supplied as grounded LLM context without recalculation.
- Make incomplete provider data explicit and prevent unknown values from silently becoming wins, losses, zero scores, or empty competition names.
- Keep the implementation proportional to the current single-team use case.

**Non-Goals:**

- Select or retrieve the next upcoming match.
- Change PandaScore retrieval, DTO mapping, or historical-window behavior.
- Generate JSON, HTML, prompts, or natural-language summaries.
- Add storage, vector retrieval, map-level statistics, or a generic statistics framework.

## Decisions

### Use one cohesive deterministic calculator over domain records

Add a small application/domain service that accepts the existing match collection plus a request containing recent-match count and opponent team ID, and returns one immutable facts snapshot. Keep Astralis team ID `3209` explicit as the subject of this application.

The calculator will have no external dependencies and therefore does not need an interface or dependency-injection registration for this change. Splitting each metric into a separate service or introducing a provider abstraction is rejected because all metrics share the same eligibility, ordering, and outcome rules and currently have one implementation.

### Normalize eligible matches once

At the start of a calculation, filter to finished matches that contain Astralis, another opponent, and a relevant timestamp. Deduplicate by provider ID and order by `BeginAt ?? ScheduledAt` descending, then provider ID descending. All recent-form and head-to-head calculations consume this normalized sequence so they cannot drift in filtering or ordering behavior.

When duplicate provider IDs contain different values, retain the first record from the supplied collection. Ingestion already deduplicates provider pages; defining stable first-record behavior keeps this boundary deterministic without inventing reconciliation logic.

### Represent classified outcomes and missing facts explicitly

Use an outcome value with `Win`, `Loss`, and `Unknown`. Winner IDs that do not identify a listed participant remain unknown. Unknown outcomes are retained in recent form and head-to-head counts but excluded from the win-percentage denominator.

Expose win percentage as a nullable decimal from 0 through 100, rounded to one decimal using an explicit rounding rule. It is null when there are no decided outcomes. Scores, sweep state, and competition details remain nullable when input data is incomplete.

Using zero as a substitute for unknown values is rejected because later static output and LLM context must distinguish “zero” from “not supplied.”

### Keep facts provider-independent and serialization-ready

Add immutable records for the aggregate snapshot, recent form, head-to-head, and individual match facts. They retain provider IDs only as provenance and use existing domain competition models where that keeps the representation clear. The facts do not expose PandaScore DTO types and contain no formatting-specific strings beyond names already supplied by the domain records.

Static JSON contracts are deferred to the next milestone. This avoids prematurely coupling calculation models to a public asset schema while still making the records straightforward to serialize later.

### Derive scores and sweeps only from complete participant results

Match scores are derived only when score entries identify Astralis and the selected opponent. A sweep is true when a decided match has complete participant scores and the losing participant has zero while the winner has a positive score; it is false when complete scores show a non-zero losing score, and unavailable when the required scores or winner are missing.

Map names and round scores are not derivable from the current endpoint and are deliberately absent.

## Risks / Trade-offs

- [PandaScore can mark a finished match without a usable winner] → Preserve the match with an `Unknown` outcome and exclude it from win-percentage denominators.
- [The team endpoint is expected to return Astralis matches but malformed records may not contain Astralis] → Apply explicit participant validation before calculation.
- [First-record deduplication does not reconcile conflicting duplicates] → Rely on ingestion as the authoritative reconciliation boundary and keep calculation deterministic.
- [A fixed Astralis team ID reduces reuse] → Accept the intentional product scope and avoid a generic multi-team statistics abstraction until a concrete use case exists.
- [Upcoming opponent selection is required by the eventual stats page] → Keep opponent ID as explicit calculation input and introduce upcoming-match selection in a later focused change.
