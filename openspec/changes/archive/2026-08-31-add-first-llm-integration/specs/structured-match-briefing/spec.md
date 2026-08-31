## Purpose

This capability turns supplied, deterministically calculated match facts into a validated Danish briefing without allowing the model to calculate statistics or add new facts.

## ADDED Requirements

### Requirement: Briefing uses supplied facts as its only factual source
The system SHALL generate the briefing using the supplied deterministic match facts as its only factual basis. The model instruction MUST prohibit invented numbers, facts that were not supplied, predictions, and selection of an expected winner.

#### Scenario: Deterministic facts are submitted
- **WHEN** a valid set of match facts is submitted to the briefing generator
- **THEN** the model receives those facts together with an instruction to describe only the supplied historical evidence

#### Scenario: No evidence is available for a claim
- **WHEN** the supplied facts do not support a particular conclusion
- **THEN** the model is instructed to omit the conclusion instead of filling it in with its own knowledge or guesses

### Requirement: Accepted briefing has a stable structured contract
The system SHALL accept only a structured briefing result with a non-empty Danish headline, a non-empty Danish summary, and a bounded list of non-empty key points. The result SHALL identify the prompt version and output schema version used.

#### Scenario: Model returns a valid structured briefing
- **WHEN** the model returns all required fields with values within the defined limits
- **THEN** the system returns a strongly typed briefing with the prompt and schema versions

#### Scenario: Model returns additional or missing fields
- **WHEN** the model output differs from the permitted structure
- **THEN** the output is rejected as invalid

### Requirement: Model output is validated before use
The system MUST validate both the output structure and its application rules before a briefing can be returned or later persisted.

#### Scenario: Structured output contains unusable text
- **WHEN** the output has valid JSON form but a required text field contains only whitespace or exceeds its permitted length
- **THEN** the entire output is rejected without a partial briefing result

#### Scenario: Provider refuses or returns malformed output
- **WHEN** the model provider refuses the request or returns content that cannot be interpreted as the required structure
- **THEN** generation fails predictably without a partial briefing result

### Requirement: Provider invocation is explicit and configurable
The system SHALL require a configured model endpoint and deployment name, SHALL support cancellation, and MUST NOT require hardcoded credentials or secrets in source code.

#### Scenario: Provider configuration is valid
- **WHEN** the endpoint, deployment, and a valid external identity are available
- **THEN** the system can perform a briefing call against the configured deployment

#### Scenario: Required provider configuration is missing
- **WHEN** the endpoint or deployment name is missing
- **THEN** the system reports a configuration error before attempting a model call

#### Scenario: Generation is cancelled
- **WHEN** the calling operation is cancelled
- **THEN** cancellation is propagated to the provider and no briefing is returned

### Requirement: Automated tests do not depend on a live model
The system's normal automated tests SHALL be able to verify briefing orchestration, validation, and failure paths without network access or an active model deployment. A real model call SHALL be a separate, explicit opt-in check.

#### Scenario: Normal test suite runs
- **WHEN** the normal test suite runs without Foundry configuration
- **THEN** no external model calls are made

#### Scenario: Live smoke check is explicitly enabled
- **WHEN** a developer explicitly enables the smoke check and supplies valid configuration and identity
- **THEN** the facts-to-briefing flow can be verified against the real deployment

#### Scenario: Local smoke-check configuration is available
- **WHEN** the smoke test runs locally without process environment values and `local.settings.json` contains the opt-in flag, endpoint, and deployment name under `Values`
- **THEN** the test uses those local values to decide whether to call the configured real deployment

#### Scenario: Process environment overrides local smoke-check configuration
- **WHEN** both a process environment value and a `local.settings.json` value exist for the same smoke-test setting
- **THEN** the process environment value is used
