## 1. Separate Upcoming Match Candidates

- [x] 1.1 Extend the PandaScore ingestion result so it separately retains only `not_started` Astralis matches scheduled at or after the run time with exactly one other opponent, while preserving historical `Matches` for current callers; verify historical-only, upcoming-only, exact-boundary, terminal/missing-status, incomplete-participant, and mixed provider records with unit tests.
- [x] 1.2 Keep `DailyStatsFunction` and static publication consuming only the historical collection; verify with an existing static-publisher test that an upcoming candidate cannot appear in published historical JSON.

## 2. Deterministic Grounding and Provenance

- [x] 2.1 Add immutable, serializable contracts for a briefing target, grounded historical evidence, and versioned provenance; verify serialization includes the selected target, reference time, grounding version, and unique evidence provider IDs ordered numerically ascending.
- [x] 2.2 Implement a side-effect-free grounding builder that rechecks upcoming eligibility, selects the next match using documented ordering, and constructs its facts from historical records only; verify inclusive reference-time selection, equal-time tie-breaking, status and participant filtering, invalid recent-match counts, and no-target results with unit tests.
- [x] 2.3 Cover grounding evidence for recent form, head-to-head, the latest meeting, and no previous meeting; verify all values derive from the existing deterministic facts calculator without external calls.

## 3. Grounded Briefing Flow

- [x] 3.1 Change the existing briefing input mapper and generator to accept the complete grounded context as their sole factual input and serialize that exact record for the model; verify with a fake provider that target, evidence, version, reference time, and evidence IDs are submitted without omitted or additional factual data.
- [x] 3.2 Return the unchanged grounded context with each accepted `MatchBriefingResult`; verify prompt/schema versions, briefing validation, and provenance survive a successful generation.
- [x] 3.3 Add the application orchestration seam from grounding preparation to briefing generation and preserve predictable no-result and failure behavior: no eligible target makes no provider call, while provider refusal, malformed output, validation failure, and cancellation return no partial grounded briefing; verify each path using network-free tests.

## 4. Learning-Focused Verification

- [x] 4.1 Update the opt-in Foundry smoke fixture and README to show the evidence bundle before the generated Danish briefing, explain deterministic retrieval versus model synthesis, and retain the explicit cost-bearing opt-in; verify normal tests make zero model calls.
- [x] 4.2 Add an educational, fixture-driven test that prints or asserts the serialized grounding context used by the fake provider; verify a developer can inspect the target, grounding version, reference time, ordered evidence IDs, and exact factual JSON independently of model output.

## 5. Overall Validation

- [x] 5.1 Run focused grounding and briefing tests, `dotnet build SpillerAstralisStats.sln`, and `dotnet test SpillerAstralisStats.sln` without smoke opt-in; verify all succeed without external model calls.
- [x] 5.2 Run `openspec validate add-grounded-match-briefing --strict`; verify the change remains limited to deterministic grounding and provenance, with scheduled generation, static briefing publication, page integration, reliability telemetry, and formal evals deferred.
