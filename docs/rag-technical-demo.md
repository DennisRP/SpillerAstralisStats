# Article RAG technical demonstration

Run the complete reproducible walkthrough from the repository root:

```powershell
dotnet run --project src/SpillerAstralisStats.RagTool -- demo run
```

The command uses only the committed synthetic fixtures under
`src/SpillerAstralisStats.Tests/Fixtures/Articles/`. It does not read the ignored
private article corpus, resolve PandaScore or Foundry clients, use credentials, or
make a network call. The displayed answer and failure outcomes are deterministic fake
model responses: they prove the RAG orchestration and validation contracts, not model
writing quality.

## Read the stages in order

1. **Synthetic corpus** lists the source documents and stable derived chunk IDs. The
   Falcons preview is a lexical distractor; the G2 recap contains the supported fact.
2. **Retrieval evaluation** runs the fixed questions. Recall@3 asks whether expected
   evidence appears among the first three passages. Reciprocal rank rewards finding
   the first expected passage earlier. These metrics measure retrieval only, not the
   quality of generated prose.
3. **Supported answer** prints ranked passages and the exact JSON context supplied to
   the fake model. Its cited chunk ID must belong to that context; the same C# validator
   used for normal article generation enforces the rule.
4. **Retrieval-level insufficient context** uses a query with no lexical overlap. No
   passage is selected and generation is never invoked.
5. **Model-selected insufficient context** has lexical overlap, but the passages do
   not support the requested claim. The fake model returns `insufficientContext` with
   no answer or citations.
6. **Generation validation failure** supplies an unretrieved citation. Retrieval did
   succeed, but C# rejects the response as a generation failure and exposes no partial
   answer.

The distinction matters: a low Recall@3/MRR result is a retrieval problem; a model
refusal, malformed result, or invalid citation is a generation problem. Neither is a
reason to let an LLM calculate PandaScore facts. Match selection, scores, form, and
head-to-head statistics remain deterministic PandaScore-derived data; articles are an
independent unstructured editorial-evidence path.

## Optional live follow-up

After the deterministic walkthrough, you can inspect a private article-corpus question
and optionally make a live Foundry call:

```powershell
dotnet run --project src/SpillerAstralisStats.RagTool -- article answer `
  --question "Hvad blev resultatet på Nuke i finalen mellem MOUZ og Spirit?"

dotnet run --project src/SpillerAstralisStats.RagTool -- article answer `
  --question "Hvad blev resultatet på Nuke i finalen mellem MOUZ og Spirit?" `
  --use-foundry
```

The first command is a no-cost preview. The second requires the configured Foundry
endpoint and deployment and may incur model cost. It is not part of the reproducible
demo.

## When to investigate semantic retrieval

Do not add embeddings, a vector database, hybrid search, or reranking merely because
this is RAG. First record a fixed evaluation set and a lexical baseline. A change is
worth measuring only when repeated, judged failures reveal a concrete limitation such
as Danish/English vocabulary mismatch, relevant evidence consistently outside top-3,
or weak shared terms producing false positives. Compare any candidate approach against
the same corpus and relevance judgments.
