# SpillerAstralisStats – Project Context

## Goal

Build a new project named **SpillerAstralisStats**.

This project is intentionally separate from the existing `spillerastralis.dk` live match widget.

The main purpose is to gain practical AI engineering experience by building a real, small feature around Astralis CS2 match data.

The project should be pragmatic, cheap to run, easy to explain in a job interview, and focused on learning:

* LLM integration
* grounded generation
* structured output
* retrieval
* evals
* observability
* AI failure handling
* pragmatic architecture

Do **not** over-engineer the solution.

---

## Existing system – keep separate

There is already a small Node.js solution that powers the current `spillerastralis.dk` experience.

Its purpose is very narrow:

> Show when Astralis plays next.

It:

* runs on a private Proxmox server
* queries PandaScore
* finds upcoming/ongoing Astralis matches
* generates static HTML
* deploys the generated page to Azure Static Web Apps
* is embedded in Home Assistant as an iframe card
* polls relatively frequently because CS2 matches are often delayed shortly before scheduled start

This existing live solution should **not** be ported to C# as part of this project.

Do not combine the live polling requirements with the AI/stats application.

The live solution should remain operationally independent.

---

## New application

The new application is:

`SpillerAstralisStats`

It should generate a richer stats/AI experience, initially intended to be served under something like:

`https://spillerastralis.dk/stats`

The output should still be suitable for static hosting.

The basic concept is:

```text
PandaScore
    ↓
.NET application
    ↓
Domain models
    ↓
Deterministic statistics / facts
    ↓
Relevant historical context
    ↓
LLM
    ↓
Structured match briefing
    ↓
Static JSON / HTML assets
    ↓
Azure Static Web App /stats
```

---

## Technology

Use:

* C#
* .NET 8
* `System.Text.Json`
* OpenSpec workflow
* Codex for implementation
* static output where practical

The initial runtime can be the existing Proxmox server.

Do not require Azure Functions, Azure SQL, Cosmos DB, or other paid Azure infrastructure for v1.

The goal is to keep ongoing cost close to zero apart from actual LLM usage.

---

## Data source

Use the PandaScore API.

Astralis team ID:

```text
3209
```

Existing useful endpoint:

```text
GET /teams/3209/matches
```

This returns historical and upcoming matches.

Useful fields observed include:

* match id
* scheduled time
* begin/end time
* status
* winner id
* opponents
* scores
* match type
* number of games
* league
* serie
* tournament
* tournament tier
* tournament country
* tournament prize pool
* games
* per-game winner
* game duration
* stream URLs
* rescheduled status

Example match data can contain:

```text
Astralis 1-2 G2

Game 1 winner: Astralis
Game 2 winner: G2
Game 3 winner: G2
```

The basic match endpoint does **not** currently provide:

* map name, e.g. Mirage
* round score, e.g. 13-9

Those appear to require higher PandaScore access / Historical endpoints.

Do not make map-level stats a v1 requirement.

---

## Important architecture principle

The LLM must **not calculate facts that normal code can calculate reliably**.

Examples of deterministic application logic:

* last 5 match results
* wins/losses
* head-to-head record
* latest meeting
* number of previous meetings
* score
* tournament
* opponent
* date/time
* win percentage
* number of maps
* sweep / 2-0 statistics

Normal C# code should calculate these values.

The LLM should primarily:

* summarize
* synthesize
* explain
* write natural-language match briefings
* identify interesting patterns from supplied facts/context

Example:

Application code produces:

```json
{
  "opponent": "G2",
  "recentForm": {
    "wins": 3,
    "losses": 2
  },
  "headToHead": {
    "astralisWins": 2,
    "g2Wins": 4
  },
  "lastMeeting": {
    "score": "1-2",
    "winner": "G2"
  }
}
```

Then the LLM may produce something like:

```json
{
  "headline": "Astralis møder G2 igen",
  "summary": "Astralis går ind til kampen med tre sejre i deres seneste fem kampe, men G2 har haft overtaget i de seneste indbyrdes opgør.",
  "keyPoints": [
    "Astralis har vundet 3 af deres seneste 5 kampe.",
    "G2 vandt det seneste indbyrdes møde 2-1."
  ]
}
```

The LLM should not invent or infer unsupported facts.

---

## Retrieval strategy

Do not add vector search merely because this is an AI/RAG project.

For structured match data, deterministic retrieval is preferable.

Examples:

```text
Find latest 5 Astralis matches
Find previous matches against the next opponent
Find latest meeting
Find recent tournament matches
```

This can initially be done in memory over fetched PandaScore data.

If persistence is later required, relational querying would be appropriate.

Vector/semantic retrieval should only be added when there is genuinely unstructured content, e.g.:

* articles
* match reports
* written analysis
* external text sources

A good future architecture may use both deterministic structured retrieval and semantic retrieval.

---

## Storage

Do **not** introduce a database for v1 unless a concrete need appears.

PandaScore can remain the source of truth.

The application can generate static data files such as:

