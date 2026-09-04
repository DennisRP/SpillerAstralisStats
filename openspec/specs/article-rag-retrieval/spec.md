# article-rag-retrieval Specification

## Purpose

Provides a deterministic and auditable RAG path from curated Markdown articles through passage retrieval and evaluation to structured, citation-constrained editorial output.

## Requirements

### Requirement: Markdown corpus documents have validated provenance
The system SHALL load one article per Markdown file and SHALL require front matter containing a unique stable document ID, non-empty title, source name, absolute HTTP or HTTPS source URL, publication date, supported language code, and non-empty normalized tags. It MUST reject an invalid corpus as a whole, identify every failing source file and field, and MUST NOT expose a partially accepted corpus.

#### Scenario: Valid article corpus is loaded
- **WHEN** every Markdown file contains the required valid metadata and article body
- **THEN** the system returns the complete set of typed documents with their provenance unchanged

#### Scenario: Article metadata is invalid
- **WHEN** a file omits a required field, contains an invalid value, has no article body, or duplicates another document ID
- **THEN** corpus loading fails with errors that identify the source file and invalid field without returning a partial corpus

### Requirement: Real article content remains private local input
The repository SHALL exclude the real corpus path `content/rag/articles/` and the real local evaluation set from source control. Real article text and derived chunks MUST NOT be copied into `dist` or other public static output. Redistributable examples and automated test fixtures SHALL use synthetic content, personal notes, or lawfully permitted excerpts.

#### Scenario: Developer adds a real article
- **WHEN** a developer stores an article under `content/rag/articles/`
- **THEN** the repository ignore rules prevent the article and its derived text from being added to source control or public static output

#### Scenario: Automated tests load article fixtures
- **WHEN** the normal test suite exercises corpus behavior
- **THEN** it uses committed redistributable fixtures rather than the developer's private corpus

### Requirement: Documents are chunked deterministically at structural boundaries
The system SHALL group Markdown headings and complete paragraphs into ordered chunks targeting approximately 150 through 300 words. It SHALL preserve a paragraph rather than split it solely to meet the target, SHALL avoid splitting a short document unnecessarily, and SHALL produce identical chunks from identical source text and chunker configuration.

#### Scenario: Multi-section article is chunked
- **WHEN** a valid article contains headings and enough complete paragraphs for multiple chunks
- **THEN** the chunks preserve article order, relevant heading context, and complete paragraph boundaries while approaching the target size

#### Scenario: Short article is chunked
- **WHEN** a valid article fits coherently within one target-sized chunk
- **THEN** the system emits one chunk rather than creating unnecessary fragments

#### Scenario: One paragraph exceeds the target
- **WHEN** a single source paragraph is longer than the target maximum
- **THEN** the paragraph remains intact in a deterministically oversized chunk

### Requirement: Every chunk retains stable provenance
Each chunk SHALL retain its document ID, deterministic ordinal chunk ID, title, source name, source URL, publication date, language, tags, heading context, chunker version, and exact passage text. Chunk IDs SHALL be unique within a corpus and SHALL be reproducible when the source document and chunker version are unchanged.

#### Scenario: Derived chunks are inspected
- **WHEN** a document is chunked successfully
- **THEN** every chunk can be traced to its original source and reproduced from the same document and chunker version

#### Scenario: Source content changes
- **WHEN** an article edit changes paragraph grouping or chunk ordering
- **THEN** the regenerated ordinal IDs describe the new derived corpus and affected evaluation judgments must be reviewed

### Requirement: Lexical retrieval is deterministic and inspectable
The system SHALL rank corpus chunks for a non-empty query using a versioned in-memory lexical scoring method. It SHALL return at most the requested positive top-K results ordered by descending score with a stable chunk-ID tie-breaker, and each result SHALL expose its rank, score, chunk provenance, retriever version, and passage text. It SHALL NOT require embeddings, a vector store, a database, or a model call.

#### Scenario: Relevant terms occur in multiple chunks
- **WHEN** a valid query shares indexed terms with multiple chunks
- **THEN** the system returns the highest-scoring chunks in deterministic order with inspectable scores and metadata

#### Scenario: Scores are tied
- **WHEN** two chunks receive the same lexical score
- **THEN** their relative order is resolved deterministically by chunk ID

#### Scenario: Retrieval input is invalid
- **WHEN** the query is blank or top-K is not positive
- **THEN** retrieval is rejected without returning partial results

### Requirement: Retrieval quality is evaluated independently of generation
The system SHALL run a fixed evaluation set that identifies each query, its expected relevant document or chunk IDs, and whether the corpus is expected to provide support. For supported queries it SHALL report Recall@3 and reciprocal rank from the deterministic ranking. For unsupported queries it SHALL report whether the retriever correctly returned no positive-scoring evidence. Evaluation MUST NOT invoke PandaScore or a language model.

#### Scenario: Supported evaluation query is run
- **WHEN** a query has one or more relevant evidence judgments
- **THEN** the report includes its ranked results, Recall@3, reciprocal rank, and the aggregate metrics for all supported queries

