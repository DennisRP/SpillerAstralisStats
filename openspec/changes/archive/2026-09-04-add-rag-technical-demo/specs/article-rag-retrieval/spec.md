## ADDED Requirements

### Requirement: Article RAG has a reproducible technical demonstration
The system SHALL provide one documented local demonstration that uses only committed redistributable fixtures and deterministic fake model output. The demonstration SHALL expose the source documents and derived chunk identities, retrieval query and ranked passages, exact model context, structured editorial result, cited chunk IDs, citation membership validation, retrieval metrics, and component version metadata. It MUST NOT require private articles, PandaScore, Foundry configuration, credentials, a database, or a network call.

#### Scenario: Developer runs the supported-evidence demonstration
- **WHEN** a developer runs the documented demonstration command with its default committed fixtures
- **THEN** the command completes successfully and presents the source-to-chunk, retrieval, context, cited-answer, citation-validation, and evaluation stages in a stable readable order

#### Scenario: Demonstration shows insufficient retrieval context
- **WHEN** the demonstration evaluates a question with no positive-scoring lexical evidence
- **THEN** it reports retrieval-level insufficient context and proves that generation was not invoked

#### Scenario: Demonstration shows model-selected insufficient context
- **WHEN** retrieved passages share terms with a question but do not support its requested claim
- **THEN** the deterministic fake model result reports insufficient context without editorial text or citations

#### Scenario: Demonstration shows generation failure
- **WHEN** the deterministic fake model returns an invalid or unretrieved citation in the failure example
- **THEN** the demonstration reports a generation validation failure without exposing a partial answer or treating it as a retrieval failure

#### Scenario: Live generation remains optional
- **WHEN** a developer runs the reproducible demonstration without an explicit live-service option
- **THEN** no Foundry client is resolved and no model cost or external request is incurred
