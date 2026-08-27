## 1. Fact Contracts and Validation

- [x] 1.1 Add immutable request, aggregate, recent-form, head-to-head, individual-match, score, and outcome models; verify nullable members distinguish unavailable facts from zero or empty values.
- [x] 1.2 Add request validation for a positive recent-match count and a positive non-Astralis opponent ID; verify focused tests reject invalid requests without returning partial facts.

## 2. Eligible Match Normalization

- [x] 2.1 Implement shared filtering for finished, timestamped matches that list Astralis and another opponent; verify tests exclude unfinished, canceled, unrelated, and timeless records.
- [x] 2.2 Implement provider-ID deduplication and newest-first ordering with the specified timestamp fallback and ID tie-breaker; verify tests cover duplicates, equal timestamps, and requested counts above and below the available total.

## 3. Deterministic Fact Calculation

- [x] 3.1 Implement win, loss, and unknown classification plus recent-form counts and nullable win percentage; verify tests cover mixed outcomes, no decided outcomes, and explicit percentage rounding.
- [x] 3.2 Implement opponent-specific head-to-head selection, counts, ordered meetings, and latest-meeting selection; verify tests cover matching and unrelated opponents plus the no-meetings result.
- [x] 3.3 Map available IDs, timestamps, opponents, participant scores, game count, and competition details into match facts and derive sweep state only from complete results; verify tests cover complete scores, non-sweeps, and missing data without invented values.

## 4. Solution Validation

- [x] 4.1 Run `dotnet restore`, `dotnet build`, and `dotnet test` for the root solution; verify all checks pass without warnings and the new fact tests perform no external I/O.
