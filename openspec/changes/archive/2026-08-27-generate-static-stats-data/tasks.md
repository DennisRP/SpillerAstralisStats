## 1. Static Data Contracts

- [x] 1.1 Add output-path options and immutable JSON DTOs for completed matches, recent form, and opponent summaries; verify serialization uses camel-case and omits unavailable values.
- [x] 1.2 Build a historical static-data snapshot from `MatchRecord` values, including one head-to-head summary per distinct eligible opponent; verify tests cover mixed input and no eligible matches.

## 2. Coherent File Publication

- [x] 2.1 Implement a staging-directory publisher for `matches.json` and `stats.json`; verify successful publication exposes both files with the expected content.
- [x] 2.2 Preserve the prior generation when serialization or publication fails; verify a failure-injection test leaves previous assets unchanged and removes staging output.

## 3. Scheduled Integration and Validation

- [x] 3.1 Register the publisher and invoke it only after successful PandaScore ingestion; verify function tests log safe publication counts and do not publish after ingestion failure.
- [x] 3.2 Update local configuration guidance for the static output path; verify the example remains secret-free and documents the default directory.
- [x] 3.3 Run `dotnet restore`, `dotnet build`, `dotnet test`, and `openspec validate --strict`; verify all checks pass without warnings and no test calls PandaScore or a language model.
