## Purpose

Produces deterministic, provider-independent facts from historical Astralis match records for later static statistics output and grounded language-model context.

## ADDED Requirements

### Requirement: Facts use eligible historical Astralis matches
The application SHALL calculate match facts only from records that are finished, identify Astralis team ID `3209` as an opponent, identify at least one other opponent, and have a relevant timestamp. It SHALL deduplicate eligible records by provider match ID before calculation.

#### Scenario: Mixed input records
- **WHEN** the input contains finished Astralis matches together with unfinished, canceled, unrelated, timeless, or duplicate records
- **THEN** the calculated facts include each eligible finished Astralis match exactly once and exclude the other records

### Requirement: Latest matches are selected deterministically
The application SHALL return up to the requested positive number of eligible matches ordered from newest to oldest by actual begin time when available and otherwise by scheduled time. A tie SHALL be resolved by provider match ID in descending order.

#### Scenario: More matches than requested
- **WHEN** more eligible matches exist than the requested match count
- **THEN** only the requested number of newest matches is returned in deterministic order

#### Scenario: Fewer matches than requested
- **WHEN** fewer eligible matches exist than the requested match count
- **THEN** every eligible match is returned without adding placeholder matches

#### Scenario: Invalid requested count
- **WHEN** the requested match count is not positive
- **THEN** the calculation is rejected without producing a partial facts result

### Requirement: Match outcomes are classified without inference
The application SHALL classify an eligible match as an Astralis win when its winner ID is `3209`, as a loss when its winner ID identifies another listed opponent, and as unknown when the winner is absent or does not identify a listed participant.

#### Scenario: Known winner
- **WHEN** a finished match identifies Astralis or another listed opponent as winner
- **THEN** the match is classified respectively as a win or loss

#### Scenario: Winner is unavailable
- **WHEN** a finished match has no valid participant winner ID
- **THEN** the match outcome is classified as unknown rather than inventing a result

### Requirement: Recent form aggregates decided outcomes
The application SHALL expose the ordered recent-form outcomes, win count, loss count, unknown count, and win percentage for the selected latest matches. Win percentage SHALL use only wins and losses as its denominator and SHALL be unavailable when no selected match has a decided outcome.

#### Scenario: Recent form contains decided and unknown outcomes
- **WHEN** selected matches contain wins, losses, and unknown outcomes
- **THEN** each count reflects its classified outcomes and win percentage excludes unknown outcomes from the denominator

#### Scenario: No decided recent matches
- **WHEN** every selected recent match has an unknown outcome
- **THEN** win percentage is represented as unavailable

### Requirement: Head-to-head facts use an explicit opponent
The application SHALL calculate head-to-head facts for a supplied positive opponent team ID different from `3209`. It SHALL include only eligible matches listing both Astralis and that opponent and SHALL expose meeting count, Astralis wins, opponent wins, unknown outcomes, ordered meetings, and the latest meeting when one exists.

#### Scenario: Previous meetings exist
- **WHEN** eligible input contains matches between Astralis and the supplied opponent
- **THEN** head-to-head facts contain only those meetings in newest-first order and identify the newest as the latest meeting

#### Scenario: No previous meeting exists
- **WHEN** no eligible input match lists both Astralis and the supplied opponent
- **THEN** head-to-head counts and meetings are empty and latest meeting is unavailable

#### Scenario: Invalid opponent ID
- **WHEN** the supplied opponent ID is non-positive or is Astralis team ID `3209`
- **THEN** the calculation is rejected without producing a partial facts result

### Requirement: Available match details are retained
Each returned match fact SHALL retain the provider match ID, relevant timestamp, opponent identity, classified outcome, available team scores, game count, and available league, serie, and tournament details. It SHALL identify a sweep only when complete scores show the winner conceding zero games; otherwise sweep status SHALL be unavailable or false as supported by the data.

#### Scenario: Complete score and competition data
- **WHEN** an eligible match contains participant scores and competition details
- **THEN** the match fact retains those values and derives its series score and sweep status deterministically

#### Scenario: Partial match details
- **WHEN** an eligible match omits scores, game count, or competition details
- **THEN** available facts are retained and absent facts remain unavailable without inferred replacements

### Requirement: Fact calculation has no external side effects
The application SHALL calculate facts in memory from the supplied internal match records and SHALL NOT call PandaScore, invoke a language model, persist data, or generate static assets as part of the calculation.

#### Scenario: Facts are calculated
- **WHEN** a caller requests match facts from an in-memory match collection
- **THEN** the result is produced without external I/O or probabilistic computation
