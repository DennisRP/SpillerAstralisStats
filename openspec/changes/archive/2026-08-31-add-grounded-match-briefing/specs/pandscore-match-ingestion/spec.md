## MODIFIED Requirements

### Requirement: Ingestion excludes non-historical matches
The application SHALL return records within the configured historical window that are not eligible upcoming candidates as its historical collection. It SHALL retain a record in a separate upcoming-candidates collection only when its scheduled time is at or after the ingestion run time, its provider status is `not_started`, it identifies Astralis team ID `3209`, and it identifies exactly one other opponent. It SHALL NOT place an upcoming candidate in the historical collection.

#### Scenario: Upcoming match appears in the provider response
- **WHEN** PandaScore returns a `not_started` Astralis match scheduled at or after the ingestion run with exactly one other opponent
- **THEN** the match is retained only in the separate upcoming-candidates collection and excluded from the historical collection

#### Scenario: Historical match appears in the provider response
- **WHEN** PandaScore returns a match whose relevant time is within the configured historical window
- **THEN** the match is retained in the historical collection unless it satisfies every upcoming-candidate rule

#### Scenario: Future record is not an upcoming candidate
- **WHEN** a future record does not have status `not_started` or does not identify Astralis and exactly one other opponent
- **THEN** the record is excluded from both the historical and upcoming-candidates collections
