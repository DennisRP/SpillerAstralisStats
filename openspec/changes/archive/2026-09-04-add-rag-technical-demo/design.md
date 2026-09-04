## Context

See [proposal.md](proposal.md) for motivation. The project already has deterministic Markdown parsing and chunking, BM25 retrieval, evaluation metrics, structured article generation, citation validation, synthetic fixtures, and an optional live Foundry command. The missing piece is one cohesive developer-facing demonstration that composes those existing boundaries without private content or network access.

## Goals / Non-Goals

**Goals:**

- Make the full article RAG evidence path understandable from one command and one short guide.
- Keep the demonstration deterministic enough for automated output assertions and repeated teaching use.
- Show retrieval-level insufficient context, model-selected insufficient context, and generation validation failure as distinct outcomes.
- Reuse production application behavior for chunking, retrieval, context construction, output validation, and metrics.

**Non-Goals:**

- Do not add a visitor-facing question-answering interface or connect article RAG to `/stats`.
- Do not call PandaScore or Foundry in the default demonstration.
- Do not add embeddings, query translation, reranking, retry policies, token telemetry, or automated article collection.
- Do not make private corpus metrics part of the committed demonstration.

## Decisions

### 1. Add one dedicated `demo run` command to the existing RAG tool

The command will orchestrate the committed synthetic fixtures into a stable sequence rather than requiring the developer to combine corpus-build output, test output, and an article-answer preview manually. The existing commands remain unchanged. A standalone script was considered, but it would duplicate orchestration and make cross-platform behavior less consistent than the existing .NET console host.

### 2. Use an in-process deterministic structured-output fake

The supported scenario will return a fixed answer citing the relevant retrieved chunk. Separate fake outcomes will return structured insufficient context and an invalid citation. The application generator and validator will process these responses exactly as they process Foundry output. Using a live model by default was rejected because it introduces credentials, cost, latency, and nondeterministic prose into a reproducible teaching artifact.

### 3. Keep the demo fixture corpus deliberately small but multi-document

Extend the committed fixtures only enough to include one relevant article and at least one lexical distractor. Stable synthetic facts make expected ranking and citation behavior inspectable without asserting real-world esports claims. The private nine-article corpus remains useful for personal experimentation but cannot support a shared baseline.

### 4. Present architecture explanation next to executable evidence

A focused Markdown guide will explain why PandaScore facts and article retrieval are separate, how Recall@3 and reciprocal rank evaluate retrieval independently of prose, and what measured failure would justify semantic retrieval later. The command output supplies the concrete trace; the guide supplies interpretation. Embedding comparison code is deferred until an evaluation set demonstrates a concrete lexical limitation worth measuring.

### 5. Test stable markers and behavior rather than every floating-point character

Automated tests will assert stage ordering, selected chunk IDs, outcome labels, citation membership, version metadata, and absence of network resolution. Exact full-output snapshots were considered, but BM25 formatting or harmless explanatory wording changes would make them brittle without improving behavioral confidence.

## Risks / Trade-offs

- [A fake answer may be mistaken for model quality evidence] → Label it prominently as deterministic and explain that it demonstrates orchestration and validation, not prose quality.
- [A demo-specific orchestration path could diverge from application behavior] → Call the existing chunker, retriever, evaluator, context builder, generator, and validator rather than reimplementing them.
- [Output becomes too verbose for a concise walkthrough] → Use a tiny corpus, stable stage headings, and only the passages required to illustrate each outcome.
- [A weak shared lexical term produces evidence for an unsupported question] → Demonstrate both zero-result retrieval insufficiency and model-selected insufficiency after weak retrieval.

## Migration Plan

Add the command, synthetic distractor fixture, tests, and guide without changing current command contracts. Rollback consists of removing these demonstration-only additions; the existing corpus build, article answer, Function host, and static assets remain unaffected.
