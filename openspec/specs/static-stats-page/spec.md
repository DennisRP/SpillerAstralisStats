# static-stats-page Specification

## Purpose

Provides a small static `/stats` experience that presents deterministic historical Astralis match data and can be deployed as an isolated directory within the existing site.

## Requirements

### Requirement: Historical statistics are presented as a static page
The application SHALL provide a static stats page that presents recent Astralis form, aggregate form values, head-to-head summaries, and recent completed matches from the published `stats.json` and `matches.json` assets. The page SHALL NOT present historical data as upcoming-match information.

#### Scenario: Historical data is available
- **WHEN** a visitor opens the stats page and both data assets load successfully with eligible historical matches
- **THEN** the page presents recent form, head-to-head information, and recent match results derived from those assets

#### Scenario: Optional match values are unavailable
- **WHEN** a published match omits an optional score or competition value
- **THEN** the page presents the available match information without inventing a replacement value or failing to render the remaining data

### Requirement: Page states are explicit
The stats page SHALL expose understandable loading, empty, and failure states so visitors are not shown stale placeholders or an indefinitely incomplete page.

#### Scenario: Data is loading
- **WHEN** the page has started loading its data assets but has not received both responses
- **THEN** the page indicates that statistics are loading

#### Scenario: No historical matches are available
- **WHEN** both data assets load successfully with valid empty collections
- **THEN** the page explains that no completed historical matches are currently available

#### Scenario: A data asset cannot be loaded
- **WHEN** either required JSON asset is unavailable, invalid, or cannot be read
- **THEN** the page displays a failure state without rendering a partial statistics view as complete

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

### Requirement: Stats output is deployable at `/stats`
The complete stats page SHALL be contained beneath `dist/stats` and SHALL resolve its own page, style, script, and data assets within the `/stats` path. It SHALL NOT depend on a server-side runtime, a live PandaScore request, or an asset outside that directory.

#### Scenario: Static directory is hosted below the existing site
- **WHEN** the contents of `dist` are published to a static host whose root also contains the existing site
- **THEN** the stats page and its required assets are available under `/stats` without replacing or modifying the existing root experience

#### Scenario: Page is loaded after generation
- **WHEN** the generated `dist/stats` directory contains the page assets and a successful static-data generation
- **THEN** the page loads its data from the colocated `data` directory without calling PandaScore directly

### Requirement: The MVP remains usable across common viewport sizes
The stats page SHALL preserve readable content, distinguishable status information, and operable document navigation on both narrow mobile and desktop viewports.

#### Scenario: Narrow viewport
- **WHEN** a visitor opens the stats page on a narrow viewport
- **THEN** statistics and match content remain readable without requiring horizontal page scrolling

#### Scenario: Script execution is unavailable
- **WHEN** client-side script execution is unavailable
- **THEN** the page still identifies itself as the Astralis stats page and explains that JavaScript is required to load the statistics
