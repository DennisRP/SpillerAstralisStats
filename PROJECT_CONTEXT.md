# SpillerAstralisStats – Project Context

## Goal

Build a new project named **SpillerAstralisStats**.

This project is intentionally separate from the existing `spillerastralis.dk` live match widget.

The main purpose is to gain practical AI engineering experience by building a real, small feature around Astralis CS2 match data.

The project should be pragmatic, cheap to run, easy to understand, and focused on learning:

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

RAG does not require a vector database. RAG is the complete pattern of retrieving
relevant external evidence, supplying that evidence to the model, and generating an
answer grounded in it. Keyword search, BM25, deterministic filtering, SQL, and vector
similarity are all possible retrieval mechanisms.

Do not add vector search merely because this is an AI/RAG project. The immediate
learning milestone uses a small, manually curated corpus of unstructured Counter-Strike
articles stored as Markdown and an in-memory lexical retriever. This keeps the work
focused on the important RAG boundaries:

```text
source documents
    -> deterministic chunking
    -> relevance-ranked retrieval
    -> retrieved passages with source IDs
    -> grounded structured generation
    -> citation and retrieval evaluation
```

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

The article corpus is genuinely unstructured content, but the first retrieval baseline
should still be lexical. Embeddings and vector search become a later comparison when
there is time to measure whether they improve retrieval quality. They are not required
to call the first implementation RAG.

Potential unstructured sources include:

* articles
* match reports
* written analysis
* external text sources

A good future architecture may use deterministic match retrieval, lexical article
retrieval, and semantic retrieval together. Mixed English and Danish articles are a
known limitation for lexical search; either keep the first corpus primarily in one
language, add a small explicit synonym list, or record this as a future reason to test
multilingual embeddings.

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

The immediate RAG corpus is file-based, not database-backed. Real copied article text
is local development material and MUST NOT be published in `dist` or committed to a
public repository. The implementation milestone should use these locations:

```text
/content/rag/articles/             # real local Markdown corpus; Git-ignored
/content/rag/examples/             # optional synthetic, redistributable examples
/src/SpillerAstralisStats.Tests/Fixtures/Articles/
                                  # synthetic deterministic test fixtures
```

The implementation MUST add the real corpus path to `.gitignore` before real article
text is stored there. Generated chunks are derived data and should be regenerated from
the Markdown sources rather than edited manually.

### Developer guide: add an article to the local RAG corpus

#### 1. Select and persist the source document

Choose a relevant article from a CS2 news site. Keep the initial
corpus intentionally small: approximately 6-10 articles that overlap across opponents,
roster changes, tournaments, and match recaps. Overlap makes retrieval evaluation
meaningful.

Save one article per file under `content/rag/articles/`. Use a stable descriptive file
name, for example:

```text
content/rag/articles/cs2-news-astralis-g2-recap-2026-08-20.md
```

Do not combine multiple articles in one file. Do not publish the copied text in static
assets. Retain the original source URL, and only store material that may lawfully be
used for this private learning prototype. If the repository or demonstration artifacts
will be shared, use short permitted excerpts, personal notes, or synthetic fixtures
instead of redistributing complete articles.

#### 2. Add metadata and preserve the article structure

Start every Markdown file with this front matter:

```markdown
---
documentId: cs2-news-astralis-g2-recap-2026-08-20
title: <original article title>
source: <CS2 news site>
sourceUrl: https://<source-domain>/<original-path>
publishedAt: 2026-08-20
language: en
tags: [astralis, g2, <tournament>]
---

<article text with original headings and paragraph boundaries preserved>
```

Use the source's factual name and the relevant language code, such as `da` for Danish
articles. Metadata values must be factual and copied from the source. Remove navigation,
advertisements, comments, image captions that add no evidence, and unrelated recommended
links.

Do not manually split the article into chunk files. The source document should remain
readable. The application will deterministically group headings and complete paragraphs
into chunks of roughly 150-300 words, avoid splitting short articles unnecessarily,
and assign stable IDs such as:

```text
cs2-news-astralis-g2-recap-2026-08-20#chunk-01
```

#### 3. Build, inspect, and evaluate the derived corpus

After adding or changing an article, run the corpus-build entry point introduced by the
RAG milestone. The OpenSpec change that implements this milestone MUST define and
document one explicit local command for this operation; do not require a database or a
live model call merely to parse and retrieve documents.

The corpus build must:

1. validate required front matter and report the source file for any error;
2. generate chunks deterministically and report document/chunk counts;
3. preserve document ID, chunk ID, title, source URL, publication date, language, and
   tags on every chunk;
4. run the deterministic retrieval evaluation set without calling Foundry; and
5. make the top retrieved chunks and scores inspectable before they are sent to the
   model.

Then run the normal solution build and tests. For the demonstration, use the prepared
local corpus and show this trace for one query:

```text
question
    -> ranked chunk IDs and scores
    -> exact passages supplied to the model
    -> structured answer with citations
    -> validation that every citation belongs to the retrieved set
```

The live model call remains optional. Retrieval and citation-contract tests must be
deterministic and runnable without network access or model cost.

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

This is a learning project, not a commercial service that needs enterprise-scale infrastructure.

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

### Milestone 7 – Article RAG and retrieval evals

This is the immediate next priority. Keep the implementation focused on the smallest
coherent RAG path that demonstrates retrieval, provenance, grounded generation, and
evaluation.

Build:

* a manually curated Markdown corpus of 6-10 relevant articles;
* deterministic Markdown validation and paragraph-aware chunking;
* an in-memory lexical retriever with inspectable scores and top-K results;
* a small fixed evaluation set with expected relevant document/chunk IDs;
* retrieval metrics such as Recall@3 and reciprocal rank;
* a versioned RAG prompt that receives only the selected chunks;
* structured output containing editorial text and cited chunk IDs;
* validation that citations refer only to chunks supplied to the model; and
* an insufficient-context result when retrieval does not support an answer.

The generated RAG context must retain source URL, document ID, chunk ID, publication
date, retrieval score/rank, and retriever/chunker version. Real article text must not be
included in public static output.

### Milestone 8 – Demonstration and focused reliability

Prepare a concise technical demonstration that can explain and show:

* why deterministic PandaScore retrieval and unstructured article retrieval are
  separate paths;
* how one Markdown article becomes stable retrieval chunks;
* how a question produces ranked passages;
* how retrieval failure differs from generation failure;
* how citation validation constrains the model;
* how Recall@K evaluates retrieval independently from generated prose; and
* why embeddings, vector storage, hybrid search, and reranking are justified future
  experiments rather than prerequisites for RAG.

Only add reliability work needed to keep this path demonstrable: safe failure states,
no live calls in normal tests, version metadata, and useful non-sensitive logs. Broader
retry policies, latency/token telemetry, and production hardening remain later work.

---

## Future possibilities

Do not implement these until useful:

* map-level PandaScore data
* Historical PandaScore subscription
* player statistics
* Azure SQL
* PostgreSQL/pgvector
* Azure AI Search
* embeddings
* hybrid retrieval
* semantic search
* approximate nearest-neighbor indexing
* automated article crawling or synchronization
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
