## 1. Briefing Contract and Deterministic Input

- [x] 1.1 Add immutable records for the serializable briefing input, `MatchBriefing`, and the version-bearing result; verify with tests that a known `MatchFactsSnapshot` is mapped without new calculations or loss of relevant facts.
- [x] 1.2 Define an explicit prompt version, schema version, Danish developer instruction, and strict JSON Schema with closed properties; verify with tests that the instruction prohibits independent facts/predictions and that the schema fields match the C# contract.
- [x] 1.3 Implement C# validation of headline, summary, and key points with explicit length and count limits; verify with parameterized tests for valid values, blank text, overlong values, empty key points, and invalid counts.

## 2. Testable Briefing Orchestration

- [x] 2.1 Introduce one provider boundary for the model call and implement the briefing generator's facts → prompt/context → provider → validated result flow; verify with a fake provider that the exact versioned instruction and serialized facts are submitted and that valid output is returned with the correct versions.
- [x] 2.2 Handle provider refusal, malformed JSON, schema deviations, semantically invalid fields, and cancellation without a partial result; verify each failure path and token propagation with network-free unit tests.

## 3. Microsoft Foundry Infrastructure

- [x] 3.1 Add the official OpenAI and Azure Identity packages and strongly typed options for the `/openai/v1` endpoint and deployment name; verify restore and options tests for valid configuration, missing values, and an invalid endpoint.
- [x] 3.2 Implement the Foundry client with `DefaultAzureCredential`, the Responses API, `store=false`, strict Structured Outputs, and cancellation; verify the constructed request and error translation without a live network call.
- [x] 3.3 Register the client and generator in dependency injection without connecting them to `DailyStatsFunction`; verify that the existing Function can start/resolve without Foundry configuration while explicit resolution of the briefing flow produces a clear configuration error.

## 4. Local Learning and Live Smoke Check

- [x] 4.1 Update the example configuration and README with the Foundry resource/deployment, required Entra roles, `az login`, variable names, and an explanation of facts → prompt → schema → validation; verify that the documentation contains no secrets and can be followed from a new local shell.
- [x] 4.2 Add an xUnit smoke test with an `Integration` trait, a fixed non-sensitive facts fixture, and opt-in through `RUN_FOUNDRY_SMOKE_TEST=true`; verify that it makes zero network calls without opt-in and can be targeted with the documented test filter.
- [x] 4.3 Run the smoke test explicitly against the selected Foundry development deployment and verify a schema- and C#-validated Danish `MatchBriefingResult`; if the deployment or access has not yet been created, leave the task open and document the specific external prerequisite.
  - Verified locally on 2026-08-31 against a `gpt-5.4-mini` version `2026-03-17` Data Zone Standard deployment. The opt-in test returned a Danish briefing that passed strict structured output handling, deserialization, and C# application validation with prompt/schema version `v1`.
- [x] 4.4 Let the smoke test fall back to the `Values` object in local `local.settings.json` while retaining process environment precedence; verify fallback, precedence, and missing-file behavior with network-free tests.
- [x] 4.5 Update example configuration and README instructions for local smoke-test debugging, including the retained explicit opt-in and model-cost warning.

## 5. Overall Validation

- [x] 5.1 Run `dotnet restore`, `dotnet build`, and the normal `dotnet test` without smoke opt-in; verify that all commands succeed and that no test makes an external model call.
- [x] 5.2 Run `openspec validate add-first-llm-integration --strict` and verify that implementation, tests, and documentation continue to keep upcoming matches, prediction, the scheduled flow, `briefing.json`, and `/stats` integration outside this milestone.
- [x] 5.3 Run focused tests, the normal test suite without smoke opt-in, and strict OpenSpec validation after adding the local settings fallback.
