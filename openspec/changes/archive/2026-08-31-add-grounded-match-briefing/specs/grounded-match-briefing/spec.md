## Purpose

Builds a deterministic, auditable evidence bundle for the next Astralis match so a language-model briefing can be grounded in supplied match data rather than model knowledge.

## ADDED Requirements

### Requirement: One upcoming briefing target is selected deterministically
The application SHALL select the next eligible upcoming Astralis match from supplied upcoming candidates. A candidate is eligible only when it has a scheduled time at or after the selection reference time, has provider status `not_started`, identifies Astralis team ID `3209`, and identifies exactly one other opponent. It SHALL select the earliest scheduled candidate, resolving an equal scheduled time by ascending provider match ID.

#### Scenario: Multiple eligible upcoming matches exist
- **WHEN** supplied candidates contain more than one eligible upcoming Astralis match
- **THEN** the grounding result identifies exactly the deterministically selected next match and its opponent

#### Scenario: No eligible upcoming match exists
- **WHEN** supplied candidates are empty or none satisfy the eligibility rules
- **THEN** the application returns no grounding result and does not request a model briefing

#### Scenario: Future record is not an eligible target
- **WHEN** a future record is finished, canceled, has another status, omits its status, or does not identify the required participants
- **THEN** the record is ignored during target selection

### Requirement: Grounding evidence is calculated only from historical match facts
The application SHALL build the selected match's evidence from supplied finished historical Astralis matches only. It SHALL include the target's available competition details, the requested recent form, head-to-head summary against the target opponent, and latest previous meeting when available, using deterministic calculation.

#### Scenario: Historical evidence exists
- **WHEN** an eligible target and eligible historical matches are supplied
- **THEN** the evidence contains deterministic recent-form and head-to-head facts for the selected opponent

#### Scenario: No previous meeting exists
- **WHEN** an eligible target has no eligible historical meeting with its opponent
- **THEN** the evidence identifies the target and exposes an empty head-to-head history without inventing prior results

#### Scenario: Invalid recent-match count
- **WHEN** the requested recent-match count is not positive
- **THEN** grounding is rejected without producing a partial context

### Requirement: Grounding provenance identifies the supplied evidence
The application SHALL retain a serializable grounding record containing the selected target match identifier, the selection reference time, and each unique provider identifier represented in the supplied historical evidence, ordered by provider identifier ascending. The record SHALL retain an explicit grounding format version.

#### Scenario: A briefing context is prepared
- **WHEN** the application prepares grounding for an eligible target
- **THEN** the resulting context identifies the target and contains a stable, duplicate-free list covering all historical evidence that can be supplied to the model

### Requirement: Grounding preparation has no external side effects
The application SHALL prepare grounding from supplied internal match records and SHALL NOT call PandaScore, invoke a language model, persist data, or publish static assets.

#### Scenario: Grounding is prepared
- **WHEN** a caller requests grounding from in-memory historical and upcoming match collections
- **THEN** the result is produced without external I/O or probabilistic computation
