## Why

The application can ground a briefing in deterministic PandaScore facts, but it cannot yet retrieve relevant evidence from unstructured Counter-Strike writing or measure whether that retrieval is reliable. A small manually curated article corpus is the next learning milestone because it exposes the core RAG boundaries—document preparation, chunking, retrieval, provenance, citation validation, and evaluation—without prematurely introducing embeddings or managed search infrastructure.

## What Changes

- Add a private, file-based corpus of manually curated Markdown articles with validated provenance metadata and documented copyright-safe handling.
- Add deterministic, paragraph-aware Markdown chunking that preserves source metadata and produces versioned, inspectable chunk identifiers.
- Add an in-memory lexical retriever with deterministic ranking, explicit scores, and configurable top-K selection.
- Add a fixed, network-free retrieval evaluation set with positive and insufficient-context queries, expected relevant evidence, Recall@3, and reciprocal-rank reporting.
- Add a versioned RAG context and prompt that submit only retrieved chunks to the model and request structured editorial text with cited chunk IDs.
- Validate that every citation belongs to the retrieved context and return an explicit insufficient-context result when retrieval cannot support an answer.
- Add one documented local corpus-build and evaluation command that requires neither a database nor a live model call.
- Keep embeddings, vector storage, hybrid search, reranking, automated article collection, static publication, and visitor-facing integration outside this change.

## Capabilities

### New Capabilities

- `article-rag-retrieval`: Prepares a curated Markdown article corpus, retrieves and evaluates relevant passages, and produces citation-constrained structured RAG output from those passages.

### Modified Capabilities

None.

## Impact

- Application code gains Markdown corpus contracts, validation, deterministic chunking, lexical retrieval, retrieval evaluation, RAG context assembly, and citation validation.
- Infrastructure gains local filesystem loading for a private corpus while the existing Foundry model boundary remains the only external AI dependency.
- Tests gain synthetic Markdown fixtures, fixed retrieval judgments, scoring and metric assertions, and fake-model grounded-output cases without network access.
- `.gitignore` and developer documentation gain explicit handling for `content/rag/articles/`; real copied article text is excluded from source control and `dist`.
- Existing PandaScore ingestion, deterministic match calculations, scheduled generation, `briefing.json`, and `/stats` behavior remain unchanged.
