## Why

The article RAG path works, but its learning value is spread across README sections, test fixtures, evaluation output, and an optional live command. A concise reproducible demonstration is needed so another developer can understand the architecture, inspect the evidence flow, and distinguish retrieval quality from generation quality without private content or model cost.

## What Changes

- Add one documented, network-free technical demonstration using the committed synthetic article corpus and evaluation fixtures.
- Make the demonstration show source-to-chunk transformation, ranked BM25 passages, exact model context, a deterministic cited fake answer, and citation membership validation.
- Include explicit demonstrations of retrieval failure, generation failure, and insufficient context with safe, non-sensitive output.
- Explain the separation between deterministic PandaScore facts and unstructured article retrieval, the meaning of Recall@3/MRR, and why semantic retrieval remains a measured future experiment.
- Keep the existing opt-in Foundry command as an optional extension rather than a prerequisite for the reproducible demo.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `article-rag-retrieval`: Add a reproducible technical demonstration contract covering the complete inspectable RAG path and its failure modes.

## Impact

- `SpillerAstralisStats.RagTool` gains a deterministic demonstration command or mode backed only by committed synthetic fixtures and a fake structured-output response.
- README or focused demo documentation gains a short walkthrough and expected interpretation of the output.
- Automated tests cover the demonstration output and prove that it performs no PandaScore or Foundry network calls.
- Private article content, `/stats`, `DailyStatsFunction`, deployment, and production retry/telemetry behavior remain unchanged.
