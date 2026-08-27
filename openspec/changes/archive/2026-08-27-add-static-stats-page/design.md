## Context

The application already publishes `matches.json` and `stats.json` atomically under the configured `dist/stats/data` directory. There is no frontend toolchain, database, or web server in the project, and the existing Node.js live-match site must remain operationally independent. See `proposal.md` and the `static-stats-page` delta spec for the intended behavior.

The future Azure Static Web App deployment needs one unambiguous handoff boundary, but the existing site's deployment job is outside this repository and its orchestration is not yet known.

## Goals / Non-Goals

**Goals:**

- Produce a useful historical-stats MVP from the current JSON contract.
- Keep `dist/stats` self-contained and safe to place beneath an existing static-site root.
- Preserve a simple local preview and a clear path to atomic deployment later.
- Keep missing provider values and page-state behavior explicit.

**Non-Goals:**

- Configure Azure Static Web Apps, credentials, routes, or CI/CD.
- Merge this application's runtime with the existing Node.js live-match process.
- Add upcoming-match retrieval, AI briefing output, client-side persistence, or a frontend build pipeline.
- Recalculate statistics in JavaScript.

## Decisions

### Use hand-authored HTML, CSS, and browser JavaScript

The MVP will use `index.html`, `styles.css`, and `app.js` without a frontend framework or package manager. The page has a small, read-only interaction surface, so a framework would add installation, build, and deployment complexity without solving a current requirement.

Alternative considered: a component framework with a bundler. This is deferred until page complexity creates a concrete need.

### Treat `dist/stats` as the deployment handoff

The static shell will live directly under `dist/stats`, while the existing publisher continues to replace `dist/stats/data` as one coherent JSON generation. All page references will stay within the `/stats` tree, making the complete directory suitable for a later atomic upload or directory swap by the existing site's deployment orchestration.

This intentionally establishes the artifact layout now without choosing how the external Azure deployment job copies it. The later deployment change should consume `dist/stats` rather than reach into project source or invoke application internals.

Alternative considered: implement Azure deployment in this change. That would require assumptions about another repository's pipeline, credentials, and ownership, and is unnecessary to prove the page contract.

### Keep rendering deterministic and presentation-only

JavaScript will fetch the two existing JSON assets, validate the small set of top-level shapes needed for rendering, and format supplied values. It may format dates and percentages for display, but it will not calculate recent form, head-to-head totals, outcomes, or other match facts.

The page will render a bounded recent-match list for readability while retaining all generated records in `matches.json`. Head-to-head summaries will be ordered deterministically using supplied opponent names and identifiers.

Alternative considered: derive summaries from `matches.json` in the browser. This would duplicate tested C# business logic and create two sources of truth.

### Fail the page as one data view

Both JSON files are required for the complete view. Loading happens together; invalid or failed input produces one visible failure state rather than presenting a partial view as if it were complete. Valid empty collections produce a dedicated empty state.

Optional nested values remain optional and use neutral display text such as "Ikke oplyst" rather than inferred content.

### Verify the static contract without adding a JavaScript test stack

Focused .NET tests will verify that required page assets exist, reference only the expected `/stats`-local resources, and remain compatible with representative serialized output. A local static-server browser smoke test will verify rendering, responsive layout, empty state, and load failure during implementation.

Alternative considered: add a browser automation framework. Its dependency and maintenance cost are disproportionate for this first static page; it can be introduced when interactive behavior grows.

## Risks / Trade-offs

- [Static shell files and generated data share `dist/stats`] → Keep generated JSON confined to `dist/stats/data` and document that deployment consumes the complete `dist/stats` directory.
- [A future JSON contract change can break browser rendering] → Validate required top-level shapes and keep representative contract tests alongside the publisher tests.
- [Hosting `/stats` without directory-index behavior may differ between preview and Azure] → Use `/stats`-local asset paths and include an Azure-path smoke check when deployment is implemented.
- [No automated browser execution] → Keep browser logic small and pure where practical, add static contract tests, and record a repeatable manual smoke check in the implementation tasks.

## Migration Plan

1. Add the static shell beneath `dist/stats` without changing the existing root site.
2. Generate `dist/stats/data` through the current scheduled flow and preview `dist` through a local static server.
3. Verify direct navigation to `/stats` and `/stats/`, successful data rendering, empty data, and unavailable data.
4. In a separate deployment change, make the existing site pipeline stage its root assets and this complete `dist/stats` directory before one Azure Static Web App publication.

Rollback consists of removing the `/stats` directory from the staged static-site artifact; the existing root experience and Node.js runtime remain unchanged.

## Open Questions

- Which repository or job will own the final composition and atomic Azure Static Web App deployment? This can be selected later because `dist/stats` is the fixed handoff contract.
