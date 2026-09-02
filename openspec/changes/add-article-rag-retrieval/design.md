## Context

See [proposal.md](proposal.md) for the motivation. The application currently has deterministic PandaScore retrieval, a grounded match-facts contract, and a match-specific structured-output path through Microsoft Foundry. It has no unstructured corpus, chunk representation, search index, retrieval evaluation set, or citation-bearing output contract.

The project intentionally targets .NET 8, local/static storage, no database, and normal tests that make no external calls. Real copied article content is private learning material and cannot be committed or published. The new capability requirements are defined in [specs/article-rag-retrieval/spec.md](specs/article-rag-retrieval/spec.md).

## Goals / Non-Goals

**Goals:**

- Make every transformation from Markdown source to model context deterministic and inspectable.
- Establish a simple lexical baseline whose strengths and failures can be measured before semantic retrieval is considered.
- Separate retrieval evaluation from probabilistic answer evaluation.
- Reuse the existing Foundry transport and strict structured-output approach without coupling article RAG to match-briefing types.
- Provide one small local tool through which a developer can learn by inspecting documents, chunks, rankings, scores, and metrics.

**Non-Goals:**

- The article corpus does not alter deterministic PandaScore facts or become a source for calculated match statistics.
- This change does not connect article output to `DailyStatsFunction`, `briefing.json`, `/stats`, or deployment.
- It does not crawl, scrape, refresh, summarize, or publish source articles automatically.
- It does not add embeddings, vector search, hybrid search, reranking, stemming, query translation, a database, or a managed Azure search service.
- It does not establish production thresholds, broad telemetry, or an end-to-end semantic answer-quality suite.

## Decisions

### 1. Keep article RAG as a separate application flow

The new flow accepts a natural-language question and a prepared article corpus, retrieves passages, constructs a dedicated RAG context, and optionally requests a cited editorial answer. It does not extend `GroundedMatchBriefingContext` or change the current scheduled briefing.

This separation preserves the existing rule that match statistics come only from deterministic PandaScore calculation. It also lets the developer diagnose article retrieval without running the production-like static pipeline. Adding article chunks directly to the existing match briefing was rejected because it would combine corpus preparation, retrieval, model behavior, publication, and visitor rendering in one milestone.

### 2. Use a constrained Markdown/front-matter contract

Each source is one UTF-8 Markdown file with `---` front-matter delimiters and the exact required fields documented in `PROJECT_CONTEXT.md`. The parser supports the deliberately small scalar and inline-tag-list syntax used by this corpus rather than pretending to implement arbitrary YAML. It validates all files, collects source-specific errors, checks unique document IDs, and returns no corpus when any document is invalid.

`documentId` is lowercase kebab-case and remains independent of the filename. `publishedAt` is an ISO calendar date, `language` initially accepts `en` or `da`, tags are lowercase unique tokens, and `sourceUrl` must be absolute HTTP or HTTPS. Unknown metadata fields are rejected so misspellings do not silently disappear.

A general YAML package was considered. The narrow contract does not need anchors, nested objects, multiline scalars, or other YAML features, so an explicit parser keeps behavior understandable and avoids a dependency. If the corpus contract later becomes richer, replacing this parser with a mature YAML library can be evaluated then.

### 3. Chunk headings and whole paragraphs with an ordinal versioned identity

The chunker recognizes Markdown headings and blank-line-delimited paragraphs. A heading becomes context for following paragraphs. It groups complete paragraphs toward a 150-word minimum and 300-word target maximum; it starts a new chunk before adding a paragraph that would exceed the maximum when the current chunk is already meaningful. A single long paragraph remains intact, and a short final fragment is merged with its predecessor when possible without crossing a top-level article boundary.

Chunks use `<documentId>#chunk-01` ordinal IDs and carry `article-chunker-v1`. “Stable” means reproducible for identical input and configuration, not immutable across source edits. An edit can shift ordinal boundaries, so the corpus-build output calls out changed counts and the developer reviews affected evaluation judgments. Generated chunks are never hand-edited or treated as source data.

Fixed character windows were rejected because they can sever paragraphs and discard the useful structure already preserved in Markdown. LLM-based semantic chunking was rejected because it would make corpus preparation probabilistic, billable, and harder to test.

### 4. Implement an explicit in-memory BM25 baseline

The first retriever uses BM25 with fixed versioned parameters (`k1 = 1.2`, `b = 0.75`). Searchable text consists of document title, inherited heading, and passage body. The v1 tokenizer lowercases invariantly and extracts Unicode letter-or-digit runs; it performs no stemming, stop-word removal, synonym expansion, language detection, or metadata boosting. Results sort by score descending and chunk ID ordinal ascending.

Every score, rank, tokenizer/retriever version, and passage is returned to the caller. A score greater than zero is the v1 minimum-support rule; zero-score chunks are not supplied to the model. This rule is intentionally simple and will be judged by negative evaluation queries rather than presented as a production relevance threshold.

A simple term-count score was considered, but BM25 adds document-frequency and length normalization while remaining small enough to implement and explain directly. A third-party search package was rejected because six to ten articles do not justify an index service or hide the learning value of the scoring calculation.

### 5. Store explicit relevance judgments and evaluate retrieval before generation

The private learning corpus uses an ignored `content/rag/evaluations.local.json` file. Each case has a stable query ID, question, expected relevant document and/or chunk IDs, and an `expectsSupport` flag. Synthetic equivalents live in test fixtures.

