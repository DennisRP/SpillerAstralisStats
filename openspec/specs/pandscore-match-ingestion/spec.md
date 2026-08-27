# PandaScore Match Ingestion Specification

## Purpose

Retrieves recent Astralis CS2 match history from PandaScore as validated internal records for later deterministic statistics and static site generation.

## Requirements

### Requirement: Ingestion uses configured PandaScore credentials
The application SHALL obtain PandaScore authentication from configuration and SHALL NOT embed, expose, or log the credential.

#### Scenario: Credential is configured
- **WHEN** the scheduled ingestion starts with a configured PandaScore credential
- **THEN** it authenticates outbound requests without including the credential in logs or exception messages

#### Scenario: Credential is missing
- **WHEN** the scheduled ingestion starts without a PandaScore credential
- **THEN** it logs a configuration failure and completes without sending a PandaScore request

### Requirement: Ingestion retrieves the configured Astralis history window
The application SHALL retrieve match records for PandaScore team ID `3209` within a configurable lookback period. The lookback SHALL default to 12 months and SHALL accept whole-month values from 6 through 12 inclusive.

#### Scenario: Default history window
- **WHEN** no lookback value is configured
- **THEN** the retrieval window starts 12 calendar months before the ingestion run

#### Scenario: Configured history window
- **WHEN** a lookback value from 6 through 12 is configured
- **THEN** the retrieval window starts that number of calendar months before the ingestion run

#### Scenario: Invalid history window
- **WHEN** the configured lookback value is outside 6 through 12 or is not a whole number
- **THEN** the application logs a configuration failure and completes without sending a PandaScore request

### Requirement: Ingestion retrieves all matching pages
The application SHALL request all PandaScore result pages required to retrieve the configured history window and SHALL stop paging when the provider reports no further records.

#### Scenario: Multiple response pages
- **WHEN** matching records span more than one PandaScore response page
- **THEN** the returned internal match collection includes records from every page exactly once

### Requirement: Ingestion excludes non-historical matches
The application SHALL exclude match records whose relevant match time is after the ingestion run and SHALL only return records within the configured historical window.

#### Scenario: Upcoming match appears in the provider response
- **WHEN** PandaScore returns a match scheduled after the ingestion run
- **THEN** the match is excluded from the returned historical collection

### Requirement: Ingestion maps records into internal match data
The application SHALL map each valid PandaScore match record into a provider-independent internal model retaining the provider match ID, scheduled and actual timestamps when present, status, winner ID when present, opponents, scores, game count, league, serie, and tournament details when present.

#### Scenario: Fully populated provider record
- **WHEN** PandaScore returns a match containing the supported fields
- **THEN** the internal match record retains their corresponding values and provider match ID

#### Scenario: Partially populated provider record
- **WHEN** PandaScore omits optional match, score, opponent, game, or tournament fields
- **THEN** the valid available values are retained and the absent values are represented as unavailable without inventing data

### Requirement: Ingestion failure is observable and safe
The application SHALL log an operational summary for a successful ingestion and SHALL log a contextual failure for provider transport, non-success response, or invalid payload failures. It SHALL propagate cancellation and SHALL NOT persist partial or raw provider data.

#### Scenario: Successful ingestion
- **WHEN** PandaScore returns valid match data
- **THEN** the application logs the retrieved and mapped match count for the configured window

#### Scenario: Provider response failure
- **WHEN** PandaScore returns a non-success response or malformed response body
- **THEN** the application logs the failure category and completes without publishing partial match data

#### Scenario: Cancellation request
- **WHEN** the Function host cancels the ingestion operation
- **THEN** the cancellation is propagated without being translated into a successful ingestion
