## ADDED Requirements

### Requirement: AI Match Briefing is presented from the static asset
The stats page SHALL load `briefing.json` from its colocated data directory and present an AI Match Briefing section independently of the historical-statistics view. When available, it SHALL present the briefing headline, summary, and key points. It SHALL NOT present grounding identifiers or internal failure details to visitors.

#### Scenario: A briefing is available
- **WHEN** `briefing.json` contains a validated available briefing
- **THEN** the page presents its headline, summary, and key points as the AI Match Briefing

#### Scenario: Briefing is unavailable
- **WHEN** `briefing.json` reports no eligible target or a generation failure
- **THEN** the page explains that an AI Match Briefing is currently unavailable while continuing to present historical statistics

#### Scenario: Briefing asset cannot be loaded
- **WHEN** `briefing.json` is unavailable, invalid, or cannot be read
- **THEN** the page presents an unavailable AI Match Briefing state while continuing to present any successfully loaded historical statistics