Evaluation always records the full top-three trace. Recall@3 measures retrieved relevant judgments divided by all relevant judgments; reciprocal rank is `1 / rank` for the first relevant result or zero when absent. Aggregate values are arithmetic means over supported cases. Unsupported cases are reported separately as correct rejections or false-positive retrievals rather than being blended into positive-query averages.

Document-level judgments allow a useful case before exact chunk boundaries settle; chunk-level judgments test precise passage ranking. A result satisfies either explicitly listed judgment type. LLM-as-judge retrieval evaluation was rejected for this baseline because it would add nondeterminism and cost before deterministic retrieval is understood.

### 6. Represent support and generation outcomes explicitly

Retrieval produces either selected positive-scoring chunks or `insufficientContext`. The latter makes no model call. When retrieval supplies chunks, the strict model schema is a discriminated result with either:

- `available`: bounded editorial text plus one or more cited chunk IDs; or
- `insufficientContext`: no editorial text and no citations.

Application validation enforces the discriminator, text limits, citation uniqueness, and the rule that every available citation belongs to the supplied chunks. The result retains the exact RAG context plus prompt, schema, chunker, tokenizer, and retriever versions. This makes a retrieval failure distinguishable from a provider failure and from an invalid model citation.

Allowing arbitrary URLs or document titles as citations was rejected because exact chunk IDs are easier to validate mechanically. Asking the model to answer when no lexical evidence exists was rejected because that would permit model memory to masquerade as retrieval.

### 7. Generalize the existing structured-output transport boundary once

The current Foundry client accepts a match-specific request even though its transport responsibility is generic: submit instructions, input JSON, schema name, and strict JSON Schema, then return output text or refusal. Refactor that genuine external boundary into one neutral structured-output client used by both the existing match generator and the new article generator. Keep match prompt construction, deserialization, exceptions, and validation match-specific so current observable behavior remains unchanged.

Creating a second nearly identical Foundry client was rejected because it would duplicate authentication, Responses API construction, error translation, and tests. Introducing a broader “AI service” abstraction was also rejected; the shared interface remains limited to the one transport operation that already exists.

### 8. Add a focused local console host for corpus inspection

Add a small .NET 8 console project, `src/SpillerAstralisStats.RagTool`, that references the application project and contains only local orchestration and presentation. The documented baseline command is:

```powershell
dotnet run --project src/SpillerAstralisStats.RagTool -- corpus build
```

It reads `content/rag/articles/` and `content/rag/evaluations.local.json`, validates and chunks the corpus in memory, runs evaluation, and prints document/chunk counts plus each query's ranked IDs, scores, exact passages, and metrics. It never resolves the Foundry client. A separate explicit command can later demonstrate generation, but a live call is not required by the corpus-build path.

Branching `Program.cs` in the Azure Functions host was rejected because a local learning CLI and the scheduled Function have different host responsibilities. Using an xUnit test as the user-facing corpus tool was rejected because tests should verify behavior rather than serve as an operational command.

### 9. Treat local source content as non-publishable input

Before any real article is stored, add `content/rag/articles/` and `content/rag/evaluations.local.json` to `.gitignore`. The corpus loader reads only explicitly configured content paths, and no publisher receives article documents or chunks. Committed examples and test fixtures contain only synthetic or otherwise redistributable text.

The tool preserves source URLs and dates for provenance but does not claim that attribution makes copying distributable. Developers remain responsible for lawful local use; documentation directs shared demonstrations toward synthetic text, personal notes, or short permitted excerpts.

## Risks / Trade-offs

- [Ordinal chunk IDs shift after source edits] → Treat chunks as derived versioned data, regenerate them, display count changes, and review affected evaluation judgments.
- [BM25 cannot bridge Danish and English vocabulary reliably] → Keep the baseline transparent, include cross-language cases in evaluation, and use measured failures to justify later synonyms or multilingual embeddings.
- [A positive lexical score can come from a weak shared term] → Report negative-query false positives and keep the minimum-support rule versioned; tune it only against the fixed evaluation set.
- [A narrow front-matter parser accepts less than full YAML] → Document the exact supported syntax and fail on unknown or malformed constructs instead of interpreting them inconsistently.
- [Copied articles create copyright and repository-leak risk] → Ignore private paths before content is added, never publish article text, and use redistributable fixtures for tests and demonstrations.
- [Generalizing the Foundry transport can regress match briefing behavior] → Preserve the request fields and add regression tests proving the existing prompt, schema, errors, cancellation, and opt-in smoke path behave unchanged.
- [A console project adds a third project to a small solution] → Keep it as a thin local host; all corpus, retrieval, and evaluation behavior remains in cohesive application types covered by tests.

## Migration Plan

1. Add ignore rules and synthetic fixtures before any private source material is introduced.
2. Add corpus contracts, validation, and chunking; use the local tool to inspect one synthetic article before expanding the corpus.
3. Add BM25 retrieval and the fixed evaluation format, then establish the lexical baseline without Foundry.
4. Refactor the neutral structured-output transport with existing match regression tests, then add article RAG context, output validation, and fake-client tests.
5. Document creation of the private 6–10 article corpus and run the corpus-build command to review every ranking and metric.
6. Optionally run one explicit live Foundry demonstration after deterministic retrieval and citations pass; normal tests and corpus building remain offline.
7. Roll back by removing the unused article tool and application flow; the existing match pipeline and published static assets require no data migration.
