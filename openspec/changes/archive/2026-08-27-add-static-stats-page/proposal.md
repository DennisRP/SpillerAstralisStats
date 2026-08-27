## Why

Milestone 3 generates deterministic `matches.json` and `stats.json`, but the project has not yet demonstrated that a static page can consume and present those assets. A small `/stats` MVP closes that gap while establishing a deployment-safe directory contract for the existing Azure Static Web App.

## What Changes

- Add a framework-free static stats page with `index.html`, `app.js`, and `styles.css` under the generated `dist/stats` tree.
- Load `data/matches.json` and `data/stats.json` through paths relative to `/stats`, then render recent form, aggregate statistics, head-to-head summaries, and recent match results.
- Provide clear loading, empty, and data-load failure states without requiring a live API or server-side runtime.
- Make the complete `dist/stats` directory a self-contained publication unit that can later be copied into the existing Azure Static Web App at `/stats`.
- Keep concrete Azure deployment automation and integration with the independent Node.js live-site job outside this change.
- Do not add upcoming-match behavior, LLM output, `briefing.json`, a frontend framework, or a database.

## Capabilities

### New Capabilities

- `static-stats-page`: A static `/stats` MVP that consumes the generated historical JSON assets and remains portable as one deployment directory.

### Modified Capabilities

None.

## Impact

- Adds static web assets and focused frontend tests or verification fixtures.
- Establishes `dist/stats` as the handoff boundary for future Azure Static Web App deployment.
- Consumes the existing `static-stats-data` JSON contract without changing PandaScore ingestion or deterministic fact calculation.
- Adds no runtime service, paid Azure resource, database, or frontend package requirement.
