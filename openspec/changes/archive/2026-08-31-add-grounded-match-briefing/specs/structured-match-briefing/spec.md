## MODIFIED Requirements

### Requirement: Briefing uses supplied facts as its only factual source
The system SHALL generate the briefing using a supplied deterministic grounding record as its only factual basis. The complete serialized record, including its target, historical evidence, grounding version, reference time, and evidence identifiers, SHALL be the factual data submitted to the model. The model instruction MUST prohibit invented numbers, facts that were not supplied, predictions, and selection of an expected winner.

#### Scenario: Deterministic facts are submitted
- **WHEN** a valid grounding record containing deterministic match facts is submitted to the briefing generator
- **THEN** the model receives that complete serialized record together with an instruction to describe only its supplied target and historical evidence

#### Scenario: No evidence is available for a claim
- **WHEN** the supplied grounding does not support a particular conclusion
- **THEN** the model is instructed to omit the conclusion instead of filling it in with its own knowledge or guesses

### Requirement: Accepted briefing has a stable structured contract
The system SHALL accept only a structured briefing result with a non-empty Danish headline, a non-empty Danish summary, and a bounded list of non-empty key points. The result SHALL identify the prompt version and output schema version used and SHALL retain the supplied grounding record unchanged so later consumers can identify the exact factual context used for generation.

#### Scenario: Model returns a valid structured briefing
- **WHEN** the model returns all required fields with values within the defined limits for a valid grounded context
- **THEN** the system returns the strongly typed briefing, its prompt and schema versions, and the unchanged grounding record

#### Scenario: Model returns additional or missing fields
- **WHEN** the model output differs from the permitted structure
- **THEN** the output is rejected as invalid