```text
/dist
    /stats
        index.html
        app.js
        styles.css
        /data
            matches.json
            stats.json
            briefing.json
```

The static files can be regenerated.

This avoids database hosting cost.

If later data cannot be regenerated from PandaScore, or AI-specific data needs to be retained, persistence can be introduced then.

Possible later persistent data:

* generated briefing history
* evaluation results
* prompt versions
* embeddings
* external article data
* retrieval provenance
* latency/cost metrics

---

## AI output

Prefer structured output.

Create a strongly typed model such as:

```csharp
public sealed record MatchBriefing(
    string Headline,
    string Summary,
    IReadOnlyList<string> KeyPoints);
```

Validate LLM output before publishing it.

Keep prompt/schema version explicit.

The application should be able to continue generating the stats site even if the LLM call fails.

AI should enhance the product, not become a single point of failure.

---

## Stats page

The initial `/stats` page may show:

* next match
* opponent
* tournament
* recent Astralis form
* latest match results
* head-to-head against upcoming opponent
* latest previous meeting
* AI Match Briefing

Possible layout:

```text
SPILLER ASTRALIS?

Next match
Astralis vs G2
Esports World Cup

AI MATCH BRIEFING

<generated summary>

Key points
- ...
- ...

RECENT FORM
W W L W L

HEAD TO HEAD
Astralis 2 - 4 G2

RECENT MEETINGS
...
```

The AI feature should feel like part of a real stats product, not like a generic chatbot.

---

## Deployment

The output should be deployable to the **same Azure Static Web App** as the existing site.

The new application should generate `/stats`.

The existing live Node.js solution should continue generating its existing live view.

Avoid coupling the two applications at runtime.

If possible, deployment should publish a complete static distribution directory atomically rather than having independent jobs overwrite arbitrary parts of the same site.

Example:

```text
/dist
    astralis.html
    /stats
        index.html
        ...
```

Exact deployment orchestration can be decided later.

---

## Cost constraints

Ongoing cost should be highly predictable and ideally close to zero.

Preferred:

* Proxmox compute: already available
* Azure Static Web Apps: existing/free
* PandaScore: existing/free access
* local/static storage: effectively free
* LLM: acceptable variable cost

Avoid:

* always-on SQL
* paid compute with unpredictable wake/runtime cost
* unnecessary Azure services
* infrastructure added mainly for architectural appearance

This is both a learning project and a portfolio/job-interview project, not a commercial service that needs enterprise-scale infrastructure.

---

## Initial development milestones

Suggested order:

### Milestone 1 – PandaScore integration

Build a .NET 8 application that:

* calls PandaScore
* retrieves Astralis matches
* deserializes the response
* maps PandaScore DTOs into internal domain models

No AI yet.

### Milestone 2 – Match facts

Create deterministic services that can calculate:

* recent form
* last N matches
* head-to-head
* latest meeting
* wins/losses
* relevant tournament data

Add unit tests.

### Milestone 3 – Static stats data

Generate:

```text
stats.json
matches.json
```

Prove that a static `/stats` page can consume the generated data.

### Milestone 4 – First LLM integration

Implement:

```text
C# → LLM → validated structured MatchBriefing
```

The input should be deterministic facts from the application.

### Milestone 5 – Grounded briefing

Create context containing:

* upcoming match
* recent form
* relevant previous meetings
* tournament information

Generate a grounded Match Briefing.

Record which context was supplied.

### Milestone 6 – Static page integration

Generate:

```text
briefing.json
```

and display it on `/stats`.

### Milestone 7 – Reliability

Add:

* timeout handling
* invalid structured output handling
* retries only where appropriate
* fallback behavior
* logging
* model name
* latency
* token usage where available
* prompt/schema version

### Milestone 8 – Evals

Create a small evaluation suite.

Possible eval cases:

* correct latest meeting retrieved
* correct head-to-head facts
* no unsupported factual claims
* valid output schema
* requested question actually answered
* latency
* approximate cost

This is an important part of the learning objective.

---

## Future possibilities

Do not implement these until useful:

* map-level PandaScore data
* Historical PandaScore subscription
* player statistics
* Azure SQL
* PostgreSQL/pgvector
* Azure AI Search
* article ingestion
* embeddings
* hybrid retrieval
* semantic search
* MCP/tool calling
* interactive question answering

They are potential later learning steps, not v1 requirements.

---

## Development philosophy

Keep the implementation pragmatic.

Prefer:

* small focused changes
* readable C#
* simple architecture
* explicit data flows
* good tests around deterministic logic
* AI only where AI adds actual value

Avoid:

* generic repository patterns without need
* interfaces for every class
* excessive Clean Architecture ceremony
* premature microservices
* unnecessary Azure infrastructure
* premature vector databases
* unnecessary abstraction around PandaScore
* rewriting the existing live application

The interesting engineering story is not:

> “I used every AI technology.”

It is:

> “I chose deterministic software for deterministic problems and added the LLM where probabilistic language synthesis actually created value. I then added grounding, structured output, failure handling and evals to make that LLM feature reliable.”

That principle should guide the project.