## 1. Static Page Shell

- [x] 1.1 Add semantic `dist/stats/index.html` with loading, content, empty, failure, and no-script regions; verify all page resources resolve within the `/stats` tree and no existing root-site asset is changed.
- [x] 1.2 Add a responsive `dist/stats/styles.css` for recent form, summary cards, head-to-head, and match history; verify narrow and desktop browser views remain readable without horizontal page scrolling.

## 2. Historical Data Rendering

- [x] 2.1 Add `dist/stats/app.js` to load `data/matches.json` and `data/stats.json` together, validate the required top-level shapes, and transition to loading, empty, or failure states; verify representative success, empty, invalid, and unavailable-data cases.
- [x] 2.2 Render only supplied recent-form, head-to-head, and recent-match facts, including neutral handling of optional scores and competition values; verify fixtures cover multiple opponents, unknown outcomes, and missing optional values without recalculating statistics in JavaScript.

## 3. Contract and Handoff Verification

- [x] 3.1 Add focused tests for the static page artifact contract and compatibility with the existing serialized JSON shape; verify the tests assert required files, `/stats`-local references, and representative publisher output.
- [x] 3.2 Document local generation and static preview from the `dist` root plus the future deployment handoff at `dist/stats`; verify the instructions do not require Azure credentials, Node.js integration, or a frontend build step.
- [x] 3.3 Generate the static data, serve `dist` locally, and smoke-test `/stats` and `/stats/` for success, empty, failure, narrow, and desktop states; record any hosting-path limitation without adding Azure deployment automation.
- [x] 3.4 Run `dotnet restore`, `dotnet build`, `dotnet test`, and `openspec validate --strict`; verify all automated checks pass without warnings and the page makes no live PandaScore or language-model call.
