## 1. Private Corpus Boundary and Fixtures

- [x] 1.1 Add `content/rag/articles/` and `content/rag/evaluations.local.json` to `.gitignore` before storing real content; verify `git check-ignore` identifies representative files in both paths and that no `dist` publisher references article content.
- [x] 1.2 Add the documented `content/rag/examples/` structure and synthetic Markdown/evaluation fixtures under the test project; verify every committed text is redistributable and normal tests do not require the ignored private corpus.
- [x] 1.3 Add immutable document, metadata, chunk, and corpus-validation contracts with explicit v1 constants; verify serialization and equality tests retain every required provenance field without provider-specific or Foundry types.

## 2. Markdown Validation and Deterministic Chunking

- [x] 2.1 Implement the constrained UTF-8 Markdown/front-matter loader for the documented scalar and inline-tag-list syntax; verify focused tests cover valid English and Danish documents, missing delimiters and fields, unknown fields, invalid IDs, URLs, dates, languages and tags, empty bodies, duplicate IDs, multiple file errors, and all-or-nothing corpus loading.
- [x] 2.2 Implement heading- and paragraph-aware chunking with the 150–300 word target, intact long paragraphs, short-document handling, final-fragment merging, ordinal IDs, and inherited heading context; verify boundary tests assert exact passages, ordering, word counts, and IDs for representative documents.
- [x] 2.3 Verify deterministic chunk provenance by running identical inputs twice and asserting byte-equivalent derived representations, unique IDs, retained metadata, and `article-chunker-v1`; add an edit case that demonstrates why affected ordinal judgments require review.

## 3. Lexical Retrieval Baseline

- [x] 3.1 Implement the versioned Unicode letter-or-digit tokenizer and BM25 index over title, inherited heading, and passage text with `k1 = 1.2` and `b = 0.75`; verify token and hand-calculated scoring tests cover casing, Danish characters, repeated terms, document length, and absent terms.
- [x] 3.2 Implement top-K retrieval with positive-score filtering, rank and score provenance, descending-score ordering, and ordinal chunk-ID tie-breaking; verify tests cover exact rankings, ties, fewer results than K, zero matches, blank queries, and invalid K without embeddings, storage, or model calls.
- [x] 3.3 Add an educational retrieval trace formatter that displays the query, retriever/tokenizer versions, ranked chunk IDs, scores, provenance, and exact passages; verify a snapshot-style test keeps the trace readable and free of credentials or unrelated corpus text.

## 4. Deterministic Retrieval Evaluation

- [x] 4.1 Add the JSON evaluation contract and all-or-nothing validation for stable query IDs, questions, support expectations, and document/chunk relevance judgments; verify synthetic fixtures cover valid positive and negative cases, missing judgments, duplicate IDs, contradictory support flags, and references absent from the corpus.
- [x] 4.2 Implement per-query Recall@3, reciprocal rank, positive-query aggregate metrics, and separate unsupported-query rejection reporting; verify calculations with hand-worked rankings including multiple relevant chunks, document-level judgments, chunk-level judgments, no relevant top-three result, and no positive score.
- [x] 4.3 Compose corpus loading, chunking, retrieval, and evaluation without external I/O beyond local file reads; verify a deterministic integration test produces the same rankings and metrics across repeated runs and makes no PandaScore or Foundry call.

## 5. Local Corpus Learning Tool

- [x] 5.1 Add the thin .NET 8 `src/SpillerAstralisStats.RagTool` console project to the solution with a `corpus build` command using the private default paths; verify `dotnet run --project src/SpillerAstralisStats.RagTool -- corpus build` resolves application services without starting the Azure Functions host or Foundry client.
- [x] 5.2 Make the command print validation results, document/chunk counts, every evaluation trace, Recall@3, reciprocal rank, aggregate metrics, and negative-query outcomes; verify an end-to-end synthetic run exposes the exact passages and scores a developer needs to inspect.
- [x] 5.3 Return a non-zero exit code with source-specific errors for invalid corpus or evaluation input and a clear message for missing private inputs; verify command tests cover success, missing files, invalid documents, invalid judgments, and no accidental model generation.

## 6. Citation-Constrained RAG Generation

- [x] 6.1 Refactor the existing match-specific Foundry transport request/client into one neutral strict structured-output boundary while leaving match prompt construction and validation unchanged; verify all existing match briefing, provider-error, cancellation, dependency-injection, and opt-in smoke-path tests continue to pass.
- [x] 6.2 Add immutable article RAG context and result contracts retaining the question, selected passages, retrieval ranks and scores, full source provenance, and prompt/schema/chunker/tokenizer/retriever versions; verify exact serialization contains only the selected chunks and all required metadata.
- [x] 6.3 Define the versioned article RAG developer instruction and strict discriminated JSON Schema for `available` and `insufficientContext` output; verify contract tests prohibit outside facts, calculated match statistics, predictions, unknown fields, editorial text without citations, and citations on insufficient-context output.
- [x] 6.4 Implement C# output validation for state consistency, bounded non-empty text, unique citations, and citation membership in the supplied context; verify parameterized tests reject blank or overlong text, missing, duplicate, and unretrieved citations, malformed states, and schema-incompatible JSON.
- [x] 6.5 Implement question → retrieval → context → structured client → validated result orchestration; verify fake-client tests cover valid cited output, pre-model insufficient context with zero client calls, model-selected insufficient context, refusal, provider failure, malformed output, invalid citation, and cancellation without partial results.
- [x] 6.6 Register the article RAG generator through the existing configurable Foundry infrastructure without connecting it to `DailyStatsFunction` or static publication; verify the Function host and corpus tool resolve and run without Foundry configuration until generation is explicitly requested.

## 7. Guided Local Corpus and Evaluation Exercise

- [x] 7.1 Update `README.md` with the exact supported front matter, lawful-use warning, source-cleaning guidance, chunk identity semantics, private paths, and the corpus-build command; verify a developer can prepare one article without reading implementation code and no copied article text appears in documentation.
- [x] 7.2 Manually prepare a private corpus of approximately 6–10 overlapping articles across opponents, tournaments, roster changes, match recaps, and at least one useful distractor; verify the files remain ignored and the corpus-build command validates every document and displays plausible chunks before retrieval is judged.
- [x] 7.3 Manually create the private evaluation set alongside the corpus with supported, unsupported, exact-passage, ambiguous, and at least one Danish/English cross-language query; verify every judgment is traceable to inspected source passages and record baseline Recall@3, reciprocal rank, and negative-query outcomes without changing the retriever to hide failures.
- [x] 7.4 Run one learning demonstration showing question → ranked chunk IDs and scores → exact model context → fake or explicit opt-in live structured answer → citation membership validation; verify the demonstration clearly distinguishes retrieval failure, generation failure, and insufficient context.

## 8. Overall Validation

- [x] 8.1 Run focused corpus, chunking, retrieval, evaluation, CLI, transport-regression, and article-generation tests; verify they pass deterministically without private articles, PandaScore, Foundry credentials, or a live model call.
- [x] 8.2 Run `dotnet restore SpillerAstralisStats.sln`, `dotnet build SpillerAstralisStats.sln`, and `dotnet test SpillerAstralisStats.sln`; verify all commands succeed without warnings and the opt-in Foundry smoke path remains disabled by default.
- [x] 8.3 Run `openspec validate add-article-rag-retrieval --strict`; verify implementation, tests, and documentation satisfy the article RAG contract while embeddings, vector storage, hybrid retrieval, reranking, automated collection, static publication, and `/stats` integration remain outside the change.