#### Scenario: Relevant evidence is absent from the first three results
- **WHEN** none of a query's expected relevant IDs occurs in its top three results
- **THEN** its Recall@3 and reciprocal rank are reported according to the ranking rather than inferred from generated prose

#### Scenario: Unsupported evaluation query is run
- **WHEN** an evaluation case declares that the corpus cannot support the query
- **THEN** the report records success only when no positive-scoring evidence is selected for generation

### Requirement: RAG context contains only selected evidence
The system SHALL create a versioned, serializable RAG context containing the question and only the selected retrieval results. For every supplied passage it SHALL retain rank, retrieval score, document ID, chunk ID, title, source URL, publication date, language, retriever version, and chunker version. When retrieval produces no positive-scoring evidence, the system SHALL return an explicit insufficient-context result without invoking the model.

#### Scenario: Positive evidence is retrieved
- **WHEN** one or more chunks have a positive lexical score for a valid question
- **THEN** the model context contains only the selected ranked chunks and their complete provenance

#### Scenario: No positive evidence is retrieved
- **WHEN** every corpus chunk has a non-positive lexical score for a valid question
- **THEN** the operation returns insufficient context and makes no model request

### Requirement: Generated editorial output is structured and citation constrained
The system SHALL request a versioned structured result containing either an available editorial answer with one or more cited chunk IDs or an explicit insufficient-context state. It MUST instruct the model to use only the supplied chunks, distinguish article claims from deterministic match facts, omit unsupported claims, and avoid predictions. Before accepting an available answer, the system MUST validate its structure, text limits, non-empty citations, and that every cited chunk ID belongs to the supplied RAG context.

#### Scenario: Model returns a valid cited answer
- **WHEN** the model returns valid editorial text whose citations all identify supplied chunks
- **THEN** the system returns the answer, unchanged RAG context, citations, prompt version, and schema version

#### Scenario: Model cites evidence that was not retrieved
- **WHEN** the model returns a chunk ID absent from the supplied RAG context
- **THEN** the entire output is rejected without a partial answer

#### Scenario: Supplied passages do not support an answer
- **WHEN** the model reports the structured insufficient-context state
- **THEN** the system returns that state without inventing editorial content or citations

#### Scenario: Provider or output fails
- **WHEN** the provider refuses, fails, is cancelled, or returns malformed or semantically invalid output
- **THEN** generation fails predictably without a partial RAG result and cancellation is propagated

### Requirement: Corpus preparation and evaluation have one explicit local entry point
The project SHALL document one local command that validates the private corpus, regenerates chunks, reports document and chunk counts, runs the fixed retrieval evaluations, and prints ranked chunk IDs, scores, and passages for inspection. The command MUST complete without a database, PandaScore request, Foundry configuration, or live model call.

#### Scenario: Developer builds the local corpus
- **WHEN** a developer runs the documented command with valid local documents and evaluation cases
- **THEN** the output makes corpus validation, chunk formation, rankings, scores, and retrieval metrics inspectable without external services

#### Scenario: Local corpus build fails validation
- **WHEN** the corpus or evaluation set is invalid
- **THEN** the command exits unsuccessfully with source-specific validation errors and does not run generation

### Requirement: Normal automated tests remain network-free
The normal automated test suite SHALL verify corpus validation, chunking, ranking, metrics, context construction, citation validation, insufficient-context behavior, provider failures, and cancellation using synthetic fixtures and fake model responses. A real Foundry call SHALL remain a separate explicit opt-in check.

#### Scenario: Normal test suite runs
- **WHEN** tests run without live-service opt-in or external credentials
- **THEN** no PandaScore or model network call is made

### Requirement: Article RAG has a reproducible technical demonstration
The system SHALL provide one documented local demonstration that uses only committed redistributable fixtures and deterministic fake model output. The demonstration SHALL expose the source documents and derived chunk identities, retrieval query and ranked passages, exact model context, structured editorial result, cited chunk IDs, citation membership validation, retrieval metrics, and component version metadata. It MUST NOT require private articles, PandaScore, Foundry configuration, credentials, a database, or a network call.

#### Scenario: Developer runs the supported-evidence demonstration
- **WHEN** a developer runs the documented demonstration command with its default committed fixtures
- **THEN** the command completes successfully and presents the source-to-chunk, retrieval, context, cited-answer, citation-validation, and evaluation stages in a stable readable order

#### Scenario: Demonstration shows insufficient retrieval context
- **WHEN** the demonstration evaluates a question with no positive-scoring lexical evidence
- **THEN** it reports retrieval-level insufficient context and proves that generation was not invoked

#### Scenario: Demonstration shows model-selected insufficient context
- **WHEN** retrieved passages share terms with a question but do not support its requested claim
- **THEN** the deterministic fake model result reports insufficient context without editorial text or citations

#### Scenario: Demonstration shows generation failure
- **WHEN** the deterministic fake model returns an invalid or unretrieved citation in the failure example
- **THEN** the demonstration reports a generation validation failure without exposing a partial answer or treating it as a retrieval failure

#### Scenario: Live generation remains optional
- **WHEN** a developer runs the reproducible demonstration without an explicit live-service option
- **THEN** no Foundry client is resolved and no model cost or external request is incurred
