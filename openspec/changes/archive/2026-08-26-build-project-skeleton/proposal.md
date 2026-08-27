## Why

SpillerAstralisStats needs a minimal, runnable Azure Functions foundation before PandaScore, deterministic statistics, and LLM functionality can be introduced. Establishing the intended scheduled host and basic validation now gives later milestones a clear starting point for periodically retrieving six to twelve months of CS2 history.

## What Changes

- Add a root-level .NET solution and general repository files, including introductory documentation.
- Add a .NET 8 isolated Azure Function App under `src/`.
- Add a timer-triggered function that runs daily using the NCRONTAB expression `0 37 13 * * *`, also runs when the Functions host starts, and logs `Hello World`.
- Add a test project under `src/` so production code and tests share the requested source-tree boundary.
- Add initial automated tests for the timer function's observable behavior and schedule configuration.
- Keep the skeleton free of PandaScore retrieval, LLM, persistence, and deployment implementation; later history retrieval must be idempotent so startup and scheduled executions can safely overlap or repeat.

## Capabilities

### New Capabilities

- `project-bootstrap`: Defines the initial repository layout and runnable, scheduled Azure Function App foundation.

### Modified Capabilities

None.

## Impact

This change introduces the root solution, documentation and general repository files, plus an Azure Function App and test project beneath `src/`. It adds Azure Functions isolated-worker and timer-extension dependencies, requires Azure Functions Core Tools and local storage support to run locally, and has no impact on the existing independently operated Node.js live-match solution or deployed website.
