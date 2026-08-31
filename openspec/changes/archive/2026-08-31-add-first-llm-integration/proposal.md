## Why

The project now has deterministic match facts, but it lacks the first controlled LLM integration that can turn those facts into readable Danish text. This milestone establishes the probabilistic boundary in isolation and in a testable form before it is connected to upcoming matches, the scheduled flow, and `/stats`.

## What Changes

- Introduce a strongly typed `MatchBriefing` generated from supplied deterministic match facts.
- Require structured model output with a strict JSON Schema followed by validation in C#.
- Make prompt and schema versions explicit so changes can be traced and tested.
- Add a focused Microsoft Foundry integration through an Azure OpenAI-compatible `/openai/v1` endpoint, the Responses API, and the standard OpenAI .NET SDK.
- Use a configurable endpoint and deployment name together with local Microsoft Entra authentication without secrets in source code.
- Keep normal automated tests free of network calls and document an explicit opt-in smoke test against a real Foundry deployment, with environment-variable configuration for CI and a local `local.settings.json` fallback for easier debugging.
- Defer upcoming-match selection, prediction, scheduled generation, publication of `briefing.json`, display on `/stats`, advanced retry/telemetry, and evals to later milestones.

## Capabilities

### New Capabilities

- `structured-match-briefing`: Generation and validation of a structured Danish briefing for which the supplied deterministic match facts are the only factual basis.

### Modified Capabilities

No existing capabilities are modified.

## Impact

- Application/domain code gains input and output contracts, prompt construction, output validation, and a clear external LLM boundary.
- Infrastructure gains a Microsoft Foundry client and configuration for endpoint, deployment, and Entra-based authentication.
- The project gains the required official .NET packages for the OpenAI client and Azure Identity.
- Tests gain fake-based unit tests and a separate opt-in path for verifying a real model call.
- The existing Azure Function flow, static JSON assets, and `/stats` page are not affected by this milestone.
