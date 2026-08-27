## 1. Configuration and Internal Models

- [x] 1.1 Add PandaScore settings binding for API base address, API key, and a 6–12 month lookback with a default of 12; verify missing or invalid values produce a credential-safe configuration failure without an HTTP request.
- [x] 1.2 Add immutable internal match, opponent, score, game, and competition records plus focused PandaScore response DTOs and `System.Text.Json` mapping; verify fully populated and partial payload fixtures map supported data without invented values.

## 2. PandaScore Retrieval

- [x] 2.1 Register a typed PandaScore `HttpClient` and implement authenticated requests to the Astralis (`3209`) team matches endpoint; verify request construction and authorization through a stub `HttpMessageHandler` without logging the credential.
- [x] 2.2 Implement paging, provider-ID deduplication, and UTC historical filtering using an injected `TimeProvider`; verify multiple pages yield each in-window historical match once and exclude upcoming, out-of-window, and time-less records.
- [x] 2.3 Return categorized results for transport, non-success HTTP, and malformed JSON failures while propagating cancellation; verify no partial match collection is returned for each failure category.

## 3. Function Integration and Validation

- [x] 3.1 Register the ingestion service in the isolated worker host and invoke it from the existing timer function with the Function cancellation token; verify successful ingestion logs the configured lookback and mapped count while retaining the initial greeting.
- [x] 3.2 Log configuration and provider failures with safe operational context only; verify logs contain no API key, authorization header, or raw response body.
- [x] 3.3 Update `local.settings.json.example` and `README.md` with non-secret PandaScore configuration guidance; verify the example contains no API key and the documented settings match the bound options.
- [x] 3.4 Run `dotnet restore`, `dotnet build`, and `dotnet test` against the root solution; verify all tests pass, the build has no warnings, and no test uses the live PandaScore API.
