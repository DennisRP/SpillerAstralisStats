## Context

See [proposal.md](proposal.md) for the motivation. The project already has `MatchFactsSnapshot`, which is calculated deterministically from completed PandaScore matches. There is currently no LLM dependency, upcoming-match context, or publication path for AI output.

As the first LLM milestone, the integration must be easy to understand: deterministic facts go in, one external model returns structured text, and the application validates the result. The existing .NET 8 Azure Functions isolated worker and project structure are retained.

## Goals / Non-Goals

**Goals:**

- Make the boundary between deterministic code and the probabilistic model clear in types and flow.
- Produce a small, versioned, validated briefing object in Danish.
- Make all application behavior locally testable with a fake provider.
- Give the developer a separate, deliberate path for observing one real Foundry call from facts to object.

**Non-Goals:**

- The generator does not select an upcoming match and does not yet receive an upcoming match as its target.
- It is not connected to `DailyStatsFunction`, static publication, or `/stats`.
- The model does not calculate facts, make predictions, or return a win probability.
- No agents, tools, RAG, embeddings, vector store, or database are introduced.
- Production retry, detailed telemetry, caching, grounding evals, and Proxmox deployment identity belong to later milestones.

## Decisions

### 1. The flow is divided at one genuine external boundary

Application code owns the briefing input, output, prompt construction, and validation. A small interface represents the model call itself, while the Foundry implementation is placed under Infrastructure. The existing `MatchFactsSnapshot` is mapped to dedicated, serializable briefing input so internal facts can evolve without turning the provider format into the domain model.

The flow is:

```text
MatchFactsSnapshot
  -> versioned briefing context and instruction
  -> IMatchBriefingClient
  -> raw structured model result
  -> deserialization and C# validation
  -> MatchBriefingResult
```

The interface is justified because the model provider is an external, probabilistic, network-dependent boundary. No interfaces are created for the prompt builder, validator, or ordinary records.

The alternative was to call the SDK directly from the Function trigger. This was rejected because it would mix prompting, authentication, and validation into host orchestration and make normal tests dependent on provider behavior.

### 2. The output contract remains small and versioned

`MatchBriefing` consists of:

- `Headline`: a short Danish headline.
- `Summary`: a short Danish summary of the supplied historical facts.
- `KeyPoints`: a bounded list of short Danish key points.

A `MatchBriefingResult` associates the briefing with `PromptVersion` and `SchemaVersion`. The versions are set by the application, not by the model. The prompt, JSON Schema, and C# rules receive explicit constants so a later change is visible and can be evaluated as a new version.

JSON Schema with strict structured output enforces shape, required properties, primitive types, and the absence of additional properties. C# validation additionally enforces non-blank text, maximum lengths, and a bounded number of key points. The schema reduces invalid output but does not replace application validation.

The alternative was free text or JSON deserialized without a schema. This was rejected because errors would only be detected late and downstream code could not rely on the contract.

### 3. The prompt receives facts as data, not as values for the model to calculate

A versioned developer instruction establishes Danish as the output language, defines the editorial role, prohibits independent facts and predictions, and requires unsupported conclusions to be omitted. The strongly typed briefing input is serialized separately as user content. The model receives no tools, web access, or hidden retrieval.

Numbers, outcomes, dates, and head-to-head values therefore come from the calculator. The model selects only wording and prioritization within the supplied evidence. Unit tests inspect the context passed to the provider boundary; semantic grounding is measured more systematically in milestone 8.

The alternative was to send raw PandaScore data and ask the model to calculate the statistics. This conflicts with the project's deterministic-facts principle and was rejected.

### 4. Microsoft Foundry is used through the OpenAI-compatible `/openai/v1` endpoint

Infrastructure uses the official OpenAI .NET SDK against the configured Azure OpenAI-compatible `/openai/v1` endpoint. The call uses the Responses API, `store=false`, strict Structured Outputs, and a configurable deployment name. Specific model names are not hardcoded; a small model such as `gpt-5-mini` can be selected as the deployment if it is available in the subscription's region and supports the contract.

Local authentication uses `DefaultAzureCredential` and a Microsoft Entra token for the Foundry scope `https://ai.azure.com/.default`. Endpoint and deployment are bound to strongly typed options and validated before use. Secrets are not placed in the repository or example configuration.

The full Foundry SDK and Agent Framework were rejected because this is one stateless model call without agent functionality. The older Azure AI Model Inference path was also rejected in favor of the current OpenAI-compatible API.

### 5. Live verification is opt-in and separate from normal testing

All normal tests use a fake `IMatchBriefingClient` and verify mapping, version metadata, validation, provider failures, and cancellation without a network. A small smoke-test entry point is activated only explicitly and documented with prerequisites such as a Foundry resource, deployment, role/access, and local `az login`.

The smoke check is implemented as a separate xUnit test with an `Integration` trait. It returns without a network call unless `RUN_FOUNDRY_SMOKE_TEST=true` is explicitly configured; it can then be run with a test filter. For CI and shell-driven use, process environment values are supported and take precedence. For local debugging, the test falls back to the `Values` object in the ignored Azure Functions `local.settings.json` copied to the test output. The test uses a fixed, non-sensitive facts fixture and writes only the validated briefing result and non-sensitive diagnostics. This requires no additional application or test framework, and normal `dotnet test` remains independent of Azure when the opt-in setting is absent or false.

The alternatives were either no real verification or a live call in unit tests. The former does not show whether the endpoint, authentication, and schema actually work; the latter makes tests slow, costly, and unstable.

## Risks / Trade-offs

- [Strict Structured Outputs support can vary by model/deployment] → The deployment name is configurable, and the smoke check verifies the selected deployment's actual support before later integration.
- [Schema-valid text can still be poorly grounded] → The prompt restricts input and claims, while dedicated grounding evals are deferred to milestone 8; the output is not yet published.
- [`DefaultAzureCredential` can select different local credentials] → Documentation specifies the expected `az login`, required roles, and troubleshooting without logging tokens.
- [`local.settings.json` can enable a billable call during a broad local test run] → The example keeps the opt-in value false, documentation calls out the behavior, and environment variables remain available for short-lived overrides.
- [An additional provider interface adds some structure to a small project] → It is limited to one external boundary and enables deterministic tests without more abstraction layers.
- [Proxmox does not have Azure managed identity] → The unattended production identity will be decided in the deployment/reliability milestone; this change requires only local, interactive Entra verification.

## Migration Plan

1. Add contracts, prompt/schema, and validation without registering the generator in the existing scheduled flow.
2. Add Foundry configuration, the SDK client, and dependency injection; missing configuration must not affect the current Function because the client is resolved only when explicitly used.
3. Run deterministic tests and then the documented opt-in smoke check against a development deployment.
4. For rollback, remove the unused briefing registration and its package dependencies; existing ingestion, static assets, and the page remain unchanged.

## Open Questions

- Which specific Foundry region and model deployment will be used locally? The choice is configuration as long as the deployment supports the Responses API and strict Structured Outputs.
- Which unattended identity should Proxmox use in a later production flow: a service principal/workload identity or a securely stored API key? The decision does not affect this local milestone.
