# static-stats-data Specification

## Purpose

Publishes deterministic historical Astralis match and statistics data as static JSON assets that a later stats page can consume without a database or live API request.

## Requirements

### Requirement: Completed historical matches are published
The application SHALL publish `matches.json` containing only finished, eligible historical Astralis match facts in newest-first order. It SHALL retain available match identifiers, timestamps, opponents, outcomes, scores, game counts, and competition details without inventing missing values.

#### Scenario: Mixed historical input
- **WHEN** ingestion supplies completed records together with unfinished, canceled, incomplete, or duplicate records
- **THEN** `matches.json` contains each eligible finished match exactly once and excludes the other records

### Requirement: Historical statistics are published
The application SHALL publish `stats.json` containing recent form and a head-to-head summary for every opponent represented by an eligible historical match. Each head-to-head summary SHALL be calculated solely from finished historical meetings with that opponent.

#### Scenario: Multiple historical opponents
- **WHEN** eligible historical matches include multiple opponents
- **THEN** `stats.json` contains a deterministic summary for each opponent without selecting or requiring a future match

#### Scenario: No eligible historical matches
- **WHEN** no eligible finished Astralis matches are available
- **THEN** both JSON files are published with valid empty collections and unavailable aggregate values where appropriate

### Requirement: JSON assets are published as a coherent generation
The application SHALL write `matches.json` and `stats.json` under the configured static data directory as one generation. It SHALL not expose one newly generated asset together with an older or partial companion asset.

#### Scenario: Successful generation
- **WHEN** valid historical match input is processed
- **THEN** both JSON assets are available together in the configured directory

#### Scenario: Generation failure
- **WHEN** serialization or filesystem publication fails before completion
- **THEN** the previously published generation remains available and the partial generation is not published

### Requirement: Scheduled ingestion publishes only after success
The scheduled application flow SHALL generate and publish static data only after successful PandaScore ingestion. It SHALL log a safe summary containing the published match and opponent-summary counts.

#### Scenario: Successful ingestion
- **WHEN** historical PandaScore ingestion succeeds
- **THEN** the application publishes static data and logs its summary without credentials or raw payload data

#### Scenario: Ingestion failure
- **WHEN** PandaScore ingestion fails
- **THEN** the application does not replace existing static assets
