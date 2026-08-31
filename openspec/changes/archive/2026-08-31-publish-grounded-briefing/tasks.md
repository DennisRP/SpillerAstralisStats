## 1. Briefing Publication Contract

- [x] 1.1 Add a serializable `briefing.json` contract with available and unavailable states, safe reason categories, and the validated briefing plus grounding provenance when available; verify JSON serialization never contains exception text, credentials, raw payloads, or partial results.
- [x] 1.2 Extend the static publisher to stage and atomically publish `matches.json`, `stats.json`, and `briefing.json` together; verify successful output contains all three files and a write failure preserves the prior complete generation.

## 2. Scheduled Optional Briefing Generation

- [x] 2.1 Wire the successful ingestion path to create one grounded briefing request from its historical and upcoming collections, then publish the available result; verify with fakes that the target, evidence, and validated result are passed through without recalculation.
- [x] 2.2 Convert no-target and handled provider/output failures into safe unavailable briefing states while publishing historical assets normally; verify each path makes no partial model result available and does not mark the scheduled run as a historical-data failure.
- [x] 2.3 Add safe structured logging for briefing availability and reason category without model output, raw match data, credentials, or exception payloads; verify log assertions for available and unavailable cases.

## 3. Static Page Briefing Section

- [x] 3.1 Add a colocated `briefing.json` request and AI Match Briefing page section; verify an available fixture renders its headline, summary, and key points without rendering grounding IDs.
- [x] 3.2 Render clear unavailable and unreadable-asset briefing states while retaining successfully loaded historical statistics; verify browser/page tests for no-target, generation-failed, malformed, and missing briefing fixtures.

## 4. Learning-Focused Documentation and Validation

- [x] 4.1 Update the README with the complete static flow and explain how to inspect `briefing.json` provenance separately from the visitor UI; verify the example contains no secrets or internal failure payloads.
- [x] 4.2 Run focused publication, scheduled-flow, and page tests plus `dotnet build SpillerAstralisStats.sln` and `dotnet test SpillerAstralisStats.sln` without smoke opt-in; verify all succeed without live Foundry calls.
- [x] 4.3 Run `openspec validate publish-grounded-briefing --strict`; verify retries, caching, detailed telemetry, persistence, and evals remain outside this milestone.
