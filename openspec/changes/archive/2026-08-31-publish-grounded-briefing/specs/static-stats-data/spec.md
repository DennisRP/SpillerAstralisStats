## ADDED Requirements

### Requirement: Grounded briefing is published as an optional static asset
The application SHALL publish `briefing.json` containing either a validated match briefing with its prompt version, schema version, and complete grounding record, or an explicit unavailable state. The unavailable state SHALL identify whether no eligible target was available or briefing generation failed, without exposing credentials, raw provider payloads, or model response text.

#### Scenario: A grounded briefing is generated
- **WHEN** successful ingestion yields an eligible upcoming match and the model returns a valid briefing
- **THEN** `briefing.json` contains that briefing and its unchanged grounding record

#### Scenario: No upcoming target is available
- **WHEN** successful ingestion has no eligible upcoming match
- **THEN** `briefing.json` contains an explicit unavailable state and historical statistics are still published

#### Scenario: Briefing generation fails
- **WHEN** the model is unavailable, refuses, or returns unusable output
- **THEN** `briefing.json` contains an explicit unavailable state and historical statistics are still published

### Requirement: Scheduled generation treats briefing generation as optional enhancement
The scheduled application flow SHALL attempt grounded briefing generation only after successful PandaScore ingestion. It SHALL log a safe outcome summary for briefing availability without exposing credentials, raw match payloads, or model output.

#### Scenario: Briefing is available
- **WHEN** a grounded briefing is generated after successful ingestion
- **THEN** the scheduled flow publishes it with the static data and logs that it was available

#### Scenario: Briefing is unavailable
- **WHEN** no target exists or briefing generation fails after successful ingestion
- **THEN** the scheduled flow publishes the unavailable state with the static data and completes without treating the historical-data generation as failed

## MODIFIED Requirements

### Requirement: JSON assets are published as a coherent generation
The application SHALL write `matches.json`, `stats.json`, and `briefing.json` under the configured static data directory as one generation. It SHALL not expose a newly generated asset together with an older or partial companion asset.

#### Scenario: Successful generation
- **WHEN** valid historical match input is processed
- **THEN** all three JSON assets are available together in the configured directory

#### Scenario: Generation failure
- **WHEN** serialization or filesystem publication fails before completion
- **THEN** the previously published generation remains available and the partial generation is not published
