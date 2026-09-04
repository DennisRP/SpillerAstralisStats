## 1. Reproducible Demo Inputs

- [x] 1.1 Extend the committed synthetic article fixtures with one clearly labeled lexical distractor and supported/unsupported evaluation cases; verify `corpus build` reports deterministic chunks, rankings, Recall@3, reciprocal rank, and unsupported-query outcomes without private files or network access.
- [x] 1.2 Define the fixed demonstration questions and fake structured responses for a cited answer, model-selected insufficient context, and invalid citation; verify every cited ID is derived from the committed fixture chunks rather than hardcoded to private content.

## 2. Demo Orchestration and Output

- [x] 2.1 Add a small testable demonstration orchestrator that composes the existing corpus loader, chunker, BM25 retriever, evaluator, RAG context builder, generator, and citation validator; verify tests prove these production paths are used and no PandaScore or Foundry client is resolved.
- [x] 2.2 Add `demo run` to `SpillerAstralisStats.RagTool` with committed fixture paths as defaults; verify the command prints stable ordered stages for documents/chunks, supported retrieval, exact context, validated fake answer and citations, metrics, retrieval-level insufficient context, model-selected insufficient context, and invalid-citation generation failure.
- [x] 2.3 Keep `corpus build` and `article answer` behavior backward compatible; verify their existing success, validation-error, preview, and explicit `--use-foundry` command paths remain unchanged.

## 3. Demonstration Guide

- [x] 3.1 Add a concise technical demo guide and link it from `README.md`; verify it provides the exact network-free command, labels fake output clearly, and explains deterministic PandaScore facts versus article retrieval, chunk provenance, Recall@3/MRR, citation constraints, and the three failure categories.
- [x] 3.2 Document the optional live Foundry follow-up and the evidence needed before considering embeddings, hybrid retrieval, or reranking; verify the default walkthrough requires no credentials, private corpus, external service, or model cost.

## 4. Validation

- [x] 4.1 Add focused automated tests for stage ordering, selected chunk IDs, version metadata, safe failure output, citation membership, deterministic repeated output, and absence of secret/private text; verify the focused test selection passes without network access.
- [x] 4.2 Run the documented `demo run` command twice and verify equivalent output and successful exit codes, then run `dotnet restore`, `dotnet build`, and `dotnet test` for the solution with no warnings or live model calls.
- [x] 4.3 Run `openspec validate add-rag-technical-demo --strict` and verify the implementation remains limited to the reproducible demonstration and focused reliability scope.
