## Context

The current Function App invokes a timer function but has no source data. This change supplies the first external boundary: PandaScore's Astralis team matches endpoint. The existing `project-bootstrap` capability continues to own the host schedule and greeting; this design adds only retrieval and mapping needed by later deterministic statistics. See `proposal.md` for motivation and `specs/pandscore-match-ingestion/spec.md` for required behavior.

## Goals / Non-Goals

**Goals:**

- Retrieve the most recent six to twelve months of Astralis match data without persisting it.
- Isolate PandaScore's HTTP/JSON contract from internal match data.
- Make request, paging, mapping, filtering, and failure behavior deterministic and unit-testable.
- Keep credentials configuration-only and prevent accidental secret or payload logging.

**Non-Goals:**

- Calculate form, head-to-head, win rates, or other statistics.
- Fetch map-level information, player statistics, articles, or historical PandaScore premium endpoints.
- Store matches, generate static assets, implement retries, or add a database.
- Add LLM or semantic retrieval functionality.

## Decisions

### Use one typed `HttpClient` with configuration-bound options

Register a typed PandaScore client through `IHttpClientFactory`. `PandaScoreOptions` provides the API base address, API credential, and `HistoryLookbackMonths`; the credential is supplied as a Function App setting such as `PandaScore__ApiKey`, never as a project file value.

The options object validates the lookback range at the ingestion boundary. Missing credentials and invalid lookback values produce an explicit failed ingestion result that the timer function logs, rather than causing a host startup failure. This preserves the timer's ability to start and makes the misconfiguration observable.

Creating a new `HttpClient` per run is rejected because the Function App already has dependency injection and a factory makes connection reuse and testing straightforward. A generic provider abstraction is also deferred because PandaScore is the only provider in scope.

### Separate PandaScore DTOs from internal records

Keep provider DTOs in a focused PandaScore infrastructure folder and deserialize them with `System.Text.Json`. Map them to small immutable internal records for matches, opponents, scores, games, and competition details. Provider DTOs never cross into later statistics code.

The mapper preserves stable PandaScore IDs and represents legitimately missing fields as nullable or empty values. It does not invent map names, round scores, or any value absent from the response.

### Fetch every page and apply an explicit historical filter

The client requests the `teams/3209/matches` endpoint page by page using the provider's standard paging parameters until it receives an empty page. It deduplicates matches by PandaScore match ID across pages.

The ingestion service computes the inclusive window from `TimeProvider`: start is the configured number of calendar months before the current UTC time, end is the current UTC time. It treats `begin_at` as the match's relevant time, falling back to `scheduled_at`; records with neither value, or with a relevant time outside the window, are excluded. This prevents upcoming matches from entering historical data while keeping the external request contract simple and testable.

Provider-side date filtering is intentionally deferred until the exact PandaScore query behavior is verified against the account in use. Local deterministic filtering remains authoritative for v1.

### Return an ingestion result and keep the trigger thin

The ingestion service returns a result containing mapped matches on success or a categorized failure result for configuration, transport, HTTP-status, and payload failures. The timer function passes the Functions cancellation token to the service and logs a compact summary: configured lookback and mapped count for success, or safe contextual failure information for failure.

Cancellation is not converted into a normal result; it propagates to the Functions runtime. Full response bodies, authorization headers, and credentials are never logged. No partial result is exposed if any page cannot be retrieved or parsed.

No automatic retry is introduced. A once-daily job with provider rate limits is better served initially by observable failure; retries can be designed later with idempotent static output generation.

### Test at the HTTP boundary with fixed time

Unit tests construct the typed client with a stub `HttpMessageHandler` and use a fixed `TimeProvider`. Tests cover authorization/request construction without asserting the credential value in output, multi-page aggregation and ID deduplication, date filtering, field mapping, invalid options, non-success responses, malformed JSON, and cancellation. Existing function tests are extended only as needed to verify it calls the ingestion boundary and logs its result.

## Risks / Trade-offs

- [The general team matches endpoint can contain more data than the configured window] → Page until completion and filter deterministically; add provider-side filtering only after its contract is verified.
- [PandaScore may alter optional fields or return incomplete data] → Treat supported fields as optional, keep DTOs isolated, and fail safely on malformed JSON.
- [A missing credential means scheduled executions retrieve nothing] → Log a credential-safe configuration failure on every run until corrected.
- [No persistence means each daily run refetches the window] → This is acceptable for v1 and avoids database cost; later static generation or persistence can consume the returned records.
- [Static `RunOnStartup` can cause extra API calls] → The client performs read-only retrieval and the later processing stages must be idempotent.
